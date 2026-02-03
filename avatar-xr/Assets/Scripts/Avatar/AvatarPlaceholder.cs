using UnityEngine;

namespace AvatarXR.Avatar
{
    /// <summary>
    /// Placeholder temporal para el avatar paciente.
    /// Será reemplazado por el avatar de Ready Player Me en la implementación completa.
    /// </summary>
    public class AvatarPlaceholder : MonoBehaviour
    {
        [Header("Configuración de Posición")]
        [SerializeField] private Transform seatPosition;
        [SerializeField] private Transform lookAtTarget; // Donde mira el avatar (hacia el usuario)
        [SerializeField] private float distanceFromUser = 1.5f;

        [Header("Visualización del Placeholder")]
        [SerializeField] private GameObject placeholderVisual;
        [SerializeField] private Material placeholderMaterial;

        [Header("Puntos de Referencia")]
        [SerializeField] private Transform headPosition;
        [SerializeField] private Transform chestPosition;

        [Header("Estado Emocional (Debug)")]
        [SerializeField] private int currentStressLevel = 7;
        [SerializeField] private string currentEmotion = "ansioso";

        [Header("Audio")]
        [SerializeField] private AudioSource voiceAudioSource;

        private void Start()
        {
            InitializePlaceholder();
        }

        private void InitializePlaceholder()
        {
            // Posicionar el avatar en el asiento
            if (seatPosition != null)
            {
                transform.position = seatPosition.position;
                transform.rotation = seatPosition.rotation;
            }

            // Crear visual placeholder si no existe
            if (placeholderVisual == null)
            {
                CreatePlaceholderVisual();
            }

            Debug.Log("[AvatarPlaceholder] Avatar placeholder inicializado. Reemplazar con Ready Player Me avatar.");
        }

        private void CreatePlaceholderVisual()
        {
            // Crear un cilindro como placeholder temporal
            placeholderVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholderVisual.name = "AvatarPlaceholder_Body";
            placeholderVisual.transform.SetParent(transform);
            placeholderVisual.transform.localPosition = new Vector3(0, 0.9f, 0);
            placeholderVisual.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);

            // Crear esfera para la cabeza
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "AvatarPlaceholder_Head";
            head.transform.SetParent(transform);
            head.transform.localPosition = new Vector3(0, 1.6f, 0);
            head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // Asignar material si existe
            if (placeholderMaterial != null)
            {
                placeholderVisual.GetComponent<Renderer>().material = placeholderMaterial;
                head.GetComponent<Renderer>().material = placeholderMaterial;
            }
            else
            {
                // Material por defecto azul semi-transparente
                Material defaultMat = new Material(Shader.Find("Standard"));
                defaultMat.color = new Color(0.3f, 0.5f, 0.8f, 0.7f);
                defaultMat.SetFloat("_Mode", 3); // Transparent
                defaultMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                defaultMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                defaultMat.EnableKeyword("_ALPHABLEND_ON");
                defaultMat.renderQueue = 3000;

                placeholderVisual.GetComponent<Renderer>().material = defaultMat;
                head.GetComponent<Renderer>().material = defaultMat;
            }

            // Guardar referencia a la cabeza
            headPosition = head.transform;

            // Desactivar colliders del placeholder
            Destroy(placeholderVisual.GetComponent<Collider>());
            Destroy(head.GetComponent<Collider>());
        }

        private void Update()
        {
            // Mirar hacia el target (usuario) si está definido
            if (lookAtTarget != null && headPosition != null)
            {
                Vector3 lookDirection = lookAtTarget.position - headPosition.position;
                lookDirection.y = 0; // Mantener nivel horizontal
                
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    headPosition.rotation = Quaternion.Slerp(headPosition.rotation, targetRotation, Time.deltaTime * 2f);
                }
            }
        }

        /// <summary>
        /// Actualiza el estado emocional del avatar (para debug).
        /// </summary>
        public void SetEmotionalState(int stressLevel, string emotion)
        {
            currentStressLevel = stressLevel;
            currentEmotion = emotion;

            // Cambiar color del placeholder según el estrés
            UpdatePlaceholderColor();

            Debug.Log($"[AvatarPlaceholder] Estado actualizado - Estrés: {stressLevel}, Emoción: {emotion}");
        }

        private void UpdatePlaceholderColor()
        {
            if (placeholderVisual == null) return;

            Color stressColor;
            if (currentStressLevel <= 3)
            {
                stressColor = new Color(0.3f, 0.8f, 0.3f, 0.7f); // Verde
            }
            else if (currentStressLevel <= 6)
            {
                stressColor = new Color(0.8f, 0.8f, 0.3f, 0.7f); // Amarillo
            }
            else if (currentStressLevel <= 8)
            {
                stressColor = new Color(0.8f, 0.5f, 0.3f, 0.7f); // Naranja
            }
            else
            {
                stressColor = new Color(0.8f, 0.3f, 0.3f, 0.7f); // Rojo
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = stressColor;
                }
            }
        }

        /// <summary>
        /// Reproduce audio de voz del avatar.
        /// </summary>
        public void PlayVoice(AudioClip voiceClip)
        {
            if (voiceAudioSource != null && voiceClip != null)
            {
                voiceAudioSource.clip = voiceClip;
                voiceAudioSource.Play();
            }
        }

        /// <summary>
        /// Detiene el audio de voz.
        /// </summary>
        public void StopVoice()
        {
            if (voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
            }
        }

        /// <summary>
        /// Verifica si el avatar está hablando.
        /// </summary>
        public bool IsSpeaking()
        {
            return voiceAudioSource != null && voiceAudioSource.isPlaying;
        }

        /// <summary>
        /// Configura el target hacia donde debe mirar el avatar.
        /// </summary>
        public void SetLookAtTarget(Transform target)
        {
            lookAtTarget = target;
        }

        /// <summary>
        /// Obtiene la posición de la cabeza del avatar.
        /// </summary>
        public Vector3 GetHeadPosition()
        {
            return headPosition != null ? headPosition.position : transform.position + Vector3.up * 1.6f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Dibujar posición del avatar
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.6f, 0.15f); // Cabeza
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.9f, new Vector3(0.4f, 1.2f, 0.3f)); // Cuerpo

            // Dibujar dirección de mirada
            if (lookAtTarget != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 headPos = transform.position + Vector3.up * 1.6f;
                Gizmos.DrawLine(headPos, lookAtTarget.position);
            }

            // Dibujar distancia ideal del usuario
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + transform.forward * distanceFromUser, 0.2f);
        }
#endif
    }
}
