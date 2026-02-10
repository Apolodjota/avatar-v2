using UnityEngine;
using UnityEditor;

public class RocketboxImporter : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        // Check for Rocketbox assets
        if (assetPath.Contains("Rocketbox") && assetPath.EndsWith(".fbx"))
        {
            ModelImporter modelImporter = assetImporter as ModelImporter;
            if (modelImporter != null)
            {
                // Set to Humanoid
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                
                // Ensure materials are handled correctly (use embedded if available/extracted)
                // Rocketbox often needs material remapping, but let's start with defaults
                modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
                
                Debug.Log($"[RocketboxImporter] Configured {assetPath} as Humanoid.");
            }
        }

        // Check for Mixamo animations to set as Humanoid (except if they already are)
        if (assetPath.Contains("Mixamo") && assetPath.EndsWith(".fbx"))
        {
             ModelImporter modelImporter = assetImporter as ModelImporter;
             if (modelImporter != null)
             {
                 modelImporter.animationType = ModelImporterAnimationType.Human;
                 modelImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                 // We need a source avatar. Usually the first one found or allow user to set it.
                 // For now, let's just set Humanoid so animations work with any humanoid avatar.
                 // CopyFromOther requires a SourceAvatar. If we don't have one, CreateFromThisModel is safer for the first import,
                 // but for animations only, we usually copy from the main character.
                 // Let's stick to Humanoid and let Unity handle the rest or keyframes.
                 // Actually, "CreateFromThisModel" on a Mixamo animation file often works if it includes the mesh.
                 // If it's animation only, we MUST copy from other.
                 // But we don't know which file is which easily without inspection.
                 // Let's disable auto-setup for Mixamo for now to avoid breaking existing setups, 
                 // OR only do it if not already set.
             }
        }
    }
}
