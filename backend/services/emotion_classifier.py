import librosa
import numpy as np
from sklearn.preprocessing import StandardScaler
import joblib
import os

class EmotionClassifier:
    def __init__(self, model_path: str = "models/emotion_classifier.pkl"):
        """Cargar modelo de clasificación de emociones."""
        if os.path.exists(model_path):
            self.model = joblib.load(model_path)
            self.scaler = joblib.load(model_path.replace('.pkl', '_scaler.pkl'))
        else:
            print(f"Modelo no encontrado en {model_path}")
            print("Usar modo fallback (reglas heurísticas)")
            self.model = None
            self.scaler = None
    
    def extract_features(self, audio_path: str) -> np.ndarray:
        """
        Extraer características acústicas del audio.
        
        Features extraídas:
        - MFCCs (13 coeficientes)
        - Pitch (F0)
        - Energy (RMS)
        - Zero Crossing Rate
        - Spectral features
        """
        # Cargar audio
        y, sr = librosa.load(audio_path, sr=16000)
        
        # MFCCs
        mfccs = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=13)
        mfcc_mean = np.mean(mfccs, axis=1)
        
        # Pitch (usando YIN algorithm)
        pitches, magnitudes = librosa.piptrack(y=y, sr=sr)
        pitch_values = []
        for t in range(pitches.shape[1]):
            index = magnitudes[:, t].argmax()
            pitch = pitches[index, t]
            if pitch > 0:
                pitch_values.append(pitch)
        pitch_mean = np.mean(pitch_values) if pitch_values else 0
        pitch_std = np.std(pitch_values) if pitch_values else 0
        
        # Energy (RMS)
        rms = librosa.feature.rms(y=y)
        energy_mean = np.mean(rms)
        energy_std = np.std(rms)
        
        # Zero Crossing Rate
        zcr = librosa.feature.zero_crossing_rate(y)
        zcr_mean = np.mean(zcr)
        
        # Spectral features
        spectral_centroid = np.mean(librosa.feature.spectral_centroid(y=y, sr=sr))
        spectral_rolloff = np.mean(librosa.feature.spectral_rolloff(y=y, sr=sr))
        
        # Concatenar todas las features
        features = np.concatenate([
            mfcc_mean,
            [pitch_mean, pitch_std, energy_mean, energy_std, 
             zcr_mean, spectral_centroid, spectral_rolloff]
        ])
        
        return features
    
    def classify(self, audio_path: str) -> dict:
        """
        Clasificar emoción del audio.
        
        Returns:
            dict: {
                "emotion": str,  # empatico, hostil, neutro, ansioso
                "confidence": float,
                "features": dict
            }
        """
        features = self.extract_features(audio_path)
        
        # Extraer valores para análisis
        pitch_mean = features[13]
        energy_mean = features[15]
        
        if self.model is not None:
            # Usar modelo entrenado
            features_scaled = self.scaler.transform(features.reshape(1, -1))
            prediction = self.model.predict(features_scaled)[0]
            probabilities = self.model.predict_proba(features_scaled)[0]
            confidence = float(np.max(probabilities))
            emotion = prediction
        else:
            # Fallback: reglas heurísticas simples
            emotion, confidence = self._heuristic_classification(
                pitch_mean, energy_mean
            )
        
        return {
            "emotion": emotion,
            "confidence": confidence,
            "features": {
                "pitch_hz": float(pitch_mean),
                "energy_rms": float(energy_mean),
                "duration_sec": librosa.get_duration(path=audio_path)
            }
        }
    
    def _heuristic_classification(self, pitch: float, energy: float) -> tuple:
        """Clasificación simple basada en umbrales."""
        # Pitch alto + energía alta = Hostil/Ansioso
        if pitch > 200 and energy > 0.05:
            return "hostil", 0.65
        # Pitch bajo + energía moderada = Empático
        elif pitch < 180 and 0.02 < energy < 0.06:
            return "empatico", 0.70
        # Muy baja energía = Desinteresado
        elif energy < 0.02:
            return "neutro", 0.60
        else:
            return "neutro", 0.55