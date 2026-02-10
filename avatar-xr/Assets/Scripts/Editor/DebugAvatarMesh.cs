using UnityEngine;
using UnityEditor;

public class DebugAvatarMesh
{
    public static void Execute()
    {
        GameObject avatar = GameObject.Find("Male_Adult_01");
        if (avatar == null)
        {
            Debug.LogError("Male_Adult_01 not found");
            return;
        }

        SkinnedMeshRenderer[] renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();
        Debug.Log($"Found {renderers.Length} SkinnedMeshRenderers on {avatar.name}");

        foreach (var renderer in renderers)
        {
            Debug.Log($"Renderer: {renderer.name}, Mesh: {renderer.sharedMesh.name}, Blendshapes: {renderer.sharedMesh.blendShapeCount}");
            for (int i = 0; i < Mathf.Min(5, renderer.sharedMesh.blendShapeCount); i++)
            {
                 Debug.Log($"  - Shape {i}: {renderer.sharedMesh.GetBlendShapeName(i)}");
            }
        }
    }
}
