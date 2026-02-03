using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using AvatarXR.UI;

namespace AvatarXR.Managers
{
    /// <summary>
    /// Controlador principal de la escena del consultorio.
    /// Gestiona la sesión de entrenamiento, el avatar y la UI diegética.
    /// </summary>
    public class ConsultorioController : MonoBehaviour
    {
        [Header("Referencias del Avatar")]
        [SerializeField] private Transform avatarPlaceholder;
        [SerializeField] private Transform avatarSeatPosition;

        [Header("Barra de Estrés (Diegética)")]
        [SerializeField] private GameObject stressBarObject;
        [SerializeField] private Slider stressBarSlider;
        [SerializeField] private Image stressBarFill;
        [SerializeField] private Gradient stressColorGradient;

        [Header("Indicador de Micrófono (No Diegético)")]
        [SerializeField] private Image microphoneRingIndicator;
        [SerializeField] private TextMeshProUGUI microphoneStateText;

        [Header("Menú de Pausa")]
        [SerializeField] private GameObject pauseMenuCanvas;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        [Header("Pantalla de Resultados")]
        [SerializeField] private ResultsScreen resultsScreen;

        [Header("Configuración de Sesión")]
        [SerializeField] private float sessionTimeLimit = 900f; // 15 minutos
        [SerializeField] private int initialStressLevel = 7;
        [SerializeField] private int minStressForSuccess = 2;
        [SerializeField] private int maxStressForFailure = 10;
        [SerializeField] private int minTurnsForCompletion = 5;

        [Header("Configuración de Inicio")]
        [SerializeField] private bool waitForMenu = true;  // Si true, espera a que el menú llame StartSession()
        [SerializeField] private GameObject mainMenuCanvas; // Referencia al Canvas del menú
        [SerializeField] private MenuStateManager menuStateManager; // Referencia al manager del menú

        [Header("Colores de Estado del Micrófono")]
        [SerializeField] private Color micOpenColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color micProcessingColor = new Color(0.9f, 0.7f, 0.1f, 1f);
        [SerializeField] private Color micClosedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Audio")]
        [SerializeField] private AudioSource avatarAudioSource;
        [SerializeField] private AudioSource ambientAudioSource;

        // Estado de la sesión
        private SessionState currentState;
        private int currentStressLevel;
        private int currentTurn;
        private float sessionElapsedTime;
        private bool isPaused;
        private bool sessionEnded;
        private List<string> detectedEmotions = new List<string>();

        public enum SessionState
        {
            Initializing,
            AvatarSpeaking,
            WaitingForUser,
            ProcessingInput,
            Paused,
            Completed,
            Failed
        }

        public enum MicrophoneState
        {
            Open,
            Processing,
            Closed
        }

        // Eventos para comunicación con otros sistemas
        public event System.Action<int> OnStressLevelChanged;
        public event System.Action<SessionState> OnSessionStateChanged;
        public event System.Action<bool> OnSessionEnded; // true = éxito, false = fracaso

        private void Start()
        {
            // Buscar MenuStateManager si no está asignado
            if (menuStateManager == null)
            {
                menuStateManager = FindObjectOfType<MenuStateManager>();
            }

            // Suscribirse a eventos del MenuStateManager
            if (menuStateManager != null)
            {
                menuStateManager.OnSessionStarted += OnMenuSessionStarted;
                Debug.Log("[Consultorio] Suscrito a MenuStateManager. Esperando inicio de sesión...");
                return; // El MenuStateManager se encargará de mostrar el menú y llamar StartSession
            }

            // Fallback: comportamiento original si no hay MenuStateManager
            if (!waitForMenu)
            {
                InitializeSession();
            }
            else
            {
                // Mostrar menú si existe
                if (mainMenuCanvas != null)
                {
                    mainMenuCanvas.SetActive(true);
                }
                Debug.Log("[Consultorio] Esperando a que el menú inicie la sesión...");
            }
        }

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (menuStateManager != null)
            {
                menuStateManager.OnSessionStarted -= OnMenuSessionStarted;
            }
        }

        /// <summary>
        /// Llamado cuando el MenuStateManager notifica que la sesión debe iniciar.
        /// </summary>
        private void OnMenuSessionStarted()
        {
            Debug.Log("[Consultorio] Evento OnSessionStarted recibido del MenuStateManager.");
            InitializeSession();
        }

        /// <summary>
        /// Método público para iniciar la sesión desde el menú.
        /// </summary>
        public void StartSession()
        {
            Debug.Log("[Consultorio] StartSession() llamado desde el menú.");
            InitializeSession();
        }

        private void Update()
        {
            if (sessionEnded || isPaused) return;

            // Actualizar tiempo de sesión
            sessionElapsedTime += Time.deltaTime;

            // Verificar timeout
            if (sessionElapsedTime >= sessionTimeLimit)
            {
                EndSession(false, "Tiempo agotado");
            }

            // Detectar botón de pausa (Menu button en Quest o ESC en teclado)
            if (OVRInput.GetDown(OVRInput.Button.Start) || Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        private void InitializeSession()
        {
            Debug.Log("[Consultorio] Inicializando sesión de entrenamiento...");

            // Aplicar configuración desde GameManager
            ApplySessionConfig();

            // Estado inicial
            currentStressLevel = initialStressLevel;
            currentTurn = 0;
            sessionElapsedTime = 0f;
            isPaused = false;
            sessionEnded = false;

            // Actualizar UI
            UpdateStressBar(currentStressLevel);
            SetMicrophoneState(MicrophoneState.Closed);

            // Ocultar menú de pausa
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
            }

            // Configurar botones del menú de pausa
            SetupPauseMenuButtons();

            // Iniciar conversación
            StartCoroutine(StartConversationRoutine());
        }

        private void ApplySessionConfig()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[Consultorio] GameManager no encontrado. Usando configuración por defecto.");
                return;
            }

            var config = GameManager.Instance.CurrentSessionConfig;

            // Aplicar visibilidad de barra de estrés
            if (stressBarObject != null)
            {
                stressBarObject.SetActive(config.ShowStressBar);
            }

            // Aplicar volumen
            AudioListener.volume = config.Volume;

            Debug.Log($"[Consultorio] Configuración aplicada: Volume={config.Volume}, StressBarVisible={config.ShowStressBar}");
        }

        private void SetupPauseMenuButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeSession);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartSession);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(ExitToMainMenu);
            }
        }

        private IEnumerator StartConversationRoutine()
        {
            // Esperar a que la escena se estabilice
            yield return new WaitForSeconds(1f);

            // Transición a estado inicial
            SetSessionState(SessionState.Initializing);

            // Pequeña pausa antes de que el avatar hable
            yield return new WaitForSeconds(0.5f);

            // Avatar inicia la conversación
            SetSessionState(SessionState.AvatarSpeaking);
            
            // TODO: Aquí se conectará con el backend para obtener el diálogo inicial
            // Por ahora, simulamos la primera línea del avatar
            Debug.Log("[Consultorio] Avatar dice: 'No sé qué hacer... todo me supera últimamente'");

            // Simular duración del audio del avatar (se reemplazará con duración real)
            yield return new WaitForSeconds(3f);

            // Abrir micrófono para el usuario
            SetSessionState(SessionState.WaitingForUser);
            SetMicrophoneState(MicrophoneState.Open);

            currentTurn = 1;
            Debug.Log($"[Consultorio] Turno {currentTurn} - Esperando respuesta del usuario...");
        }

        /// <summary>
        /// Actualiza el nivel de estrés del avatar y la UI correspondiente.
        /// </summary>
        public void UpdateStressLevel(int newStressLevel)
        {
            int previousLevel = currentStressLevel;
            currentStressLevel = Mathf.Clamp(newStressLevel, 0, 10);

            Debug.Log($"[Consultorio] Estrés actualizado: {previousLevel} → {currentStressLevel}");

            // Animar la barra de estrés
            StartCoroutine(AnimateStressBar(previousLevel, currentStressLevel));

            // Notificar cambio
            OnStressLevelChanged?.Invoke(currentStressLevel);

            // Verificar condiciones de finalización
            CheckEndConditions();
        }

        private void UpdateStressBar(int level)
        {
            if (stressBarSlider != null)
            {
                stressBarSlider.value = level / 10f;
            }

            if (stressBarFill != null && stressColorGradient != null)
            {
                stressBarFill.color = stressColorGradient.Evaluate(level / 10f);
            }
        }

        private IEnumerator AnimateStressBar(int fromLevel, int toLevel)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                float currentLevel = Mathf.Lerp(fromLevel, toLevel, smoothT);
                UpdateStressBar(Mathf.RoundToInt(currentLevel));

                yield return null;
            }

            UpdateStressBar(toLevel);
        }

        private void CheckEndConditions()
        {
            // Éxito: estrés ≤ 2 y al menos 5 turnos
            if (currentStressLevel <= minStressForSuccess && currentTurn >= minTurnsForCompletion)
            {
                EndSession(true, "¡Desescalamiento exitoso!");
                return;
            }

            // Fracaso: estrés = 10
            if (currentStressLevel >= maxStressForFailure)
            {
                EndSession(false, "El paciente ha abandonado la sesión");
                return;
            }
        }

        private void EndSession(bool success, string message)
        {
            if (sessionEnded) return;

            sessionEnded = true;
            Debug.Log($"[Consultorio] Sesión terminada - {(success ? "ÉXITO" : "FRACASO")}: {message}");

            SetSessionState(success ? SessionState.Completed : SessionState.Failed);
            SetMicrophoneState(MicrophoneState.Closed);

            // Notificar finalización
            OnSessionEnded?.Invoke(success);

            // Mostrar pantalla de resultados
            StartCoroutine(ShowResultsRoutine(success, message));
        }

        private IEnumerator ShowResultsRoutine(bool success, string message)
        {
            // Fade out gradual
            yield return new WaitForSeconds(1f);

            Debug.Log($"[Consultorio] Mostrando resultados - Turnos: {currentTurn}, Tiempo: {sessionElapsedTime:F1}s, Estrés final: {currentStressLevel}");

            // Mostrar pantalla de resultados si está configurada
            if (resultsScreen != null)
            {
                resultsScreen.ShowResults(
                    success,
                    message,
                    initialStressLevel,
                    currentStressLevel,
                    currentTurn,
                    sessionElapsedTime,
                    detectedEmotions.ToArray()
                );
                // No volver automáticamente al menú, el usuario elige
                yield break;
            }

            // Fallback si no hay ResultsScreen
            yield return new WaitForSeconds(3f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }

        /// <summary>
        /// Establece el estado del indicador de micrófono.
        /// </summary>
        public void SetMicrophoneState(MicrophoneState state)
        {
            Color targetColor;
            string stateText;

            switch (state)
            {
                case MicrophoneState.Open:
                    targetColor = micOpenColor;
                    stateText = "ABIERTO";
                    break;
                case MicrophoneState.Processing:
                    targetColor = micProcessingColor;
                    stateText = "PROCESANDO";
                    break;
                default:
                    targetColor = micClosedColor;
                    stateText = "CERRADO";
                    break;
            }

            if (microphoneRingIndicator != null)
            {
                microphoneRingIndicator.color = targetColor;
            }

            if (microphoneStateText != null)
            {
                microphoneStateText.text = stateText;
                microphoneStateText.color = targetColor;
            }
        }

        private void SetSessionState(SessionState newState)
        {
            currentState = newState;
            Debug.Log($"[Consultorio] Estado de sesión: {newState}");
            OnSessionStateChanged?.Invoke(newState);
        }

        #region Pause Menu

        public void TogglePause()
        {
            if (sessionEnded) return;

            isPaused = !isPaused;

            if (isPaused)
            {
                PauseSession();
            }
            else
            {
                ResumeSession();
            }
        }

        private void PauseSession()
        {
            isPaused = true;
            Time.timeScale = 0f;
            SetSessionState(SessionState.Paused);

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);
            }

            Debug.Log("[Consultorio] Sesión pausada");
        }

        public void ResumeSession()
        {
            isPaused = false;
            Time.timeScale = 1f;
            SetSessionState(SessionState.WaitingForUser);

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
            }

            Debug.Log("[Consultorio] Sesión reanudada");
        }

        public void RestartSession()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Consultorio");
        }

        public void ExitToMainMenu()
        {
            Time.timeScale = 1f;
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }

        #endregion

        #region Public API for Backend Integration

        /// <summary>
        /// Llamado cuando el usuario termina de hablar y el audio se envía al backend.
        /// </summary>
        public void OnUserAudioCaptured()
        {
            SetMicrophoneState(MicrophoneState.Processing);
            SetSessionState(SessionState.ProcessingInput);
        }

        /// <summary>
        /// Llamado cuando el backend responde con la clasificación emocional.
        /// </summary>
        public void OnEmotionClassified(string emotion, float confidence, int newStressLevel)
        {
            Debug.Log($"[Consultorio] Emoción detectada: {emotion} (confianza: {confidence:F2})");
            
            // Guardar emociones únicas para los resultados
            if (!detectedEmotions.Contains(emotion))
            {
                detectedEmotions.Add(emotion);
            }
            
            UpdateStressLevel(newStressLevel);
            currentTurn++;
        }

        /// <summary>
        /// Llamado cuando el avatar comienza a hablar.
        /// </summary>
        public void OnAvatarStartSpeaking()
        {
            SetSessionState(SessionState.AvatarSpeaking);
            SetMicrophoneState(MicrophoneState.Closed);
        }

        /// <summary>
        /// Llamado cuando el avatar termina de hablar.
        /// </summary>
        public void OnAvatarFinishedSpeaking()
        {
            if (!sessionEnded)
            {
                SetSessionState(SessionState.WaitingForUser);
                SetMicrophoneState(MicrophoneState.Open);
            }
        }

        #endregion
    }
}
