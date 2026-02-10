using UnityEngine;
using AvatarXR.Avatar;

public class VerifyAvatarSetup
{
    public static void Execute()
    {
         VerifyAvatar();
    }

    private static void VerifyAvatar()
    {
        GameObject avatar = GameObject.Find("Male_Adult_01");
        if (avatar == null)
        {
            Debug.LogError("❌ VerifyAvatar: Male_Adult_01 not found!");
            return;
        }

        Debug.Log("✅ VerifyAvatar: Avatar found.");

        RocketboxAvatarController controller = avatar.GetComponent<RocketboxAvatarController>();
        if (controller == null)
        {
            Debug.LogError("❌ VerifyAvatar: RocketboxAvatarController missing!");
        }
        else
        {
            Debug.Log("✅ VerifyAvatar: RocketboxAvatarController found.");
            // We can't really test runtime behavior (coroutines) in Editor script easily without Play Mode, 
            // but we can verify components are present.
        }

        AudioSource audio = avatar.GetComponent<AudioSource>();
        if (audio != null)
        {
            Debug.Log("✅ VerifyAvatar: AudioSource found.");
        }
        else 
        {
             Debug.LogError("❌ VerifyAvatar: AudioSource missing!");
        }
        
        // Check AvatarLoader reference
        AvatarLoader loader = Object.FindObjectOfType<AvatarLoader>();
        if (loader != null)
        {
            // Since we are in Editor mode and not Play mode, loadedAvatar might be null until Start() runs.
            // But we can check if the script logic we added (Find) will work.
            Debug.Log("✅ VerifyAvatar: AvatarLoader script present in scene.");
        }
    }
}
