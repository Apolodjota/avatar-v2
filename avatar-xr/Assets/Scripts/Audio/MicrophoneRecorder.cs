using UnityEngine;
using System.Collections;
using System.IO;

namespace AvatarXR.Audio
{
    /// <summary>
    /// Graba audio del micrófono y lo convierte a formato WAV.
    /// </summary>
    public class MicrophoneRecorder : MonoBehaviour
    {
        public static MicrophoneRecorder Instance { get; private set; }

        [Header("Configuración de Grabación")]
        [SerializeField] private int sampleRate = 16000; // Whisper prefiere 16kHz
        [SerializeField] private int maxRecordingTime = 30; // segundos

        private AudioClip recordedClip;
        private string microphoneDevice;
        private bool isRecording = false;

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

            CheckMicrophone();
        }

        private void CheckMicrophone()
        {
            if (Microphone.devices.Length > 0)
            {
                microphoneDevice = Microphone.devices[0];
                IsMicrophoneAvailable = true;
                Debug.Log($"[MicrophoneRecorder] ✅ Micrófono detectado: {microphoneDevice}");
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

            recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingTime, sampleRate);
            isRecording = true;
            
            OnRecordingStarted?.Invoke();
            Debug.Log("[MicrophoneRecorder] 🎤 Grabación iniciada");
        }

        /// <summary>
        /// Detiene la grabación y retorna los datos WAV.
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

            if (position <= 0)
            {
                Debug.LogWarning("[MicrophoneRecorder] Grabación vacía");
                return null;
            }

            // Recortar el audio al tiempo real grabado
            float[] samples = new float[position * recordedClip.channels];
            recordedClip.GetData(samples, 0);

            // Convertir a WAV
            byte[] wavData = ConvertToWAV(samples, recordedClip.channels, sampleRate);

            Debug.Log($"[MicrophoneRecorder] ⏹️ Grabación detenida. Tamaño: {wavData.Length} bytes");
            
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
}
