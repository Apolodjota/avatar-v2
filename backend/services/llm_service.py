import google.generativeai as genai
import os
from typing import List, Dict

class LLMService:
    def __init__(self):
        api_key = os.getenv("GEMINI_API_KEY")
        if not api_key:
            raise ValueError("GEMINI_API_KEY no configurada")
        
        genai.configure(api_key=api_key)
        self.model = genai.GenerativeModel('gemini-1.5-flash')
        
        # System prompt para el avatar paciente
        self.system_prompt = """Eres un paciente virtual en crisis de ansiedad siendo entrevistado por un estudiante de salud.

CONTEXTO INICIAL:
- Nivel de estrés actual: {stress_level}/10
- Llevas {turn_count} turnos de conversación
- El estudiante acaba de decir: "{user_input}"

TU PERSONALIDAD:
- Tienes 28 años, trabajas en marketing
- Últimamente sientes presión laboral intensa
- Tiendes a rumiar pensamientos negativos
- Respondes mejor a validación emocional que a consejos directos

INSTRUCCIONES DE RESPUESTA:
1. Si el estudiante fue empático/validante → tu estrés BAJA
2. Si fue hostil/invalidante → tu estrés SUBE
3. Si fue neutral/técnico → estrés se mantiene

Responde EN PRIMERA PERSONA como este paciente.
- Máximo 2-3 oraciones
- Usa lenguaje natural, no técnico
- Incluye marcadores de ansiedad si estrés > 6: "no sé...", "todo es...", pausas
- NO menciones tu nivel de estrés numéricamente

RESPUESTA:"""
    
    def generate_response(
        self,
        user_input: str,
        stress_level: int,
        conversation_history: List[Dict[str, str]],
        turn_count: int
    ) -> str:
        """
        Generar respuesta del avatar paciente.
        
        Args:
            user_input: Lo que dijo el estudiante
            stress_level: Nivel actual de estrés (0-10)
            conversation_history: Historial previo
            turn_count: Número de turno actual
        
        Returns:
            str: Respuesta del paciente
        """
        # Formatear prompt con contexto
        prompt = self.system_prompt.format(
            stress_level=stress_level,
            turn_count=turn_count,
            user_input=user_input
        )
        
        # Construir historial para contexto
        history_text = "\n".join([
            f"{'Estudiante' if msg['role'] == 'user' else 'Paciente'}: {msg['content']}"
            for msg in conversation_history[-4:]  # Últimos 4 turnos
        ])
        
        full_prompt = f"{prompt}\n\nHISTORIAL RECIENTE:\n{history_text}\n\nRespuesta del paciente:"
        
        # Generar respuesta
        response = self.model.generate_content(full_prompt)
        return response.text.strip()