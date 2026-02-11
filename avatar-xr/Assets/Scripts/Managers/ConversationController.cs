using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using AvatarXR.Network;
using AvatarXR.Audio;
using AvatarXR.Avatar;

namespace AvatarXR.Managers
{
    /// <summary>
    /// Controla el flujo de conversación entre usuario y avatar.
    /// v2: Agrega session_id, historial de conversación, y mejor manejo de errores.
    /// </summary>
    public class ConversationController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private AvatarLoader avatarLoader;
        [SerializeField] private ConsultorioController consultorioController;

        [Header("Configuración")]
        [SerializeField] private bool usePushToTalk = true;
        [SerializeField] private float minRecordingTime = 1.0f; // Mínimo 1 segundo

        private bool isProcessing = false;
        private bool isRecording = false;
        private float recordingStartTime;

        // Session tracking
        private string sessionId;
        private int currentTurn = 0;
        private int currentStress = 7;
        private List<ConversationEntry> conversationHistory = new List<ConversationEntry>();

        [System.Serializable]
        private class ConversationEntry
        {
            public string role; // "user" or "avatar"
            public string text;
            public int stressLevel;
        }

        private void Start()
        {
            // Generar session_id
            sessionId = System.Guid.NewGuid().ToString();
            Debug.Log($"[ConversationController] Session ID: {sessionId}");

            if (NetworkManager.Instance == null)
            {
                var go = new GameObject("NetworkManager");
                go.AddComponent<NetworkManager>();
            }

            if (MicrophoneRecorder.Instance == null)
            {
                var go = new GameObject("MicrophoneRecorder");
                go.AddComponent<MicrophoneRecorder>();
            }

            if (avatarLoader != null)
            {
                avatarLoader.OnAvatarLoaded += OnAvatarReady;
            }
        }

        private void Update()
        {
            if (isProcessing) return;

            if (usePushToTalk)
            {
                HandlePushToTalk();
            }
            
            HandleControllerInput();
        }

        private void HandlePushToTalk()
        {
            if (Keyboard.current == null) return;
            
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StartRecording();
            }
            else if (Keyboard.current.spaceKey.wasReleasedThisFrame && isRecording)
            {
                StopRecordingAndProcess();
            }
        }

        private void HandleControllerInput()
        {
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
            recordingStartTime = Time.realtimeSinceStartup;
            MicrophoneRecorder.Instance.StartRecording();
            
            if (consultorioController != null)
            {
                consultorioController.SetMicrophoneState(ConsultorioController.MicrophoneState.Open);
            }
        }

        public void StopRecordingAndProcess()
        {
            if (!isRecording) return;

            // Verificar tiempo mínimo de grabación
            float elapsed = Time.realtimeSinceStartup - recordingStartTime;
            if (elapsed < minRecordingTime)
            {
                Debug.LogWarning($"[ConversationController] Grabación demasiado corta ({elapsed:F1}s), esperando...");
                StartCoroutine(DelayedStop());
                return;
            }

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

        private IEnumerator DelayedStop()
        {
            float remaining = minRecordingTime - (Time.realtimeSinceStartup - recordingStartTime);
            yield return new WaitForSeconds(remaining);
            
            if (isRecording)
            {
                isRecording = false;
                byte[] audioData = MicrophoneRecorder.Instance.StopRecording();
                if (audioData != null && audioData.Length > 0)
                {
                    ProcessUserInput(audioData);
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

            StartCoroutine(SendToBackend(audioData, currentStress, currentTurn));
        }

        private IEnumerator SendToBackend(byte[] audioData, int stress, int turn)
        {
            Debug.Log($"[ConversationController] Enviando audio al backend (session={sessionId}, turn={turn}, stress={stress})...");

            yield return NetworkManager.Instance.ProcessAudioWithSession(
                audioData,
                stress,
                turn,
                sessionId,
                OnBackendResponse
            );
        }

        private void OnBackendResponse(AudioProcessResponse response)
        {
            if (response == null)
            {
                Debug.LogError("[ConversationController] Error en respuesta del backend");
                isProcessing = false;
                
                // Volver a estado de espera para que el usuario pueda reintentar
                if (consultorioController != null)
                {
                    consultorioController.OnAvatarFinishedSpeaking();
                }
                return;
            }

            Debug.Log($"[ConversationController] Respuesta recibida:");
            Debug.Log($"  - Usuario dijo: {response.transcription}");
            Debug.Log($"  - Emoción: {response.user_emotion} ({response.emotion_confidence:F2})");
            Debug.Log($"  - Estrés: {response.stress_level_previous} → {response.stress_level_new}");
            Debug.Log($"  - Avatar: {response.avatar_response_text}");

            // Actualizar estado local
            currentStress = response.stress_level_new;
            currentTurn = response.turn_number;

            // Guardar en historial local
            if (!string.IsNullOrEmpty(response.transcription))
            {
                conversationHistory.Add(new ConversationEntry {
                    role = "user",
                    text = response.transcription,
                    stressLevel = response.stress_level_previous
                });
            }
            conversationHistory.Add(new ConversationEntry {
                role = "avatar",
                text = response.avatar_response_text,
                stressLevel = response.stress_level_new
            });

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
                Debug.Log($"[Avatar responde (sin audio)]: {response.avatar_response_text}");
                isProcessing = false;
                if (consultorioController != null)
                {
                    consultorioController.OnAvatarFinishedSpeaking();
                }
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

        public void SayText(string text, int stressLevel)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            Debug.Log($"[ConversationController] Solicitando hablar: {text}");
            
            if (consultorioController != null) consultorioController.OnAvatarStartSpeaking();

            if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
            {
                StartCoroutine(SynthesizeAndPlay(text, stressLevel));
            }
            else
            {
                Debug.LogWarning("[ConversationController] Backend no disponible, simulando...");
                StartCoroutine(SimulateSpeech(text));
            }
        }

        private IEnumerator SynthesizeAndPlay(string text, int stressLevel)
        {
            SynthesizeTextResponse ttsResponse = null;
            
            yield return NetworkManager.Instance.SynthesizeText(text, stressLevel, (response) => {
                ttsResponse = response;
            });
            
            if (ttsResponse != null && !string.IsNullOrEmpty(ttsResponse.audio_url))
            {
                yield return NetworkManager.Instance.DownloadAudio(ttsResponse.audio_url, (clip) => {
                    if (clip != null && avatarLoader != null)
                    {
                        Debug.Log($"[ConversationController] Reproduciendo audio TTS: {clip.length}s");
                        avatarLoader.Speak(clip, () => {
                            OnAvatarFinishedSpeaking();
                        });
                    }
                    else
                    {
                        Debug.LogWarning("[ConversationController] No se pudo reproducir audio TTS");
                        OnAvatarFinishedSpeaking();
                    }
                });
            }
            else
            {
                Debug.LogWarning("[ConversationController] TTS falló, simulando...");
                yield return StartCoroutine(SimulateSpeech(text));
            }
        }

        private IEnumerator SimulateSpeech(string text)
        {
            float duration = 2f + (text.Length * 0.05f); 
            Debug.Log($"[ConversationController] Simulando habla por {duration}s");
            yield return new WaitForSeconds(duration);
            OnAvatarFinishedSpeaking();
        }

        private void OnAvatarReady(GameObject avatar)
        {
            Debug.Log("[ConversationController] Avatar cargado y listo para conversación");
        }
    }
}
