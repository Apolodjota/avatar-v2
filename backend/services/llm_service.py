import google.generativeai as genai
import os
import random
from typing import List, Dict, Optional

class LLMService:
    def __init__(self):
        api_key = os.getenv("GEMINI_API_KEY")
        if not api_key:
            raise ValueError("GEMINI_API_KEY no configurada")
        
        genai.configure(api_key=api_key)
        
        # IMPORTANT: Usar modelo explícito para evitar migración automática
        # gemini-1.5-flash tiene quota separada de gemini-2.0-flash
        model_name = os.getenv("GEMINI_MODEL", "gemini-1.5-flash")
        self.model = genai.GenerativeModel(model_name)
        print(f"✅ LLMService inicializado con modelo: {model_name}")
        
        # Historial de conversación por sesión
        self._session_histories: Dict[str, List[Dict]] = {}
        
        # System prompt para el avatar paciente
        self.system_prompt = """Eres un paciente virtual en crisis de ansiedad siendo entrevistado por un estudiante de salud.

CONTEXTO ACTUAL:
- Nivel de estrés actual: {stress_level}/10
- Turno de conversación: {turn_count}
- El estudiante acaba de decir: "{user_input}"

TU PERSONALIDAD:
- Tienes 28 años, trabajas en marketing
- Últimamente sientes presión laboral intensa
- Tiendes a rumiar pensamientos negativos
- Respondes mejor a validación emocional que a consejos directos

INSTRUCCIONES DE RESPUESTA:
1. Si el estudiante fue empático/validante → tu estrés BAJA, muestras apertura
2. Si fue hostil/invalidante → tu estrés SUBE, te cierras más
3. Si fue neutral/técnico → estrés se mantiene, respondes con cautela

Responde EN PRIMERA PERSONA como este paciente.
- Máximo 2-3 oraciones
- Usa lenguaje natural, no técnico
- Si estrés > 7: usa marcadores de ansiedad ("no sé...", "es que...", pausas "...")
- Si estrés 4-7: habla con más calma pero aún preocupado
- Si estrés < 4: muestra alivio y gratitud
- NO menciones tu nivel de estrés numéricamente
- NO uses comillas ni formato especial

RESPUESTA:"""
    
    def get_history(self, session_id: str) -> List[Dict]:
        """Obtiene el historial de conversación de una sesión."""
        if session_id not in self._session_histories:
            self._session_histories[session_id] = []
        return self._session_histories[session_id]
    
    def add_to_history(self, session_id: str, role: str, content: str):
        """Agrega un turno al historial."""
        history = self.get_history(session_id)
        history.append({"role": role, "content": content})
        # Mantener solo los últimos 10 turnos para no exceder el contexto
        if len(history) > 10:
            self._session_histories[session_id] = history[-10:]
    
    def generate_response(
        self,
        user_input: str,
        stress_level: int,
        conversation_history: List[Dict[str, str]],
        turn_count: int,
        session_id: str = None
    ) -> str:
        """
        Generar respuesta del avatar paciente.
        """
        # Usar historial por sesión si está disponible
        if session_id:
            history = self.get_history(session_id)
            self.add_to_history(session_id, "user", user_input)
        else:
            history = conversation_history
        
        # Intentar generar con Gemini
        try:
            response_text = self._generate_with_gemini(
                user_input, stress_level, history, turn_count
            )
            
            # Guardar respuesta en historial
            if session_id:
                self.add_to_history(session_id, "assistant", response_text)
            
            return response_text
            
        except Exception as e:
            error_msg = str(e)
            print(f"⚠️ Error en Gemini: {error_msg}")
            
            # Si es error de quota, usar respuestas offline
            if "429" in error_msg or "RESOURCE_EXHAUSTED" in error_msg:
                print("🔄 Usando respuestas offline (quota agotada)")
                return self._get_offline_response(user_input, stress_level, turn_count)
            
            # Para otros errores, también usar offline
            return self._get_offline_response(user_input, stress_level, turn_count)
    
    def _generate_with_gemini(
        self, user_input: str, stress_level: int,
        history: List[Dict], turn_count: int
    ) -> str:
        """Genera respuesta usando Gemini API."""
        prompt = self.system_prompt.format(
            stress_level=stress_level,
            turn_count=turn_count,
            user_input=user_input
        )
        
        # Construir historial para contexto
        if history:
            history_text = "\n".join([
                f"{'Estudiante' if msg['role'] == 'user' else 'Paciente'}: {msg['content']}"
                for msg in history[-6:]  # Últimos 6 turnos
            ])
            prompt += f"\n\nHISTORIAL RECIENTE:\n{history_text}"
        
        prompt += "\n\nRespuesta del paciente:"
        
        response = self.model.generate_content(prompt)
        text = response.text.strip()
        
        # Limpiar comillas si las hay
        text = text.strip('"').strip("'")
        
        return text
    
    def _get_offline_response(
        self, user_input: str, stress_level: int, turn_count: int
    ) -> str:
        """
        Respuestas offline inteligentes basadas en el nivel de estrés y keywords.
        Se usa como fallback cuando Gemini no está disponible.
        """
        user_lower = user_input.lower() if user_input else ""
        
        # Detectar intención del usuario por keywords simples
        is_empathetic = any(w in user_lower for w in [
            "entiendo", "comprendo", "difícil", "escucho", "ayudar",
            "cuéntame", "sientes", "tranquil", "normal", "válido",
            "aquí estoy", "puedo", "quiero ayudar", "lamento"
        ])
        
        is_hostile = any(w in user_lower for w in [
            "cálmate", "exagera", "no es para tanto", "supéra",
            "deja de", "ya basta", "ridícul", "tonto", "problema tuyo"
        ])
        
        # Respuestas según estrés alto (7-10)
        high_stress = [
            "No sé... es que siento que todo se me viene encima y no puedo con esto.",
            "Es que... no duermo bien, no como bien... todo es demasiado últimamente.",
            "A veces siento que nadie entiende lo que me pasa... es muy frustrante.",
            "No puedo dejar de pensar en el trabajo... las fechas, los reportes... es demasiado.",
            "Siento como un nudo aquí en el pecho que no se va... no sé qué hacer.",
        ]
        
        # Respuestas según estrés medio (4-6)
        mid_stress = [
            "Bueno... supongo que sí necesito hablar de esto. Últimamente ha sido difícil.",
            "Es que en el trabajo me presionan mucho... pero gracias por preguntar.",
            "A veces me siento mejor, pero luego vuelve esa sensación de agobio.",
            "Creo que lo que más me afecta es sentir que no llego a todo lo que me piden.",
            "Hoy ha sido un poco mejor que otros días... pero sigo preocupado.",
        ]
        
        # Respuestas según estrés bajo (0-3)
        low_stress = [
            "Sabes qué... creo que hablar de esto me está ayudando. Gracias.",
            "Me siento un poco más tranquilo ahora. Es bueno que alguien escuche.",
            "Creo que puedo manejar esto si no me lo guardo todo para mí.",
            "Gracias por escucharme... de verdad hacía falta.",
        ]
        
        # Respuestas a empatía (el usuario hizo algo bien)
        empathy_responses = [
            "Gracias por decir eso... no mucha gente se toma el tiempo de escuchar.",
            "Es reconfortante saber que alguien entiende... a veces me siento muy solo con esto.",
            "Eso que dices me hace sentir un poco mejor... como que no estoy loco por sentirme así.",
        ]
        
        # Respuestas a hostilidad
        hostile_responses = [
            "Eso... eso no ayuda. No es tan fácil como parece desde afuera.",
            "Ya... todos dicen lo mismo. Si fuera tan fácil ya lo hubiera hecho.",
            "Mejor no hubiera dicho nada... sabía que no iban a entender.",
        ]
        
        # Input vacío (micrófono silencioso)
        if not user_input or len(user_input.strip()) < 3:
            empty_responses = [
                "Perdón... ¿decías algo? Es que estoy un poco distraído con todo esto.",
                "No te escuché bien... es que a veces me pierdo en mis pensamientos.",
                "¿Podrías repetir? Lo siento, estoy un poco nervioso.",
            ]
            return random.choice(empty_responses)
        
        # Seleccionar respuesta
        if is_empathetic and stress_level > 3:
            return random.choice(empathy_responses)
        elif is_hostile:
            return random.choice(hostile_responses)
        elif stress_level >= 7:
            return random.choice(high_stress)
        elif stress_level >= 4:
            return random.choice(mid_stress)
        else:
            return random.choice(low_stress)
