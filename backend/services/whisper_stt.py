import whisper
import torch
import os
from typing import Optional

class WhisperSTT:
    def __init__(self, model_name: str = "base"):
        """
        Inicializar Whisper.
        Modelos: tiny, base, small, medium, large
        """
        device = "cuda" if torch.cuda.is_available() else "cpu"
        print(f"Cargando Whisper modelo '{model_name}' en {device}...")
        self.model = whisper.load_model(model_name, device=device)
        self.device = device
    
    def transcribe(self, audio_path: str, language: str = "es") -> dict:
        """
        Transcribir audio a texto.
        
        Returns:
            dict: {"text": str, "language": str, "segments": list}
        """
        result = self.model.transcribe(
            audio_path,
            language=language,
            fp16=False if self.device == "cpu" else True
        )
        return {
            "text": result["text"].strip(),
            "language": result["language"],
            "segments": result.get("segments", [])
        }

# Singleton
_whisper_instance: Optional[WhisperSTT] = None

def get_whisper_service() -> WhisperSTT:
    global _whisper_instance
    if _whisper_instance is None:
        model_name = os.getenv("WHISPER_MODEL", "base")
        _whisper_instance = WhisperSTT(model_name)
    return _whisper_instance