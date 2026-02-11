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
        # Debug: check audio file
        import os
        file_size = os.path.getsize(audio_path) if os.path.exists(audio_path) else 0
        print(f"🔍 [Whisper Debug] Audio file: {audio_path}, size: {file_size} bytes")
        
        try:
            import librosa
            audio_data, sr = librosa.load(audio_path, sr=None)
            duration = len(audio_data) / sr if sr > 0 else 0
            max_amplitude = max(abs(audio_data)) if len(audio_data) > 0 else 0
            print(f"🔍 [Whisper Debug] Duration: {duration:.2f}s, Sample rate: {sr}, Max amplitude: {max_amplitude:.4f}")
            
            if max_amplitude < 0.01:
                print(f"⚠️ [Whisper Debug] Audio is nearly SILENT (max amplitude {max_amplitude:.6f})")
        except Exception as e:
            print(f"⚠️ [Whisper Debug] Could not analyze audio: {e}")
        
        result = self.model.transcribe(
            audio_path,
            language=language,
            fp16=False if self.device == "cpu" else True
        )
        
        print(f"🔍 [Whisper Debug] Raw result text: '{result['text']}'")
        print(f"🔍 [Whisper Debug] Segments count: {len(result.get('segments', []))}")
        
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