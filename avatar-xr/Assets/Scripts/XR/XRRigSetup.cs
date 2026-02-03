using UnityEngine;

namespace AvatarXR.XR
{
    /// <summary>
    /// Configuración del OVRCameraRig para Meta Quest Pro.
    /// Gestiona la posición del usuario, tracking y configuración del rig VR.
    /// </summary>
    public class XRRigSetup : MonoBehaviour
    {
        [Header("Referencias del Rig")]
        [SerializeField] private OVRCameraRig ovrCameraRig;
        [SerializeField] private Transform centerEyeAnchor;
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;

        [Header("Configuración de Posición")]
        [SerializeField] private Transform userSpawnPoint;
        [SerializeField] private float userHeight = 1.7f;

        [Header("Tracking Settings")]
        [SerializeField] private OVRManager.TrackingOrigin trackingOrigin = OVRManager.TrackingOrigin.FloorLevel;

        [Header("Passthrough (Opcional)")]
        [SerializeField] private bool enablePassthrough = false;
        [SerializeField] private OVRPassthroughLayer passthroughLayer;

        [Header("Hand Tracking")]
        [SerializeField] private bool enableHandTracking = true;
        [SerializeField] private OVRHand leftHand;
        [SerializeField] private OVRHand rightHand;

        private void Awake()
        {
            ValidateReferences();
            ConfigureTracking();
        }

        private void Start()
        {
            PositionUser();
            ConfigurePassthrough();
            ConfigureHandTracking();
        }

        private void ValidateReferences()
        {
            if (ovrCameraRig == null)
            {
                ovrCameraRig = GetComponent<OVRCameraRig>();
                if (ovrCameraRig == null)
                {
                    ovrCameraRig = FindObjectOfType<OVRCameraRig>();
                }
            }

            if (ovrCameraRig == null)
            {
                Debug.LogError("[XRRigSetup] OVRCameraRig no encontrado. Asegúrate de añadir el prefab OVRCameraRig a la escena.");
                return;
            }

            // Auto-asignar anchors
            if (centerEyeAnchor == null)
            {
                centerEyeAnchor = ovrCameraRig.centerEyeAnchor;
            }

            if (leftHandAnchor == null)
            {
                leftHandAnchor = ovrCameraRig.leftHandAnchor;
            }

            if (rightHandAnchor == null)
            {
                rightHandAnchor = ovrCameraRig.rightHandAnchor;
            }
        }

        private void ConfigureTracking()
        {
            if (OVRManager.instance != null)
            {
                OVRManager.instance.trackingOriginType = trackingOrigin;
                Debug.Log($"[XRRigSetup] Tracking origin configurado: {trackingOrigin}");
            }
        }

        private void PositionUser()
        {
            if (userSpawnPoint != null && ovrCameraRig != null)
            {
                ovrCameraRig.transform.position = userSpawnPoint.position;
                ovrCameraRig.transform.rotation = userSpawnPoint.rotation;
                Debug.Log($"[XRRigSetup] Usuario posicionado en: {userSpawnPoint.position}");
            }
        }

        private void ConfigurePassthrough()
        {
            if (!enablePassthrough) return;

            if (passthroughLayer != null)
            {
                passthroughLayer.enabled = true;
                Debug.Log("[XRRigSetup] Passthrough habilitado");
            }
            else
            {
                Debug.LogWarning("[XRRigSetup] Passthrough solicitado pero OVRPassthroughLayer no asignado.");
            }
        }

        private void ConfigureHandTracking()
        {
            if (!enableHandTracking) return;

            // Hand tracking se configura principalmente desde el OVRManager
            // Este código es para configuración adicional si es necesario
            Debug.Log("[XRRigSetup] Hand tracking habilitado");
        }

        /// <summary>
        /// Obtiene la posición actual de la cabeza del usuario.
        /// </summary>
        public Vector3 GetHeadPosition()
        {
            return centerEyeAnchor != null ? centerEyeAnchor.position : Vector3.zero;
        }

        /// <summary>
        /// Obtiene la dirección hacia donde mira el usuario.
        /// </summary>
        public Vector3 GetHeadForward()
        {
            return centerEyeAnchor != null ? centerEyeAnchor.forward : Vector3.forward;
        }

        /// <summary>
        /// Recentra la vista del usuario.
        /// </summary>
        public void RecenterView()
        {
            OVRManager.display.RecenterPose();
            Debug.Log("[XRRigSetup] Vista recentrada");
        }

        /// <summary>
        /// Obtiene la posición de una mano específica.
        /// </summary>
        public Vector3 GetHandPosition(bool leftHand)
        {
            Transform hand = leftHand ? leftHandAnchor : rightHandAnchor;
            return hand != null ? hand.position : Vector3.zero;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Dibujar spawn point
            if (userSpawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(userSpawnPoint.position, 0.3f);
                Gizmos.DrawRay(userSpawnPoint.position, userSpawnPoint.forward * 1f);

                // Dibujar altura del usuario
                Gizmos.color = Color.green;
                Gizmos.DrawLine(userSpawnPoint.position, userSpawnPoint.position + Vector3.up * userHeight);
            }
        }
#endif
    }
}
