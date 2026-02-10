using UnityEngine;

namespace AvatarXR.Avatar
{
    /// <summary>
    /// Controls the Microsoft Rocketbox avatar animations via Animator.
    /// Handles stress levels and talking states.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class RocketboxAvatarController : MonoBehaviour
    {
        private Animator animator;
        private int currentStress = 0;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        /// <summary>
        /// Updates the emotion based on stress level (0-10).
        /// </summary>
        public void SetStressLevel(int stress)
        {
            currentStress = Mathf.Clamp(stress, 0, 10);
            
            // Map stress to EmotionID for the Animator
            // 0-3: Idle (Calm) -> ID 0
            // 4-6: Sad (Concerned/Moderate) -> ID 1
            // 7-10: Stress (Anxious) -> ID 2
            
            int emotionId = 0;
            if (currentStress >= 7) emotionId = 2;
            else if (currentStress >= 4) emotionId = 1;
            else emotionId = 0;

            if (animator != null)
            {
                animator.SetInteger("EmotionID", emotionId);
                animator.SetInteger("StressLevel", currentStress);
            }
        }

        /// <summary>
        /// Sets the talking state.
        /// </summary>
        public void SetTalking(bool isTalking)
        {
            if (animator != null)
            {
                animator.SetBool("IsTalking", isTalking);
            }
        }
    }
}
