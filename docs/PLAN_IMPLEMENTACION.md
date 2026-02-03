# Plan de Implementación: Sistema de Entrenamiento VR para Crisis de Salud Mental

## 📋 Índice
1. [Arquitectura de Desarrollo](#arquitectura)
2. [Fase 1: Configuración del Entorno](#fase1)
3. [Fase 2: Backend de IA](#fase2)
4. [Fase 3: Cliente Unity VR](#fase3)
5. [Fase 4: Integración](#fase4)
6. [Fase 5: Testing y Refinamiento](#fase5)
7. [Checklist de Progreso](#checklist)

---

## 🏗️ Arquitectura de Desarrollo {#arquitectura}

### Estructura de Carpetas Propuesta

```
proyecto-vr-crisis/
├── backend/                    # Servidor Python
│   ├── api/                   # FastAPI endpoints
│   ├── models/                # Modelos ML entrenados
│   ├── services/              # Lógica de negocio
│   │   ├── whisper_stt.py
│   │   ├── emotion_classifier.py
│   │   ├── llm_service.py
│   │   └── tts_service.py
│   ├── utils/                 # Utilidades
│   ├── requirements.txt
│   └── main.py
│
├── avatar-xr/                   # Proyecto Unity
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   ├── MainMenu.unity
│   │   │   └── Consultorio.unity
│   │   ├── Scripts/
│   │   │   ├── Managers/
│   │   │   ├── UI/
│   │   │   ├── Avatar/
│   │   │   └── Network/
│   │   ├── Prefabs/
│   │   ├── Materials/
│   │   └── Models/
│   └── Packages/
│
├── data/                       # Datasets y modelos
│   ├── emotion_classifier/
│   └── session_logs/
│
└── docs/                       # Documentación técnica
    ├── api_specs.md
    └── deployment.md
```

---

## 🚀 FASE 1: Configuración del Entorno {#fase1}

### 1.1 Backend Python

**Duración estimada: 2-3 días**

#### Paso 1: Crear entorno virtual

```bash
# En tu directorio de proyecto
mkdir proyecto-vr-crisis
cd proyecto-vr-crisis
mkdir backend
cd backend

# Crear entorno virtual
python -m venv venv

# Activar (Windows)
venv\Scripts\activate
# Activar (Linux/Mac)
source venv/bin/activate
```

#### Paso 2: Instalar dependencias

Crear `requirements.txt`:

```txt
# Framework web
fastapi==0.109.0
uvicorn[standard]==0.27.0
python-multipart==0.0.6

# IA y ML
openai-whisper==20231117
librosa==0.10.1
soundfile==0.12.1
scikit-learn==1.4.0
xgboost==2.0.3
google-generativeai==0.3.2

# Audio processing
pydub==0.25.1

# Google Cloud TTS
google-cloud-texttospeech==2.16.0

# Database
motor==3.3.2  # MongoDB async driver
pymongo==4.6.1

# Utilidades
python-dotenv==1.0.0
pydantic==2.5.3
httpx==0.26.0
```

#### Paso 3: Estructura inicial del backend

```python
# backend/main.py
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI(title="VR Clinical Training API", version="1.0.0")

# Configurar CORS para Unity
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # En producción, especificar origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/")
def read_root():
    return {"status": "API funcionando", "version": "1.0.0"}

@app.get("/health")
def health_check():
    return {"status": "healthy"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
```

#### Paso 4: Variables de entorno

Crear `.env`:

```env
# API Keys
GEMINI_API_KEY=tu_clave_aqui
GOOGLE_APPLICATION_CREDENTIALS=path/to/google-credentials.json

# MongoDB
MONGODB_URL=mongodb://localhost:27017
DB_NAME=vr_training

# Configuración
WHISPER_MODEL=base
MAX_AUDIO_DURATION=30
STRESS_INITIAL_LEVEL=7
```

### 1.2 Unity VR

**Duración estimada: 3-4 días**

#### Paso 1: Crear proyecto Unity

1. Abrir Unity Hub
2. Crear nuevo proyecto:
   - Nombre: `avatar-xr`
   - Template: **3D (URP)** (Universal Render Pipeline - mejor rendimiento VR)
   - Versión: Unity 6 LTS

#### Paso 2: Configurar para VR

**Instalar paquetes necesarios:**

1. Window → Package Manager
2. Instalar:
   - **XR Plugin Management**
   - **XR Interaction Toolkit** (2.5.0+)
   - **Oculus XR Plugin**

**Configurar XR:**

1. Edit → Project Settings → XR Plug-in Management
2. Activar **Oculus** en Android tab
3. Edit → Project Settings → Player → Android Settings:
   - Minimum API Level: Android 10.0 (API 29)
   - Target API Level: Android 12.0 (API 31)

#### Paso 3: Importar Oculus Integration

1. Asset Store → Buscar "Oculus Integration"
2. Descargar e importar (versión 62.0+)
3. Aceptar upgrade de scripts si lo pide
    
#### Paso 4: Estructura de escenas inicial

Crear 2 escenas:

**MainMenu.unity:**
- Canvas UI para configuración
- OVRCameraRig para preview

**Consultorio.unity:**
- OVRCameraRig
- Lighting setup
- Placeholder para avatar

---

## 🤖 FASE 2: Backend de IA {#fase2}

### 2.1 Módulo de Transcripción (Whisper)

**Archivo: `backend/services/whisper_stt.py`**

```python
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
```

### 2.2 Análisis Acústico y Clasificación Emocional

**Archivo: `backend/services/emotion_classifier.py`**

```python
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
            print(f"⚠️ Modelo no encontrado en {model_path}")
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
```

### 2.3 Servicio LLM (Gemini)

**Archivo: `backend/services/llm_service.py`**

```python
import google.generativeai as genai
import os
from typing import List, Dict

class LLMService:
    def __init__(self):
        api_key = os.getenv("GEMINI_API_KEY")
        if not api_key:
            raise ValueError("GEMINI_API_KEY no configurada")
        
        genai.configure(api_key=api_key)
        self.model = genai.GenerativeModel('gemini-2.0-flash-exp')
        
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
```

### 2.4 Servicio TTS (Google Cloud)

**Archivo: `backend/services/tts_service.py`**

```python
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
```

### 2.5 API Endpoints

**Archivo: `backend/api/routes.py`**

```python
from fastapi import APIRouter, UploadFile, File, HTTPException
from pydantic import BaseModel
import os
import uuid
from services.whisper_stt import get_whisper_service
from services.emotion_classifier import EmotionClassifier
from services.llm_service import LLMService
from services.tts_service import TTSService

router = APIRouter()

# Servicios (singleton)
whisper = get_whisper_service()
emotion_clf = EmotionClassifier()
llm = LLMService()
tts = TTSService()

class ConversationTurn(BaseModel):
    stress_level: int
    conversation_history: list
    turn_count: int

@router.post("/process-audio")
async def process_user_audio(
    audio: UploadFile = File(...),
    stress_level: int = 7,
    turn_count: int = 0
):
    """
    Endpoint principal: procesar audio del usuario y generar respuesta del avatar.
    
    Pipeline:
    1. Guardar audio temporal
    2. Transcribir con Whisper
    3. Clasificar emoción
    4. Actualizar nivel de estrés
    5. Generar respuesta con LLM
    6. Sintetizar voz
    7. Retornar todo
    """
    # Guardar audio temporal
    temp_id = str(uuid.uuid4())
    audio_path = f"temp/{temp_id}.wav"
    os.makedirs("temp", exist_ok=True)
    
    with open(audio_path, "wb") as f:
        f.write(await audio.read())
    
    try:
        # 1. Transcribir
        transcription = whisper.transcribe(audio_path)
        user_text = transcription["text"]
        
        # 2. Clasificar emoción
        emotion_result = emotion_clf.classify(audio_path)
        user_emotion = emotion_result["emotion"]
        
        # 3. Actualizar estrés basado en emoción
        stress_delta = {
            "empatico": -2,
            "hostil": +2,
            "neutro": 0,
            "ansioso": +1
        }.get(user_emotion, 0)
        
        new_stress = max(0, min(10, stress_level + stress_delta))
        
        # 4. Generar respuesta del avatar
        avatar_response = llm.generate_response(
            user_input=user_text,
            stress_level=new_stress,
            conversation_history=[],  # TODO: pasar historial real
            turn_count=turn_count + 1
        )
        
        # 5. Sintetizar voz
        audio_output_path = f"temp/{temp_id}_response.mp3"
        tts.synthesize(
            text=avatar_response,
            stress_level=new_stress,
            output_path=audio_output_path
        )
        
        # 6. Retornar resultados
        return {
            "transcription": user_text,
            "user_emotion": user_emotion,
            "emotion_confidence": emotion_result["confidence"],
            "stress_level_previous": stress_level,
            "stress_level_new": new_stress,
            "avatar_response_text": avatar_response,
            "audio_file": audio_output_path,
            "turn_number": turn_count + 1
        }
        
    finally:
        # Limpiar audio de entrada
        if os.path.exists(audio_path):
            os.remove(audio_path)

@router.get("/session/start")
def start_session():
    """Iniciar nueva sesión de entrenamiento."""
    session_id = str(uuid.uuid4())
    initial_stress = int(os.getenv("STRESS_INITIAL_LEVEL", 7))
    
    return {
        "session_id": session_id,
        "initial_stress": initial_stress,
        "timestamp": "2025-01-28T10:00:00Z"
    }
```

---

## 🎮 FASE 3: Cliente Unity VR {#fase3}

### 3.1 Managers Core

**Archivo: `Assets/Scripts/Managers/SessionManager.cs`**

```csharp
using UnityEngine;
using System;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    
    // Estado de la sesión
    public string SessionId { get; private set; }
    public int CurrentStressLevel { get; private set; } = 7;
    public int TurnCount { get; private set; } = 0;
    public float SessionDuration { get; private set; } = 0f;
    
    // Configuración
    public bool StressBarVisible { get; set; } = true;
    public float Volume { get; set; } = 0.75f;
    
    // Eventos
    public event Action<int> OnStressLevelChanged;
    public event Action<string> OnSessionEnded;
    
    private float sessionStartTime;
    private bool sessionActive = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartSession(string sessionId, int initialStress)
    {
        SessionId = sessionId;
        CurrentStressLevel = initialStress;
        TurnCount = 0;
        sessionStartTime = Time.time;
        sessionActive = true;
        
        Debug.Log($"Sesión iniciada: {SessionId}, Estrés inicial: {initialStress}");
    }
    
    public void UpdateStressLevel(int newLevel)
    {
        CurrentStressLevel = Mathf.Clamp(newLevel, 0, 10);
        OnStressLevelChanged?.Invoke(CurrentStressLevel);
        
        // Verificar condiciones de fin
        CheckEndConditions();
    }
    
    public void IncrementTurn()
    {
        TurnCount++;
    }
    
    void Update()
    {
        if (sessionActive)
        {
            SessionDuration = Time.time - sessionStartTime;
            
            // Timeout de 15 minutos
            if (SessionDuration >= 900f) // 15 min = 900 sec
            {
                EndSession("timeout");
            }
        }
    }
    
    void CheckEndConditions()
    {
        if (!sessionActive) return;
        
        // Victoria: estrés <= 2 y al menos 5 turnos
        if (CurrentStressLevel <= 2 && TurnCount >= 5)
        {
            EndSession("success");
        }
        // Fracaso: estrés = 10
        else if (CurrentStressLevel >= 10)
        {
            EndSession("failure");
        }
    }
    
    public void EndSession(string reason)
    {
        sessionActive = false;
        OnSessionEnded?.Invoke(reason);
        Debug.Log($"Sesión terminada: {reason}. Duración: {SessionDuration}s");
    }
}
```

**Archivo: `Assets/Scripts/Managers/NetworkManager.cs`**

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

[Serializable]
public class AudioProcessResponse
{
    public string transcription;
    public string user_emotion;
    public float emotion_confidence;
    public int stress_level_previous;
    public int stress_level_new;
    public string avatar_response_text;
    public string audio_file;
    public int turn_number;
}

[Serializable]
public class SessionStartResponse
{
    public string session_id;
    public int initial_stress;
    public string timestamp;
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    
    [SerializeField] private string baseUrl = "http://localhost:8000";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public IEnumerator StartSession(Action<SessionStartResponse> callback)
    {
        string url = $"{baseUrl}/session/start";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<SessionStartResponse>(request.downloadHandler.text);
                callback?.Invoke(response);
            }
            else
            {
                Debug.LogError($"Error iniciando sesión: {request.error}");
                callback?.Invoke(null);
            }
        }
    }
    
    public IEnumerator ProcessAudio(
        byte[] audioData,
        int currentStress,
        int turnCount,
        Action<AudioProcessResponse> callback
    )
    {
        string url = $"{baseUrl}/process-audio?stress_level={currentStress}&turn_count={turnCount}";
        
        // Crear form con el audio
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, "recording.wav", "audio/wav");
        
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<AudioProcessResponse>(request.downloadHandler.text);
                callback?.Invoke(response);
            }
            else
            {
                Debug.LogError($"Error procesando audio: {request.error}");
                callback?.Invoke(null);
            }
        }
    }
    
    public IEnumerator DownloadAudio(string filename, Action<AudioClip> callback)
    {
        string url = $"{baseUrl}/static/{filename}";
        
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                callback?.Invoke(clip);
            }
            else
            {
                Debug.LogError($"Error descargando audio: {request.error}");
                callback?.Invoke(null);
            }
        }
    }
}
```

### 3.2 Sistema de Audio y Micrófono

**Archivo: `Assets/Scripts/Audio/MicrophoneRecorder.cs`**

```csharp
using UnityEngine;
using System.Collections;
using System.IO;

public class MicrophoneRecorder : MonoBehaviour
{
    public static MicrophoneRecorder Instance { get; private set; }
    
    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isRecording = false;
    
    [SerializeField] private int sampleRate = 16000; // Whisper prefiere 16kHz
    [SerializeField] private int maxRecordingTime = 30; // segundos
    
    public bool IsMicrophoneAvailable { get; private set; }
    public bool IsRecording => isRecording;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        CheckMicrophone();
    }
    
    void CheckMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            IsMicrophoneAvailable = true;
            Debug.Log($"Micrófono detectado: {microphoneDevice}");
        }
        else
        {
            IsMicrophoneAvailable = false;
            Debug.LogError("No se detectó micrófono");
        }
    }
    
    public void StartRecording()
    {
        if (!IsMicrophoneAvailable)
        {
            Debug.LogError("No hay micrófono disponible");
            return;
        }
        
        if (isRecording)
        {
            Debug.LogWarning("Ya se está grabando");
            return;
        }
        
        recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingTime, sampleRate);
        isRecording = true;
        Debug.Log("Grabación iniciada");
    }
    
    public byte[] StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("No hay grabación activa");
            return null;
        }
        
        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording = false;
        
        // Recortar el audio al tiempo real grabado
        float[] samples = new float[position * recordedClip.channels];
        recordedClip.GetData(samples, 0);
        
        // Convertir a WAV
        byte[] wavData = ConvertToWAV(samples, recordedClip.channels, sampleRate);
        
        Debug.Log($"Grabación detenida. Tamaño: {wavData.Length} bytes");
        return wavData;
    }
    
    private byte[] ConvertToWAV(float[] samples, int channels, int sampleRate)
    {
        using (MemoryStream memStream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(memStream))
        {
            int sampleCount = samples.Length;
            int byteRate = sampleRate * channels * 2; // 16-bit
            
            // Header WAV
            writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + sampleCount * 2);
            writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
            
            // Subchunk fmt
            writer.Write(new char[4] { 'f', 'm', 't', ' ' });
            writer.Write(16); // Subchunk size
            writer.Write((ushort)1); // Audio format (PCM)
            writer.Write((ushort)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((ushort)(channels * 2)); // Block align
            writer.Write((ushort)16); // Bits per sample
            
            // Subchunk data
            writer.Write(new char[4] { 'd', 'a', 't', 'a' });
            writer.Write(sampleCount * 2);
            
            // Audio data (float to 16-bit PCM)
            foreach (float sample in samples)
            {
                short intSample = (short)(sample * short.MaxValue);
                writer.Write(intSample);
            }
            
            return memStream.ToArray();
        }
    }
}
```

---

## 🔗 FASE 4: Integración {#fase4}

### 4.1 Controller Principal de Conversación

**Archivo: `Assets/Scripts/Managers/ConversationController.cs`**

```csharp
using UnityEngine;
using System.Collections;

public class ConversationController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private AvatarController avatar;
    [SerializeField] private StressBarUI stressBar;
    [SerializeField] private MicIndicatorUI micIndicator;
    
    private bool waitingForResponse = false;
    
    void Start()
    {
        // Suscribirse a eventos
        SessionManager.Instance.OnStressLevelChanged += OnStressChanged;
    }
    
    public void OnUserStartSpeaking()
    {
        if (waitingForResponse) return;
        
        micIndicator.SetState(MicIndicatorState.Recording);
        MicrophoneRecorder.Instance.StartRecording();
    }
    
    public void OnUserStopSpeaking()
    {
        if (waitingForResponse) return;
        
        byte[] audioData = MicrophoneRecorder.Instance.StopRecording();
        
        if (audioData != null && audioData.Length > 0)
        {
            ProcessUserInput(audioData);
        }
        else
        {
            Debug.LogWarning("Audio vacío, ignorando");
            micIndicator.SetState(MicIndicatorState.Ready);
        }
    }
    
    void ProcessUserInput(byte[] audioData)
    {
        waitingForResponse = true;
        micIndicator.SetState(MicIndicatorState.Processing);
        
        StartCoroutine(NetworkManager.Instance.ProcessAudio(
            audioData,
            SessionManager.Instance.CurrentStressLevel,
            SessionManager.Instance.TurnCount,
            OnServerResponse
        ));
    }
    
    void OnServerResponse(AudioProcessResponse response)
    {
        if (response == null)
        {
            Debug.LogError("Error en respuesta del servidor");
            waitingForResponse = false;
            micIndicator.SetState(MicIndicatorState.Ready);
            return;
        }
        
        // Actualizar sesión
        SessionManager.Instance.UpdateStressLevel(response.stress_level_new);
        SessionManager.Instance.IncrementTurn();
        
        // Log para debug
        Debug.Log($"Usuario dijo: {response.transcription}");
        Debug.Log($"Emoción detectada: {response.user_emotion} ({response.emotion_confidence:F2})");
        Debug.Log($"Estrés: {response.stress_level_previous} → {response.stress_level_new}");
        Debug.Log($"Avatar responde: {response.avatar_response_text}");
        
        // Descargar y reproducir audio del avatar
        StartCoroutine(PlayAvatarResponse(response.audio_file, response.avatar_response_text));
    }
    
    IEnumerator PlayAvatarResponse(string audioFile, string text)
    {
        yield return NetworkManager.Instance.DownloadAudio(audioFile, (clip) =>
        {
            if (clip != null)
            {
                avatar.Speak(clip, text);
                waitingForResponse = false;
                micIndicator.SetState(MicIndicatorState.Ready);
            }
            else
            {
                Debug.LogError("No se pudo descargar el audio del avatar");
                waitingForResponse = false;
                micIndicator.SetState(MicIndicatorState.Ready);
            }
        });
    }
    
    void OnStressChanged(int newLevel)
    {
        // Actualizar UI
        stressBar.SetLevel(newLevel);
        
        // Actualizar expresión del avatar
        avatar.UpdateEmotion(newLevel);
    }
}
```

### 4.2 Control del Avatar

**Archivo: `Assets/Scripts/Avatar/AvatarController.cs`**

```csharp
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AvatarController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SkinnedMeshRenderer faceMesh;
    [SerializeField] private OVRLipSync lipSync;
    
    private AudioSource audioSource;
    private Animator animator;
    
    // Blendshapes (ARKit standard)
    private int blendBrowDown;
    private int blendEyesLookAway;
    private int blendMouthFrown;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        
        // Mapear blendshapes (nombres dependen del modelo Ready Player Me)
        blendBrowDown = faceMesh.sharedMesh.GetBlendShapeIndex("browDown_L");
        blendEyesLookAway = faceMesh.sharedMesh.GetBlendShapeIndex("eyeLookOut_L");
        blendMouthFrown = faceMesh.sharedMesh.GetBlendShapeIndex("mouthFrown_L");
    }
    
    public void Speak(AudioClip clip, string text)
    {
        audioSource.clip = clip;
        audioSource.Play();
        
        // Activar lip sync
        if (lipSync != null)
        {
            lipSync.enabled = true;
        }
        
        // Animar gestos
        StartCoroutine(AnimateGestures(clip.length));
    }
    
    public void UpdateEmotion(int stressLevel)
    {
        // Mapear estrés a expresiones faciales
        // Estrés alto = ceño fruncido, mirada evasiva
        float intensity = stressLevel / 10f;
        
        faceMesh.SetBlendShapeWeight(blendBrowDown, intensity * 100f);
        faceMesh.SetBlendShapeWeight(blendEyesLookAway, intensity * 80f);
        faceMesh.SetBlendShapeWeight(blendMouthFrown, intensity * 60f);
        
        // Animar parámetros del Animator
        if (animator != null)
        {
            animator.SetFloat("Anxiety", intensity);
        }
    }
    
    IEnumerator AnimateGestures(float duration)
    {
        // Gestos sutiles durante el habla
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Micro-expresiones aleatorias
            if (Random.value < 0.1f)
            {
                // Parpadeo
                yield return new WaitForSeconds(0.1f);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Desactivar lip sync al terminar
        if (lipSync != null)
        {
            lipSync.enabled = false;
        }
    }
}
```

---

## ✅ CHECKLIST DE PROGRESO {#checklist}

### Backend
- [ ] Entorno Python configurado
- [ ] Whisper funcionando localmente
- [ ] Clasificador de emociones (al menos heurístico)
- [ ] Gemini API conectada
- [ ] Google Cloud TTS configurado
- [ ] Endpoint `/process-audio` funcional
- [ ] MongoDB configurado (opcional para MVP)

### Unity VR
- [ ] Proyecto Unity con XR activado
- [ ] Oculus Integration importado
- [ ] Escena MainMenu creada
- [ ] Escena Consultorio con OVRCameraRig
- [ ] Micrófono funcionando en Quest
- [ ] Avatar Ready Player Me importado
- [ ] Sistema de grabación de audio
- [ ] Comunicación con backend (HTTP)
- [ ] Barra de estrés UI
- [ ] Blendshapes del avatar mapeados

### Integración
- [ ] Audio del usuario se envía al backend
- [ ] Respuesta del servidor se recibe correctamente
- [ ] Audio del avatar se reproduce en VR
- [ ] Nivel de estrés se actualiza visualmente
- [ ] Condiciones de finalización funcionan
- [ ] Pantalla de resultados implementada

---

## 📝 Notas Finales

Este plan te da una ruta clara de implementación. Te recomiendo:

1. **Empezar por el backend** - es más fácil debuggear y probar
2. **Luego Unity básico** - sin VR primero, solo desktop
3. **Después VR** - cuando todo funcione en PC
4. **Finalmente optimizar** - rendimiento, latencia, UX

¿Por dónde quieres que empecemos específicamente?
