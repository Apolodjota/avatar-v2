using UnityEngine;
using UnityEditor;

public class DebugBlendshapes
{
    public static void Execute()
    {
        // Find using transform search to also find inactive objects
        GameObject avatar = GameObject.Find("Male_Adult_01");
        if (avatar == null)
        {
            // Try FindObjectsOfType as fallback
            var allRenderers = Object.FindObjectsOfType<SkinnedMeshRenderer>(true);
            Debug.Log($"[DebugBlendshapes] Male_Adult_01 not found via Find. Total SkinnedMeshRenderers in scene: {allRenderers.Length}");
            foreach (var r in allRenderers)
            {
                Debug.Log($"[DebugBlendshapes] Found renderer: {r.gameObject.name} (path: {GetPath(r.transform)})");
            }
            return;
        }

        SkinnedMeshRenderer[] renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Debug.Log($"[DebugBlendshapes] Found {renderers.Length} SkinnedMeshRenderers on {avatar.name}");

        foreach (var renderer in renderers)
        {
            if (renderer.sharedMesh == null)
            {
                Debug.Log($"[DebugBlendshapes] Renderer: {renderer.name} - NO MESH ASSIGNED");
                continue;
            }

            int count = renderer.sharedMesh.blendShapeCount;
            Debug.Log($"[DebugBlendshapes] Renderer: {renderer.name}, Mesh: {renderer.sharedMesh.name}, BlendShapeCount: {count}");

            for (int i = 0; i < count; i++)
            {
                string shapeName = renderer.sharedMesh.GetBlendShapeName(i);
                Debug.Log($"[DebugBlendshapes]   [{i}] {shapeName}");
            }

            if (count == 0)
            {
                Debug.Log($"[DebugBlendshapes]   *** THIS MESH HAS ZERO BLENDSHAPES ***");
            }
        }
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
