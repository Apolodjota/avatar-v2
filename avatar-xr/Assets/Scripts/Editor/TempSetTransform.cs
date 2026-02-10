using UnityEngine;

public class TempSetTransform
{
    public static void Execute()
    {
        GameObject go = GameObject.Find("AvatarSpawnPoint");
        if (go != null)
        {
            go.transform.position = new Vector3(1.43f, 0f, 1.079413f);
            go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            Debug.Log($"AvatarSpawnPoint transform fixed: {go.transform.position}, {go.transform.rotation.eulerAngles}");
        }
        else
        {
            Debug.LogError("AvatarSpawnPoint not found.");
        }
    }
}
