using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupPatientAnimator : MonoBehaviour
{
    public static void Execute()
    {
        string controllerPath = "Assets/Rocketbox/PatientAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            Debug.LogError($"AnimatorController not found at {controllerPath}");
            return;
        }

        // 1. Add Parameters
        AddParameter(controller, "StressLevel", AnimatorControllerParameterType.Int);
        AddParameter(controller, "EmotionID", AnimatorControllerParameterType.Int);
        AddParameter(controller, "IsTalking", AnimatorControllerParameterType.Bool);

        // 2. Setup Layers
        // Ensure Base Layer exists
        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
        }

        // Add UpperBody Layer with Mask
        SetupLayer(controller, "UpperBody", true);
        
        // Add Face Layer
        SetupLayer(controller, "Face", false); // Face usually additive or override, depends on setup. Let's keep it simple override for now or additive? 
        // For Rocketbox, Face animations are usually blendshapes or bone transforms. Let's assume Override for now.

        Debug.Log($"✅ Configured PatientAnimator at {controllerPath}");
    }

    private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        bool exists = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == name)
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            controller.AddParameter(name, type);
            Debug.Log($"Added parameter: {name}");
        }
    }

    private static void SetupLayer(AnimatorController controller, string layerName, bool requireMask)
    {
        bool layerExists = false;
        int layerIndex = -1;

        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name == layerName)
            {
                layerExists = true;
                layerIndex = i;
                break;
            }
        }

        if (!layerExists)
        {
            controller.AddLayer(layerName);
            layerIndex = controller.layers.Length - 1;
            Debug.Log($"Created layer: {layerName}");
        }

        // Configure Layer
        var layers = controller.layers;
        var layer = layers[layerIndex];

        layer.defaultWeight = 1f;
        
        if (requireMask)
        {
            // Try to find an existing mask or create one? 
            // For now, let's look for a generic "UpperBodyMask" or create a basic one if possible (hard to create mask purely via script without defining bones).
            // We will skip mask creation for now and warn user to assign it.
            // But we can check if one exists in the project.
            string[] guids = AssetDatabase.FindAssets("t:AvatarMask UpperBody");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                layer.avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
                Debug.Log($"Assigned mask {path} to layer {layerName}");
            }
            else
            {
                 Debug.LogWarning($"⚠️ No 'UpperBody' AvatarMask found. Please create one and assign it to the '{layerName}' layer manually.");
            }
        }

        controller.layers = layers;
    }
}
