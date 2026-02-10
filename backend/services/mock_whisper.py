"""
Mock Whisper service para pruebas sin el modelo completo.
Retorna una transcripción de prueba.
"""

class MockWhisperSTT:
    def __init__(self):
        print("⚠️ MockWhisperSTT inicializado (solo para pruebas)")
    
    def transcribe(self, audio_path: str, language: str = "es") -> dict:
        """
        Transcripción mock para pruebas.
        """
        return {
            "text": "[Transcripción de prueba - Whisper no disponible]",
            "language": language,
            "segments": []
        }

def get_mock_whisper_service() -> MockWhisperSTT:
    return MockWhisperSTT()
