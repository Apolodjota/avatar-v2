using UnityEngine;

namespace AvatarXR.Avatar
{
    /// <summary>
    /// Locks the avatar's Y position to prevent sinking during animations.
    /// </summary>
    public class AvatarPositionLock : MonoBehaviour
    {
        [Tooltip("The Y position to maintain")]
        public float lockedY = 0.05f;

        [Tooltip("Lock the Y position every frame")]
        public bool lockY = true;

        [Tooltip("Also lock X and Z positions")]
        public bool lockXZ = false;

        private Vector3 lockedPosition;

        private void Start()
        {
            // Store initial position
            lockedPosition = transform.position;
            lockedPosition.y = lockedY;
        }

        private void Update()
        {
            // Lock in Update (before animations)
            if (lockY)
            {
                Vector3 pos = transform.position;
                pos.y = lockedY;
                if (lockXZ)
                {
                    pos.x = lockedPosition.x;
                    pos.z = lockedPosition.z;
                }
                transform.position = pos;
            }
        }

        private void LateUpdate()
        {
            // Lock in LateUpdate (after animations)
            if (lockY)
            {
                Vector3 pos = transform.position;
                if (Mathf.Abs(pos.y - lockedY) > 0.001f)
                {
                    Debug.LogWarning($"[AvatarPositionLock] Correcting Y from {pos.y:F3} to {lockedY:F3}");
                    pos.y = lockedY;
                    transform.position = pos;
                }
            }
        }
    }
}
