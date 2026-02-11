using UnityEngine;
using System.Collections;

namespace AvatarXR.Avatar
{
    /// <summary>
    /// Locks the avatar's position to prevent sinking during animations.
    /// Uses SkinnedMeshRenderer bounds to auto-correct when the mesh goes below the floor.
    /// </summary>
    public class AvatarPositionLock : MonoBehaviour
    {
        [Tooltip("Lock the Y position every frame")]
        public bool lockY = true;

        [Tooltip("Also lock X and Z positions")]
        public bool lockXZ = false;

        [Tooltip("The floor Y level - mesh should not go below this")]
        public float floorY = 0f;

        [Tooltip("Minimum distance above floor for the mesh bottom")]
        public float floorOffset = 0.01f;

        private Vector3 lockedPosition;
        private float lockedY;
        private SkinnedMeshRenderer skinnedMesh;
        private bool initialized = false;

        private void Start()
        {
            skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            StartCoroutine(InitializeAfterAnimator());
        }

        /// <summary>
        /// Wait for the Animator to settle, then calculate the correct Y 
        /// based on where the mesh actually renders.
        /// </summary>
        private IEnumerator InitializeAfterAnimator()
        {
            // Wait for Animator to play the first frames of the idle/sitting animation
            yield return new WaitForSeconds(0.5f);
            
            CapturePosition();
            
            Debug.Log($"[AvatarPositionLock] Initialized: lockedY={lockedY:F4}, meshBottomY={GetMeshBottomY():F4}, transform.y={transform.position.y:F4}");
        }

        /// <summary>
        /// Captures and locks the current position. 
        /// Auto-adjusts Y so the mesh bottom is at or above the floor.
        /// </summary>
        public void CapturePosition()
        {
            // Auto-correct: if the mesh bottom is below the floor, push the transform up
            float meshBottom = GetMeshBottomY();
            float correction = 0f;
            
            if (meshBottom < floorY + floorOffset)
            {
                correction = (floorY + floorOffset) - meshBottom;
                Vector3 pos = transform.position;
                pos.y += correction;
                transform.position = pos;
                Debug.Log($"[AvatarPositionLock] Auto-corrected Y by +{correction:F4} (mesh was at {meshBottom:F4})");
            }

            lockedPosition = transform.position;
            lockedY = transform.position.y;
            initialized = true;
        }

        private float GetMeshBottomY()
        {
            if (skinnedMesh != null)
            {
                return skinnedMesh.bounds.min.y;
            }
            return transform.position.y;
        }

        private void LateUpdate()
        {
            if (!lockY || !initialized) return;

            Vector3 pos = transform.position;
            
            // Always force Y to locked value
            pos.y = lockedY;

            if (lockXZ)
            {
                pos.x = lockedPosition.x;
                pos.z = lockedPosition.z;
            }

            transform.position = pos;

            // Safety check: if mesh still sinks below floor, re-correct
            float meshBottom = GetMeshBottomY();
            if (meshBottom < floorY - 0.05f) // Tolerance
            {
                float correction = (floorY + floorOffset) - meshBottom;
                pos.y += correction;
                lockedY = pos.y;
                transform.position = pos;
            }
        }
    }
}
