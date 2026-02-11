using UnityEngine;
using UnityEngine.UI;
using AvatarXR.Managers;

namespace AvatarXR.UI
{
    /// <summary>
    /// Controlador del HUD de micrófono. Muestra un icono en la esquina inferior izquierda
    /// que cambia de color según el estado del micrófono:
    /// - Verde: micrófono abierto (el usuario puede hablar)
    /// - Rojo: micrófono cerrado/silenciado
    /// - Amarillo: procesando audio
    /// Se posiciona en un Canvas Screen Space Overlay para que siempre esté visible.
    /// </summary>
    public class MicHUDController : MonoBehaviour
    {
        [Header("Referencias UI")]
        [SerializeField] private Image micIcon;
        [SerializeField] private Image ringGlow;

        [Header("Colores")]
        [SerializeField] private Color micOpenColor = new Color(0.2f, 0.85f, 0.3f, 1f);      // Verde
        [SerializeField] private Color micClosedColor = new Color(0.9f, 0.2f, 0.2f, 1f);      // Rojo
        [SerializeField] private Color micProcessingColor = new Color(0.95f, 0.75f, 0.1f, 1f); // Amarillo

        [Header("Animación")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinAlpha = 0.3f;
        [SerializeField] private float pulseMaxAlpha = 0.8f;

        private ConsultorioController.MicrophoneState currentState = ConsultorioController.MicrophoneState.Closed;
        private bool isPulsing = false;

        private void Start()
        {
            // Estado inicial: cerrado (rojo)
            SetState(ConsultorioController.MicrophoneState.Closed);
            
            // Buscar el ConsultorioController y suscribirse a cambios
            var consultorio = FindObjectOfType<ConsultorioController>();
            if (consultorio != null)
            {
                // El ConsultorioController ya llama a SetMicrophoneState,
                // ahora también actualizará este HUD
                Debug.Log("[MicHUD] Conectado al ConsultorioController.");
            }
        }

        /// <summary>
        /// Actualiza el estado visual del HUD del micrófono.
        /// Llamado desde ConsultorioController.SetMicrophoneState().
        /// </summary>
        public void SetState(ConsultorioController.MicrophoneState state)
        {
            currentState = state;
            Color targetColor;

            switch (state)
            {
                case ConsultorioController.MicrophoneState.Open:
                    targetColor = micOpenColor;
                    isPulsing = true;
                    break;
                case ConsultorioController.MicrophoneState.Processing:
                    targetColor = micProcessingColor;
                    isPulsing = true;
                    break;
                default: // Closed
                    targetColor = micClosedColor;
                    isPulsing = false;
                    break;
            }

            if (micIcon != null)
            {
                micIcon.color = targetColor;
            }

            if (ringGlow != null)
            {
                ringGlow.color = new Color(targetColor.r, targetColor.g, targetColor.b, pulseMaxAlpha);
            }
        }

        private void Update()
        {
            // Efecto de pulso en el anillo de brillo cuando el mic está activo
            if (isPulsing && ringGlow != null)
            {
                float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                    (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
                Color c = ringGlow.color;
                c.a = alpha;
                ringGlow.color = c;
            }
            else if (!isPulsing && ringGlow != null)
            {
                Color c = ringGlow.color;
                c.a = pulseMaxAlpha;
                ringGlow.color = c;
            }
        }
    }
}
