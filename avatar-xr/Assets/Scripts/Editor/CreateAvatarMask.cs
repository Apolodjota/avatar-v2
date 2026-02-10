using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class CreateAvatarMask
{
    public static void Execute()
    {
        string maskPath = "Assets/Rocketbox/UpperBodyMask.mask";
        
        // 1. Find Avatar to get skeleton
        GameObject avatar = GameObject.Find("Male_Adult_01");
        if (avatar == null)
        {
            Debug.LogError("Male_Adult_01 not found in scene.");
            return;
        }

        AvatarMask mask = new AvatarMask();
        mask.transformCount = 0; 
        
        // Add transforms
        List<string> upperBodyKeywords = new List<string> { 
            "Spine", "Chest", "Neck", "Head", "Shoulder", "Arm", "Hand", "Finger" 
        };

        Transform[] transforms = avatar.GetComponentsInChildren<Transform>();
        List<string> transformPaths = new List<string>();

        foreach (Transform t in transforms)
        {
            if (t == avatar.transform) continue;
            
            string path = AnimationUtility.CalculateTransformPath(t, avatar.transform);
            bool isUpper = false;
            foreach(var kw in upperBodyKeywords)
            {
                if (t.name.Contains(kw))
                {
                    isUpper = true;
                    break;
                }
            }
            
            if (isUpper)
            {
               mask.transformCount++;
               mask.SetTransformPath(mask.transformCount - 1, path);
               mask.SetTransformActive(mask.transformCount - 1, true);
            }
        }
        
        // Ensure asset directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Rocketbox"))
        {
            AssetDatabase.CreateFolder("Assets", "Rocketbox");
        }

        AssetDatabase.CreateAsset(mask, maskPath);
        Debug.Log($"Created AvatarMask at {maskPath}");

        // 2. Assign to Controller
        string controllerPath = "Assets/Rocketbox/PatientAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
        {
            var layers = controller.layers;
            for(int i=0; i<layers.Length; i++)
            {
                if (layers[i].name == "UpperBody")
                {
                    layers[i].avatarMask = mask;
                    Debug.Log($"Assigned mask to UpperBody layer");
                    break;
                }
            }
            controller.layers = layers;
        }
    }
}
