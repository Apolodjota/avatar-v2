using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

namespace AvatarXR.Network
{
    [Serializable]
    public class AudioProcessResponse
    {
        public string transcription;
        public string user_emotion;
        public float emotion_confidence;
        public int stress_level_previous;
        public int stress_level_new;
        public string avatar_response_text;
        public string audio_url;
        public int turn_number;
    }

    [Serializable]
    public class SessionStartResponse
    {
        public string session_id;
        public int initial_stress;
        public string timestamp;
    }

    [Serializable]
    public class SynthesizeTextResponse
    {
        public string text;
        public string audio_url;
        public int stress_level;
    }

    /// <summary>
    /// Manager singleton para comunicación HTTP con el backend de IA.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Configuración del Backend")]
        [SerializeField] private string baseUrl = "http://localhost:8000";
        [SerializeField] private float timeout = 30f;

        public bool IsConnected { get; private set; }
        public event Action<bool> OnConnectionStatusChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(CheckConnection());
        }

        private IEnumerator CheckConnection()
        {
            string url = $"{baseUrl}/health";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                bool connected = request.result == UnityWebRequest.Result.Success;
                
                if (connected != IsConnected)
                {
                    IsConnected = connected;
                    OnConnectionStatusChanged?.Invoke(IsConnected);
                }

                if (connected)
                {
                    Debug.Log("[NetworkManager] ✅ Conectado al backend");
                }
                else
                {
                    Debug.LogWarning($"[NetworkManager] ⚠️ No se pudo conectar al backend: {request.error}");
                }
            }
        }

        /// <summary>
        /// Inicia una nueva sesión de entrenamiento.
        /// </summary>
        public IEnumerator StartSession(Action<SessionStartResponse> callback)
        {
            string url = $"{baseUrl}/api/session/start";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)timeout;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SessionStartResponse>(request.downloadHandler.text);
                    Debug.Log($"[NetworkManager] Sesión iniciada: {response.session_id}");
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Error iniciando sesión: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Procesa el audio del usuario y obtiene respuesta del avatar.
        /// </summary>
        public IEnumerator ProcessAudio(
            byte[] audioData,
            int currentStress,
            int turnCount,
            Action<AudioProcessResponse> callback
        )
        {
            string url = $"{baseUrl}/api/process-audio?stress_level={currentStress}&turn_count={turnCount}";

            // Crear formulario con el audio
            WWWForm form = new WWWForm();
            form.AddBinaryData("audio", audioData, "recording.wav", "audio/wav");

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.timeout = (int)timeout;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AudioProcessResponse>(request.downloadHandler.text);
                    Debug.Log($"[NetworkManager] Respuesta recibida - Emoción: {response.user_emotion}");
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Error procesando audio: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Descarga el audio de respuesta del avatar.
        /// </summary>
        public IEnumerator DownloadAudio(string audioUrl, Action<AudioClip> callback)
        {
            // Construir URL completa si es relativa
            string fullUrl = audioUrl.StartsWith("http") ? audioUrl : $"{baseUrl}{audioUrl}";

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fullUrl, AudioType.MPEG))
            {
                request.timeout = (int)timeout;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    Debug.Log($"[NetworkManager] Audio descargado: {clip.length}s");
                    callback?.Invoke(clip);
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Error descargando audio: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Sintetiza texto a voz usando el backend.
        /// </summary>
        public IEnumerator SynthesizeText(string text, int stressLevel, Action<SynthesizeTextResponse> callback)
        {
            string url = $"{baseUrl}/api/synthesize-text";
            
            // Crear JSON body
            string jsonBody = JsonUtility.ToJson(new SynthesizeRequest { text = text, stress_level = stressLevel });
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)timeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<SynthesizeTextResponse>(request.downloadHandler.text);
                    Debug.Log($"[NetworkManager] TTS completado: {response.audio_url}");
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Error sintetizando texto: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        [Serializable]
        private class SynthesizeRequest
        {
            public string text;
            public int stress_level;
        }

        /// <summary>
        /// Cambia la URL base del backend.
        /// </summary>
        public void SetBaseUrl(string url)
        {
            baseUrl = url;
            StartCoroutine(CheckConnection());
        }
    }
}
