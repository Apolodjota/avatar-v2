from google.cloud import texttospeech
import os
from typing import Optional

class TTSService:
    def __init__(self):
        # Verificar credenciales
        creds_path = os.getenv("GOOGLE_APPLICATION_CREDENTIALS")
        if not creds_path or not os.path.exists(creds_path):
            raise ValueError("Google Cloud credentials no configuradas correctamente")
        
        self.client = texttospeech.TextToSpeechClient()
        
        # Configuración de voz (voz femenina latina neutral)
        self.voice = texttospeech.VoiceSelectionParams(
            language_code="es-US",
            name="es-US-Neural2-A",  # Voz femenina neural
            ssml_gender=texttospeech.SsmlVoiceGender.FEMALE
        )
    
    def synthesize(
        self,
        text: str,
        stress_level: int,
        output_path: str = "temp_audio.mp3"
    ) -> str:
        """
        Sintetizar texto a voz con parámetros emocionales.
        
        Args:
            text: Texto a sintetizar
            stress_level: Nivel de estrés (0-10) para modular voz
            output_path: Donde guardar el audio
        
        Returns:
            str: Path del archivo generado
        """
        # Ajustar parámetros según estrés
        # Estrés alto = voz más rápida, pitch variable, volumen alto
        speaking_rate = 0.9 + (stress_level * 0.02)  # 0.9 - 1.1
        pitch = -2.0 + (stress_level * 0.4)  # -2.0 a +2.0
        volume_gain = 0.0 + (stress_level * 0.5)  # 0.0 a +5.0
        
        # Configurar síntesis
        synthesis_input = texttospeech.SynthesisInput(text=text)
        
        audio_config = texttospeech.AudioConfig(
            audio_encoding=texttospeech.AudioEncoding.MP3,
            speaking_rate=speaking_rate,
            pitch=pitch,
            volume_gain_db=volume_gain
        )
        
        # Generar audio
        response = self.client.synthesize_speech(
            input=synthesis_input,
            voice=self.voice,
            audio_config=audio_config
        )
        
        # Guardar archivo
        with open(output_path, "wb") as out:
            out.write(response.audio_content)
        
        return output_path