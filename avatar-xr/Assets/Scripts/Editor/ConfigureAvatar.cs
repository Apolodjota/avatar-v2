using UnityEngine;
using UnityEditor;
using AvatarXR.Avatar;

public class ConfigureAvatar
{
    public static void Execute()
    {
        GameObject avatar = GameObject.Find("Male_Adult_01");
        if (avatar == null)
        {
            Debug.LogError("Male_Adult_01 not found in scene.");
            return;
        }

        // 1. Add RocketboxAvatarController
        RocketboxAvatarController controller = avatar.GetComponent<RocketboxAvatarController>();
        if (controller == null)
        {
            controller = avatar.AddComponent<RocketboxAvatarController>();
            Debug.Log("Added RocketboxAvatarController to Male_Adult_01");
        }

        // 2. Assign Animator Controller
        Animator animator = avatar.GetComponent<Animator>();
        if (animator != null)
        {
            // Load the controller from the path we saw in file search
            RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Rocketbox/PatientAnimator.controller");
            if (rac != null)
            {
                animator.runtimeAnimatorController = rac;
                Debug.Log($"Assigned Animator Controller: {rac.name}");
            }
            else
            {
                Debug.LogError("Could not load PatientAnimator.controller at Assets/Rocketbox/PatientAnimator.controller");
            }
        }
        else
        {
            Debug.LogError("Animator component missing on Male_Adult_01");
        }

        // 3. Ensure AudioSource
        AudioSource audio = avatar.GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = avatar.AddComponent<AudioSource>();
            audio.spatialBlend = 1.0f;
            Debug.Log("Added AudioSource to Male_Adult_01");
        }
    }
}
