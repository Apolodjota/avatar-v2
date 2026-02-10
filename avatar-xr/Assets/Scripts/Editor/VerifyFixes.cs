using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class VerifyFixes
{
    public static void Execute()
    {
        // 1. Verify Animator
        string controllerPath = "Assets/Rocketbox/PatientAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
        {
            Debug.Log($"[VerifyFixes] Layers count: {controller.layers.Length}");
            foreach(var layer in controller.layers)
            {
                Debug.Log($"[VerifyFixes] Layer: {layer.name}, Mask: {(layer.avatarMask != null ? layer.avatarMask.name : "None")}");
            }
            Debug.Log($"[VerifyFixes] Parameters count: {controller.parameters.Length}");
            foreach(var param in controller.parameters)
            {
                Debug.Log($"[VerifyFixes] Parameter: {param.name} ({param.type})");
            }
        }
        else
        {
            Debug.LogError($"[VerifyFixes] Controller not found at {controllerPath}");
        }

        // 2. Verify Position
        GameObject spawnPoint = GameObject.Find("AvatarSpawnPoint");
        if (spawnPoint != null)
        {
            Debug.Log($"[VerifyFixes] SpawnPoint Position: {spawnPoint.transform.position}");
            if (Mathf.Abs(spawnPoint.transform.position.y) < 0.01f)
            {
                Debug.Log("[VerifyFixes] ✅ SpawnPoint Y is ~0");
            }
            else
            {
                Debug.LogWarning($"[VerifyFixes] ⚠️ SpawnPoint Y is {spawnPoint.transform.position.y}, expected ~0");
            }
        }
    }
}
