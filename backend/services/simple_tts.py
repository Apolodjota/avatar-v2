from gtts import gTTS
import os

class SimpleTTSService:
    """
    Servicio TTS simple usando gTTS (Google Text-to-Speech offline).
    No requiere credenciales de Google Cloud.
    """
    
    def __init__(self):
        print("✅ SimpleTTSService inicializado (usando gTTS)")
    
    def synthesize(
        self,
        text: str,
        stress_level: int,
        output_path: str = "temp_audio.mp3"
    ) -> str:
        """
        Sintetizar texto a voz usando gTTS.
        
        Args:
            text: Texto a sintetizar
            stress_level: Nivel de estrés (0-10) - actualmente no se usa
            output_path: Donde guardar el audio
        
        Returns:
            str: Path del archivo generado
        """
        try:
            # Crear directorio si no existe
            os.makedirs(os.path.dirname(output_path) if os.path.dirname(output_path) else ".", exist_ok=True)
            
            # Generar audio con gTTS
            # Usar voz lenta si el estrés es bajo, normal si es alto
            slow = stress_level < 5
            
            # Usar tld='com.mx' para voz masculina mexicana
            # O tld='es' para español de España (también masculino)
            tts = gTTS(text=text, lang='es', slow=slow, tld='com.mx')
            tts.save(output_path)
            
            print(f"✅ Audio TTS generado: {output_path}")
            return output_path
            
        except Exception as e:
            print(f"❌ Error en SimpleTTSService: {e}")
            raise
