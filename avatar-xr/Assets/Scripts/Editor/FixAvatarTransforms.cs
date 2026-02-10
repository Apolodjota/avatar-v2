using UnityEngine;

public class FixAvatarTransforms
{
    public static void Execute()
    {
        GameObject avatar = GameObject.Find("Male_Adult_01");
        GameObject spawnPoint = GameObject.Find("AvatarSpawnPoint");

        if (spawnPoint != null)
        {
            // Fix SpawnPoint to be at floor level/seated height 
            // User requested: 1.43, 0, 1.079413. Y=0 is floor. 
            // If sitting, the avatar pivot (usually feet) should be at floor (0) relative to chair. 
            // If the chair is at Y=0, then avatar at Y=0 should check out. 
            // But if users says "clipping through floor", maybe it's too low? Or maybe 0 is correct and the floor is lower?
            // Let's ensure it's exactly at the requested coordinates.
            
            spawnPoint.transform.position = new Vector3(1.43f, 0f, 1.079413f);
            spawnPoint.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            Debug.Log($"Fixed AvatarSpawnPoint: {spawnPoint.transform.position}");
        }

        if (avatar != null)
        {
            if (spawnPoint != null)
            {
                avatar.transform.position = spawnPoint.transform.position;
                avatar.transform.rotation = spawnPoint.transform.rotation;
                Debug.Log($"Snapped Male_Adult_01 to SpawnPoint: {avatar.transform.position}");
            }
            else
            {
                // Fallback if no spawn point
                avatar.transform.position = new Vector3(1.43f, 0f, 1.079413f);
                avatar.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                 Debug.Log($"Fixed Male_Adult_01 (No SpawnPoint): {avatar.transform.position}");
            }
        }
    }
}
