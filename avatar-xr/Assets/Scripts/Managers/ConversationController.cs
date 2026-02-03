using UnityEngine;
using System.Collections;
using AvatarXR.Network;
using AvatarXR.Audio;
using AvatarXR.Avatar;

namespace AvatarXR.Managers
{
    /// <summary>
    /// Controla el flujo de conversación entre usuario y avatar.
    /// Integra NetworkManager, MicrophoneRecorder y AvatarLoader.
    /// </summary>
    public class ConversationController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private AvatarLoader avatarLoader;
        [SerializeField] private ConsultorioController consultorioController;

        [Header("Configuración")]
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.Space;
        [SerializeField] private bool usePushToTalk = true;

        private bool isProcessing = false;
        private bool isRecording = false;

        private void Start()
        {
            // Verificar dependencias
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[ConversationController] NetworkManager no encontrado. Creando...");
                var go = new GameObject("NetworkManager");
                go.AddComponent<NetworkManager>();
            }

            if (MicrophoneRecorder.Instance == null)
            {
                Debug.LogError("[ConversationController] MicrophoneRecorder no encontrado. Creando...");
                var go = new GameObject("MicrophoneRecorder");
                go.AddComponent<MicrophoneRecorder>();
            }

            // Suscribirse a eventos
            if (avatarLoader != null)
            {
                avatarLoader.OnAvatarLoaded += OnAvatarReady;
            }
        }

        private void Update()
        {
            if (isProcessing) return;

            // Push-to-talk con tecla o botón del controlador
            if (usePushToTalk)
            {
                HandlePushToTalk();
            }
            
            // Botón A del controlador Quest para grabar
            HandleControllerInput();
        }

        private void HandlePushToTalk()
        {
            if (Input.GetKeyDown(pushToTalkKey))
            {
                StartRecording();
            }
            else if (Input.GetKeyUp(pushToTalkKey) && isRecording)
            {
                StopRecordingAndProcess();
            }
        }

        private void HandleControllerInput()
        {
            // Botón A (Primary) para grabar en Quest
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                if (!isRecording)
                {
                    StartRecording();
                }
                else
                {
                    StopRecordingAndProcess();
                }
            }
        }

        public void StartRecording()
        {
            if (isRecording || isProcessing) return;
            
            if (!MicrophoneRecorder.Instance.IsMicrophoneAvailable)
            {
                Debug.LogWarning("[ConversationController] Micrófono no disponible");
                return;
            }

            isRecording = true;
            MicrophoneRecorder.Instance.StartRecording();
            
            // Notificar a ConsultorioController
            if (consultorioController != null)
            {
                consultorioController.SetMicrophoneState(ConsultorioController.MicrophoneState.Open);
            }
        }

        public void StopRecordingAndProcess()
        {
            if (!isRecording) return;

            isRecording = false;
            byte[] audioData = MicrophoneRecorder.Instance.StopRecording();

            if (audioData != null && audioData.Length > 0)
            {
                ProcessUserInput(audioData);
            }
            else
            {
                Debug.LogWarning("[ConversationController] Audio vacío, ignorando");
                if (consultorioController != null)
                {
                    consultorioController.SetMicrophoneState(ConsultorioController.MicrophoneState.Closed);
                }
            }
        }

        private void ProcessUserInput(byte[] audioData)
        {
            isProcessing = true;

            if (consultorioController != null)
            {
                consultorioController.OnUserAudioCaptured();
            }

            // Obtener estado actual de la sesión
            int currentStress = 7;
            int turnCount = 0;

            if (GameManager.Instance != null)
            {
                // Usar valores de GameManager si están disponibles
            }

            StartCoroutine(SendToBackend(audioData, currentStress, turnCount));
        }

        private IEnumerator SendToBackend(byte[] audioData, int stress, int turn)
        {
            Debug.Log("[ConversationController] Enviando audio al backend...");

            yield return NetworkManager.Instance.ProcessAudio(
                audioData,
                stress,
                turn,
                OnBackendResponse
            );
        }

        private void OnBackendResponse(AudioProcessResponse response)
        {
            if (response == null)
            {
                Debug.LogError("[ConversationController] Error en respuesta del backend");
                isProcessing = false;
                return;
            }

            Debug.Log($"[ConversationController] Respuesta recibida:");
            Debug.Log($"  - Usuario dijo: {response.transcription}");
            Debug.Log($"  - Emoción: {response.user_emotion} ({response.emotion_confidence:F2})");
            Debug.Log($"  - Estrés: {response.stress_level_previous} → {response.stress_level_new}");
            Debug.Log($"  - Avatar: {response.avatar_response_text}");

            // Notificar a ConsultorioController
            if (consultorioController != null)
            {
                consultorioController.OnEmotionClassified(
                    response.user_emotion,
                    response.emotion_confidence,
                    response.stress_level_new
                );
            }

            // Actualizar expresión del avatar
            if (avatarLoader != null && avatarLoader.IsLoaded)
            {
                avatarLoader.UpdateEmotionalState(response.stress_level_new);
            }

            // Reproducir respuesta del avatar
            if (!string.IsNullOrEmpty(response.audio_url))
            {
                StartCoroutine(PlayAvatarResponse(response.audio_url, response.avatar_response_text));
            }
            else
            {
                // Sin audio, solo texto
                Debug.Log($"[Avatar responde (sin audio)]: {response.avatar_response_text}");
                isProcessing = false;
            }
        }

        private IEnumerator PlayAvatarResponse(string audioUrl, string responseText)
        {
            if (consultorioController != null)
            {
                consultorioController.OnAvatarStartSpeaking();
            }

            yield return NetworkManager.Instance.DownloadAudio(audioUrl, (clip) =>
            {
                if (clip != null && avatarLoader != null)
                {
                    avatarLoader.Speak(clip, () =>
                    {
                        OnAvatarFinishedSpeaking();
                    });
                }
                else
                {
                    Debug.LogWarning("[ConversationController] No se pudo reproducir audio");
                    OnAvatarFinishedSpeaking();
                }
            });
        }

        private void OnAvatarFinishedSpeaking()
        {
            isProcessing = false;

            if (consultorioController != null)
            {
                consultorioController.OnAvatarFinishedSpeaking();
            }
        }

        private void OnAvatarReady(GameObject avatar)
        {
            Debug.Log("[ConversationController] Avatar cargado y listo para conversación");
        }
    }
}
