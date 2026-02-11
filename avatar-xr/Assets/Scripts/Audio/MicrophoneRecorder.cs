using UnityEngine;
using System.Collections;
using System.IO;

namespace AvatarXR.Audio
{
    /// <summary>
    /// Graba audio del micrófono y lo convierte a formato WAV.
    /// v2: Fix para audio silencioso en Meta Quest (Oculus Virtual Audio Device).
    /// </summary>
    public class MicrophoneRecorder : MonoBehaviour
    {
        public static MicrophoneRecorder Instance { get; private set; }

        [Header("Configuración de Grabación")]
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private int maxRecordingTime = 30;

        [Header("Debug")]
        [SerializeField] private bool logAmplitude = true;

        private AudioClip recordedClip;
        private string microphoneDevice;
        private bool isRecording = false;
        private float recordingStartTime;

        public bool IsMicrophoneAvailable { get; private set; }
        public bool IsRecording => isRecording;

        public event System.Action OnRecordingStarted;
        public event System.Action<byte[]> OnRecordingStopped;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Request microphone permission on Android (Quest)
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            }
            #endif

            StartCoroutine(InitMicrophoneDelayed());
        }

        /// <summary>
        /// Espera un frame antes de inicializar el micrófono.
        /// En Quest, los dispositivos pueden no estar listos inmediatamente.
        /// </summary>
        private IEnumerator InitMicrophoneDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            CheckMicrophone();
        }

        private void CheckMicrophone()
        {
            string[] devices = Microphone.devices;
            
            if (devices.Length > 0)
            {
                // Listar todos los dispositivos disponibles
                for (int i = 0; i < devices.Length; i++)
                {
                    Debug.Log($"[MicrophoneRecorder] Dispositivo {i}: '{devices[i]}'");
                }

                #if UNITY_EDITOR
                // EN EDITOR: Preferir micrófono REAL de la PC (Realtek, USB, etc.)
                // Evitar dispositivos virtuales de Oculus que dan audio silencioso sin Quest Link
                microphoneDevice = devices[0]; // fallback
                bool foundReal = false;
                for (int i = 0; i < devices.Length; i++)
                {
                    // Saltar dispositivos virtuales de Oculus/Meta
                    if (devices[i].Contains("Oculus") || devices[i].Contains("Meta") || devices[i].Contains("Virtual"))
                        continue;
                    
                    microphoneDevice = devices[i];
                    foundReal = true;
                    break;
                }
                if (!foundReal)
                {
                    Debug.LogWarning("[MicrophoneRecorder] ⚠️ No se encontró mic real de PC, usando: " + microphoneDevice);
                }
                #elif UNITY_ANDROID
                // EN QUEST (standalone): Preferir micrófono del headset
                microphoneDevice = devices[0]; // En Quest solo hay uno normalmente
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i].Contains("Headset") || devices[i].Contains("Quest"))
                    {
                        microphoneDevice = devices[i];
                        break;
                    }
                }
                #else
                microphoneDevice = devices[0];
                #endif
                
                IsMicrophoneAvailable = true;
                Debug.Log($"[MicrophoneRecorder] ✅ Micrófono seleccionado: '{microphoneDevice}'");
            }
            else
            {
                IsMicrophoneAvailable = false;
                Debug.LogError("[MicrophoneRecorder] ❌ No se detectó micrófono");
            }
        }

        /// <summary>
        /// Inicia la grabación de audio.
        /// </summary>
        public void StartRecording()
        {
            if (!IsMicrophoneAvailable)
            {
                Debug.LogError("[MicrophoneRecorder] No hay micrófono disponible");
                return;
            }

            if (isRecording)
            {
                Debug.LogWarning("[MicrophoneRecorder] Ya se está grabando");
                return;
            }

            // Detener cualquier instancia previa del micrófono
            if (Microphone.IsRecording(microphoneDevice))
            {
                Microphone.End(microphoneDevice);
            }

            // Iniciar grabación con loop=false
            recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingTime, sampleRate);
            
            if (recordedClip == null)
            {
                Debug.LogError("[MicrophoneRecorder] ❌ Microphone.Start() retornó null");
                return;
            }

            // CRITICAL FIX: Esperar a que el micrófono realmente empiece a grabar
            StartCoroutine(WaitForMicrophoneStart());
        }

        /// <summary>
        /// Espera hasta que Microphone.GetPosition() sea > 0 antes de marcar como grabando.
        /// Esto evita el bug de audio silencioso donde se lee antes de que haya datos.
        /// </summary>
        private IEnumerator WaitForMicrophoneStart()
        {
            float timeout = 2f;
            float elapsed = 0f;
            
            // Esperar a que el micrófono realmente empiece a escribir datos
            while (Microphone.GetPosition(microphoneDevice) <= 0 && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (elapsed >= timeout)
            {
                Debug.LogError("[MicrophoneRecorder] ❌ Timeout esperando inicio del micrófono");
                Microphone.End(microphoneDevice);
                yield break;
            }

            isRecording = true;
            recordingStartTime = Time.realtimeSinceStartup;
            
            OnRecordingStarted?.Invoke();
            Debug.Log($"[MicrophoneRecorder] 🎤 Grabación iniciada (delay: {elapsed:F2}s, pos: {Microphone.GetPosition(microphoneDevice)})");
        }

        /// <summary>
        /// Detiene la grabación y retorna los datos WAV.
        /// Incluye validación de amplitud para detectar audio silencioso.
        /// </summary>
        public byte[] StopRecording()
        {
            if (!isRecording)
            {
                Debug.LogWarning("[MicrophoneRecorder] No hay grabación activa");
                return null;
            }

            int position = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
            isRecording = false;

            float recordingDuration = Time.realtimeSinceStartup - recordingStartTime;
            Debug.Log($"[MicrophoneRecorder] Posición final: {position}, Duración: {recordingDuration:F2}s");

            if (position <= 0)
            {
                Debug.LogWarning("[MicrophoneRecorder] Grabación vacía (position <= 0)");
                return null;
            }

            // Mínimo 0.5 segundos de audio
            if (recordingDuration < 0.5f)
            {
                Debug.LogWarning("[MicrophoneRecorder] Grabación demasiado corta, ignorando");
                return null;
            }

            // Obtener samples
            float[] samples = new float[position * recordedClip.channels];
            recordedClip.GetData(samples, 0);

            // VALIDACIÓN DE AMPLITUD: Detectar audio silencioso
            float maxAmplitude = 0f;
            float sumAmplitude = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float abs = Mathf.Abs(samples[i]);
                if (abs > maxAmplitude) maxAmplitude = abs;
                sumAmplitude += abs;
            }
            float avgAmplitude = sumAmplitude / samples.Length;

            if (logAmplitude)
            {
                Debug.Log($"[MicrophoneRecorder] 📊 Amplitud - Max: {maxAmplitude:F6}, Avg: {avgAmplitude:F6}, Samples: {samples.Length}");
            }

            if (maxAmplitude < 0.001f)
            {
                Debug.LogWarning($"[MicrophoneRecorder] ⚠️ AUDIO SILENCIOSO detectado (max: {maxAmplitude:F6}). " +
                    "Posibles causas: permisos de micrófono, dispositivo virtual, Quest Link config.");
                // Aún así retornamos el audio para que el backend pueda dar feedback
            }

            // Convertir a WAV
            byte[] wavData = ConvertToWAV(samples, recordedClip.channels, sampleRate);

            Debug.Log($"[MicrophoneRecorder] ⏹️ Grabación detenida. Tamaño: {wavData.Length} bytes, Amplitud max: {maxAmplitude:F4}");
            
            OnRecordingStopped?.Invoke(wavData);
            return wavData;
        }

        /// <summary>
        /// Convierte samples de audio a formato WAV.
        /// </summary>
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
                writer.Write(16);
                writer.Write((ushort)1); // PCM
                writer.Write((ushort)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);

                // Subchunk data
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(sampleCount * 2);

                // Audio data - con clamp para evitar overflow
                foreach (float sample in samples)
                {
                    float clamped = Mathf.Clamp(sample, -1f, 1f);
                    short intSample = (short)(clamped * 32767f);
                    writer.Write(intSample);
                }

                return memStream.ToArray();
            }
        }
    }
}
