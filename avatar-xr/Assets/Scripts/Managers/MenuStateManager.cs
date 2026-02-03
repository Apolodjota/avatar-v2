using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using System.Collections;

namespace AvatarXR.Managers
{
    /// <summary>
    /// Gestiona el estado de transición entre el menú principal y la sesión activa.
    /// Controla la locomotion e interacción del usuario hasta que inicie la sesión.
    /// </summary>
    public class MenuStateManager : MonoBehaviour
    {
        [Header("Referencias XR")]
        [SerializeField] private GameObject locomotionSystem;
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private Camera xrCamera;

        [Header("Referencias de Interactores")]
        [SerializeField] private NearFarInteractor leftNearFarInteractor;
        [SerializeField] private NearFarInteractor rightNearFarInteractor;
        [SerializeField] private GameObject leftTeleportInteractor;
        [SerializeField] private GameObject rightTeleportInteractor;

        [Header("Referencias UI")]
        [SerializeField] private GameObject mainMenuCanvas;
        [SerializeField] private CanvasGroup menuCanvasGroup;

        [Header("Configuración del Menú")]
        [SerializeField] private float menuDistanceFromPlayer = 2f;
        [SerializeField] private float menuHeightOffset = 0.2f;
        [SerializeField] private bool positionMenuOnStart = true;

        [Header("Layers")]
        [SerializeField] private LayerMask uiLayerMask = 1 << 5; // UI layer

        [Header("Bloqueo de Posición")]
        [SerializeField] private bool lockPositionDuringMenu = true;

        private bool isMenuActive = true;
        private Vector3 lockedPosition;
        private bool positionLocked = false;

        public bool IsMenuActive => isMenuActive;

        // Eventos
        public event System.Action OnMenuClosed;
        public event System.Action OnSessionStarted;

        private void Awake()
        {
            // Auto-buscar referencias si no están asignadas
            FindReferencesIfNeeded();
        }

        private void Start()
        {
            // Configurar estado inicial del menú
            SetMenuState(true);

            // Posicionar menú frente al jugador
            if (positionMenuOnStart && mainMenuCanvas != null)
            {
                StartCoroutine(PositionMenuInFrontOfPlayer());
            }
        }

        private void Update()
        {
            // Bloquear posición del XR Origin mientras el menú está activo
            if (isMenuActive && positionLocked && lockPositionDuringMenu && xrOrigin != null)
            {
                xrOrigin.position = lockedPosition;
            }
        }

        private void FindReferencesIfNeeded()
        {
            // Buscar XR Origin
            if (xrOrigin == null)
            {
                var xrOriginObj = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOriginObj != null)
                {
                    xrOrigin = xrOriginObj.transform;
                }
            }

            // Buscar cámara XR
            if (xrCamera == null && xrOrigin != null)
            {
                xrCamera = xrOrigin.GetComponentInChildren<Camera>();
            }

            // Buscar sistema de locomotion
            if (locomotionSystem == null && xrOrigin != null)
            {
                var locomotion = xrOrigin.Find("Locomotion");
                if (locomotion != null)
                {
                    locomotionSystem = locomotion.gameObject;
                }
            }

            // Buscar Near-Far Interactors
            if ((leftNearFarInteractor == null || rightNearFarInteractor == null) && xrOrigin != null)
            {
                var nearFarInteractors = xrOrigin.GetComponentsInChildren<NearFarInteractor>(true);
                foreach (var interactor in nearFarInteractors)
                {
                    if (interactor.handedness == InteractorHandedness.Left && leftNearFarInteractor == null)
                    {
                        leftNearFarInteractor = interactor;
                    }
                    else if (interactor.handedness == InteractorHandedness.Right && rightNearFarInteractor == null)
                    {
                        rightNearFarInteractor = interactor;
                    }
                }
            }

            // Buscar Teleport Interactors
            if ((leftTeleportInteractor == null || rightTeleportInteractor == null) && xrOrigin != null)
            {
                var leftController = xrOrigin.Find("Camera Offset/Left Controller/Teleport Interactor");
                var rightController = xrOrigin.Find("Camera Offset/Right Controller/Teleport Interactor");
                if (leftController != null) leftTeleportInteractor = leftController.gameObject;
                if (rightController != null) rightTeleportInteractor = rightController.gameObject;
            }

            // Buscar MainMenuCanvas
            if (mainMenuCanvas == null)
            {
                var canvas = GameObject.Find("MainMenuCanvas");
                if (canvas != null)
                {
                    mainMenuCanvas = canvas;
                    menuCanvasGroup = canvas.GetComponent<CanvasGroup>();
                }
            }
        }



        /// <summary>
        /// Establece el estado del menú (activo/inactivo).
        /// </summary>
        public void SetMenuState(bool menuActive)
        {
            isMenuActive = menuActive;

            // Controlar bloqueo de posición
            if (menuActive && lockPositionDuringMenu && xrOrigin != null)
            {
                lockedPosition = xrOrigin.position;
                positionLocked = true;
                Debug.Log($"[MenuStateManager] Posición bloqueada en {lockedPosition}");
            }
            else
            {
                positionLocked = false;
                Debug.Log("[MenuStateManager] Posición desbloqueada");
            }

            // Controlar locomotion
            if (locomotionSystem != null)
            {
                locomotionSystem.SetActive(!menuActive);
                Debug.Log($"[MenuStateManager] Locomotion {(menuActive ? "deshabilitada" : "habilitada")}");
            }

            // Controlar interacción - solo UI cuando menú activo
            SetInteractionMode(menuActive);

            // Mostrar/ocultar menú
            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.SetActive(menuActive);
            }

            Debug.Log($"[MenuStateManager] Estado del menú: {(menuActive ? "ACTIVO" : "INACTIVO")}");
        }

        private void SetInteractionMode(bool uiOnly)
        {
            // Deshabilitar teleport interactors durante el menú
            if (leftTeleportInteractor != null)
            {
                leftTeleportInteractor.SetActive(!uiOnly);
            }
            if (rightTeleportInteractor != null)
            {
                rightTeleportInteractor.SetActive(!uiOnly);
            }

            // Los Near-Far interactors pueden seguir activos para interactuar con UI
            // La UI ya está en el layer correcto y los interactores funcionan con ella
            Debug.Log($"[MenuStateManager] Teleport interactors {(uiOnly ? "deshabilitados" : "habilitados")}");
        }

        private IEnumerator PositionMenuInFrontOfPlayer()
        {
            // Esperar un frame para que XR se inicialice
            yield return null;
            yield return new WaitForEndOfFrame();

            if (xrCamera == null || mainMenuCanvas == null)
            {
                Debug.LogWarning("[MenuStateManager] No se puede posicionar el menú: falta cámara o canvas.");
                yield break;
            }

            // Obtener posición y orientación de la cámara
            Vector3 cameraPosition = xrCamera.transform.position;
            Vector3 cameraForward = xrCamera.transform.forward;
            
            // Proyectar forward en el plano horizontal
            cameraForward.y = 0;
            cameraForward.Normalize();

            // Calcular posición del menú
            Vector3 menuPosition = cameraPosition + cameraForward * menuDistanceFromPlayer;
            menuPosition.y = cameraPosition.y + menuHeightOffset;

            // Orientar el menú hacia el jugador
            Quaternion menuRotation = Quaternion.LookRotation(cameraForward, Vector3.up);

            // Aplicar transformación
            mainMenuCanvas.transform.position = menuPosition;
            mainMenuCanvas.transform.rotation = menuRotation;

            Debug.Log($"[MenuStateManager] Menú posicionado en {menuPosition}");
        }

        /// <summary>
        /// Llamado cuando el usuario presiona "Iniciar Sesión".
        /// Inicia la transición del menú a la sesión activa.
        /// </summary>
        public void StartSession()
        {
            StartCoroutine(StartSessionRoutine());
        }

        private IEnumerator StartSessionRoutine()
        {
            Debug.Log("[MenuStateManager] Iniciando transición a sesión...");

            // Fade out del menú (si hay CanvasGroup)
            if (menuCanvasGroup != null)
            {
                float fadeDuration = 0.5f;
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

            // Notificar cierre del menú
            OnMenuClosed?.Invoke();

            // Pequeña pausa
            yield return new WaitForSeconds(0.2f);

            // Cambiar estado - habilitar locomotion e interacción completa
            SetMenuState(false);

            // Notificar inicio de sesión
            OnSessionStarted?.Invoke();

            Debug.Log("[MenuStateManager] Transición completada. Sesión iniciada.");
        }

        /// <summary>
        /// Muestra el menú principal (para volver al menú desde pausa, etc.)
        /// </summary>
        public void ShowMenu()
        {
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 1f;
            }

            SetMenuState(true);
            StartCoroutine(PositionMenuInFrontOfPlayer());
        }
    }
}
