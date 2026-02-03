using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace AvatarXR.UI
{
    /// <summary>
    /// Controlador del menú principal en VR.
    /// Gestiona la configuración de la sesión antes de iniciar el entrenamiento.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Referencias UI")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle stressBarToggle;
        [SerializeField] private Image microphoneIndicator;
        [SerializeField] private Button startSessionButton;
        [SerializeField] private TextMeshProUGUI volumeValueText;
        [SerializeField] private Slider initialStressSlider;
        [SerializeField] private TextMeshProUGUI initialStressValueText;
        [SerializeField] private TextMeshProUGUI microphoneStatusText;

        [Header("Colores del Indicador de Micrófono")]
        [SerializeField] private Color micActiveColor = new Color(0.2f, 0.8f, 0.2f, 1f);    // Verde
        [SerializeField] private Color micInactiveColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Rojo
        [SerializeField] private Color micProcessingColor = new Color(0.9f, 0.7f, 0.1f, 1f); // Amarillo

        [Header("Fade Settings")]
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip toggleSound;

        [Header("Referencias Externas")]
        [SerializeField] private Managers.MenuStateManager menuStateManager;

        private bool isMicrophoneAvailable = false;
        private Coroutine micCheckCoroutine;

        private void Start()
        {
            InitializeUI();
            StartMicrophoneCheck();
            FindMenuStateManager();
        }

        private void FindMenuStateManager()
        {
            if (menuStateManager == null)
            {
                menuStateManager = FindObjectOfType<Managers.MenuStateManager>();
            }
        }

        private void OnDestroy()
        {
            if (micCheckCoroutine != null)
            {
                StopCoroutine(micCheckCoroutine);
            }
        }

        private void InitializeUI()
        {
            // Configurar slider de volumen
            if (volumeSlider != null)
            {
                volumeSlider.value = 0.75f;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                UpdateVolumeText(volumeSlider.value);
            }

            // Configurar toggle de barra de estrés
            if (stressBarToggle != null)
            {
                stressBarToggle.isOn = true;
                stressBarToggle.onValueChanged.AddListener(OnStressBarToggleChanged);
            }

            // Configurar slider de estrés inicial
            if (initialStressSlider != null)
            {
                initialStressSlider.value = 0.5f;
                initialStressSlider.onValueChanged.AddListener(OnInitialStressChanged);
                UpdateInitialStressText(initialStressSlider.value);
            }

            // Configurar botón de inicio
            if (startSessionButton != null)
            {
                startSessionButton.onClick.AddListener(OnStartSessionClicked);
                startSessionButton.interactable = false; // Deshabilitado hasta verificar micrófono
            }

            // Estado inicial del micrófono
            UpdateMicrophoneIndicator(false);
        }

        private void StartMicrophoneCheck()
        {
            micCheckCoroutine = StartCoroutine(CheckMicrophoneRoutine());
        }

        private IEnumerator CheckMicrophoneRoutine()
        {
            // Pequeña espera inicial
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                bool micAvailable = CheckMicrophoneAvailability();
                
                if (micAvailable != isMicrophoneAvailable)
                {
                    isMicrophoneAvailable = micAvailable;
                    UpdateMicrophoneIndicator(isMicrophoneAvailable);
                    UpdateStartButtonState();
                }

                yield return new WaitForSeconds(1f); // Verificar cada segundo
            }
        }

        private bool CheckMicrophoneAvailability()
        {
            // Verificar si hay dispositivos de micrófono disponibles
            string[] devices = Microphone.devices;
            
            if (devices.Length == 0)
            {
                Debug.LogWarning("[MainMenu] No se detectaron dispositivos de micrófono.");
                return false;
            }

            Debug.Log($"[MainMenu] Micrófono detectado: {devices[0]}");
            return true;
        }

        private void UpdateMicrophoneIndicator(bool isActive)
        {
            if (microphoneIndicator != null)
            {
                microphoneIndicator.color = isActive ? micActiveColor : micInactiveColor;
            }

            if (microphoneStatusText != null)
            {
                microphoneStatusText.text = isActive ? "Micrófono: Listo" : "Micrófono: No detectado";
                microphoneStatusText.color = isActive ? micActiveColor : micInactiveColor;
            }
        }

        private void UpdateStartButtonState()
        {
            if (startSessionButton != null)
            {
                startSessionButton.interactable = isMicrophoneAvailable;
            }
        }

        private void OnVolumeChanged(float value)
        {
            UpdateVolumeText(value);
            
            // Actualizar configuración en GameManager
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.CurrentSessionConfig.Volume = value;
            }

            // Aplicar volumen global
            AudioListener.volume = value;

            PlayUISound(toggleSound);
        }

        private void UpdateVolumeText(float value)
        {
            if (volumeValueText != null)
            {
                volumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void OnStressBarToggleChanged(bool isOn)
        {
            Debug.Log($"[MainMenu] Barra de estrés visible: {isOn}");
            
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.CurrentSessionConfig.ShowStressBar = isOn;
            }

            PlayUISound(toggleSound);
        }

        private void OnInitialStressChanged(float value)
        {
            UpdateInitialStressText(value);
            
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.CurrentSessionConfig.InitialStress = value;
            }
        }

        private void UpdateInitialStressText(float value)
        {
            if (initialStressValueText != null)
            {
                initialStressValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void OnStartSessionClicked()
        {
            if (!isMicrophoneAvailable)
            {
                Debug.LogWarning("[MainMenu] No se puede iniciar sin micrófono funcional.");
                ShowMicrophoneError();
                return;
            }

            PlayUISound(buttonClickSound);
            StartCoroutine(StartSessionRoutine());
        }

        private IEnumerator StartSessionRoutine()
        {
            Debug.Log("[MainMenu] Iniciando transición a sesión de entrenamiento...");

            // Deshabilitar interacción
            if (startSessionButton != null)
            {
                startSessionButton.interactable = false;
            }

            // Fade out del menú
            if (menuCanvasGroup != null)
            {
                yield return StartCoroutine(FadeOutMenu());
            }

            // Usar MenuStateManager para la transición si está disponible
            if (menuStateManager != null)
            {
                // El MenuStateManager manejará el fade, locomotion y la transición
                menuStateManager.StartSession();
                yield break;
            }

            // Fallback: comportamiento original si no hay MenuStateManager
            // Ocultar el menú completamente
            gameObject.SetActive(false);

            // Pequeña pausa
            yield return new WaitForSeconds(0.3f);

            // Notificar al ConsultorioController para iniciar sesión (si existe en la misma escena)
            var consultorioController = FindObjectOfType<AvatarXR.Managers.ConsultorioController>();
            if (consultorioController != null)
            {
                Debug.Log("[MainMenu] Iniciando sesión en ConsultorioController...");
                consultorioController.StartSession();
            }
            else if (Managers.GameManager.Instance != null)
            {
                // Fallback: usar GameManager si no hay ConsultorioController
                Managers.GameManager.Instance.CurrentSessionConfig.MicrophoneEnabled = true;
                Managers.GameManager.Instance.StartTrainingSession();
            }
            else
            {
                Debug.LogError("[MainMenu] No se encontró ConsultorioController ni GameManager.");
            }
        }

        private IEnumerator FadeOutMenu()
        {
            float elapsed = 0f;
            float startAlpha = menuCanvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }

            menuCanvasGroup.alpha = 0f;
        }

        private void ShowMicrophoneError()
        {
            // Parpadear el indicador de micrófono para llamar la atención
            StartCoroutine(BlinkMicrophoneIndicator());
        }

        private IEnumerator BlinkMicrophoneIndicator()
        {
            if (microphoneIndicator == null) yield break;

            for (int i = 0; i < 3; i++)
            {
                microphoneIndicator.color = Color.white;
                yield return new WaitForSeconds(0.15f);
                microphoneIndicator.color = micInactiveColor;
                yield return new WaitForSeconds(0.15f);
            }
        }

        private void PlayUISound(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Método público para establecer el indicador de procesamiento del micrófono.
        /// </summary>
        public void SetMicrophoneProcessing(bool isProcessing)
        {
            if (microphoneIndicator != null)
            {
                microphoneIndicator.color = isProcessing ? micProcessingColor : 
                    (isMicrophoneAvailable ? micActiveColor : micInactiveColor);
            }
        }
    }
}
