using UnityEngine;
using UnityEngine.SceneManagement;

namespace AvatarXR.Managers
{
    /// <summary>
    /// Singleton que gestiona el estado global del juego y la transición entre escenas.
    /// Persiste entre escenas usando DontDestroyOnLoad.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuración de Sesión")]
        [SerializeField] private SessionConfig sessionConfig;

        public SessionConfig CurrentSessionConfig => sessionConfig;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSessionConfig();
        }

        private void InitializeSessionConfig()
        {
            if (sessionConfig == null)
            {
                sessionConfig = new SessionConfig();
            }
        }

        /// <summary>
        /// Inicia la sesión de entrenamiento y carga la escena del consultorio.
        /// </summary>
        public void StartTrainingSession()
        {
            Debug.Log($"[GameManager] Iniciando sesión con config: Volume={sessionConfig.Volume}, StressBarVisible={sessionConfig.ShowStressBar}");
            SceneManager.LoadScene("Consultorio");
        }

        /// <summary>
        /// Regresa al menú principal.
        /// </summary>
        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Sale de la aplicación.
        /// </summary>
        public void QuitApplication()
        {
            Debug.Log("[GameManager] Saliendo de la aplicación...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>
    /// Configuración de la sesión de entrenamiento.
    /// </summary>
    [System.Serializable]
    public class SessionConfig
    {
        [Range(0f, 1f)]
        public float Volume = 0.75f;

        public bool ShowStressBar = true;
        
        [Range(0f, 1f)]
        public float InitialStress = 0.5f;

        public bool MicrophoneEnabled = false;

        public string SessionId;

        public SessionConfig()
        {
            SessionId = System.Guid.NewGuid().ToString();
            Volume = 0.75f;
            ShowStressBar = true;
            InitialStress = 0.5f;
            MicrophoneEnabled = false;
        }
    }
}
