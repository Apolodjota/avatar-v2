using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

using System.IO;

[InitializeOnLoad]
public class RocketboxSetup : MonoBehaviour
{
    static RocketboxSetup()
    {
        EditorApplication.delayCall += CheckAndCreate;
    }

    private static void CheckAndCreate()
    {
        if (!File.Exists("Assets/Rocketbox/PatientAnimator.controller"))
        {
            CreateAnimator();
        }
        SetupPrefabAndScene();
    }

    private static void SetupPrefabAndScene()
    {
        // 1. Find FBX
        string fbxPath = "Assets/Rocketbox/Male_Adult_01.fbx";
        if (!File.Exists(fbxPath)) return;

        // 2. Load Controller
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Rocketbox/PatientAnimator.controller");
        if (controller == null) return;

        // 3. Create/Update Prefab
        string prefabPath = "Assets/Rocketbox/RocketboxPatient.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                Animator anim = instance.GetComponent<Animator>();
                if (anim == null) anim = instance.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                
                // Add RocketboxAvatarController
                if (instance.GetComponent<AvatarXR.Avatar.RocketboxAvatarController>() == null)
                    instance.AddComponent<AvatarXR.Avatar.RocketboxAvatarController>();

                prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                Debug.Log("[RocketboxSetup] Created RocketboxPatient.prefab with Animator Controller.");
            }
        }

        // 4. Assign to Scene (SessionManager)
        AssignToScene(prefab);
    }

    private static void AssignToScene(GameObject prefab)
    {
        GameObject sessionManager = GameObject.Find("SessionManager");
        if (sessionManager != null)
        {
            var loader = sessionManager.GetComponent<AvatarXR.Avatar.AvatarLoader>();
            if (loader != null)
            {
                SerializedObject so = new SerializedObject(loader);
                SerializedProperty prop = so.FindProperty("avatarPrefab");
                if (prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = prefab;
                    so.ApplyModifiedProperties();
                    Debug.Log("[RocketboxSetup] Assigned RocketboxPatient prefab to SessionManager.");
                }
            }
        }
    }

    [MenuItem("Rocketbox/Setup Animator Controller")]
    public static void CreateAnimator()
    {
        string controllerPath = "Assets/Rocketbox/PatientAnimator.controller";
        
        // Ensure directory exists
        if (!Directory.Exists("Assets/Rocketbox"))
        {
            Directory.CreateDirectory("Assets/Rocketbox");
        }

        // Create Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Add Parameters
        controller.AddParameter("StressLevel", AnimatorControllerParameterType.Int); // 0-10
        controller.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
        
        // Add Layers
        AnimatorControllerLayer baseLayer = controller.layers[0];
        AnimatorStateMachine stateMachine = baseLayer.stateMachine;

        // Find Clips
        AnimationClip idleClip = FindClip("Sitting Disbelief"); // Calm/Neutral
        AnimationClip sadClip = FindClip("Sad Idle"); // Sad
        AnimationClip stressClip = FindClip("Sitting anxious"); // Stress
        AnimationClip talkClip = FindClip("Sitting Talking"); // Talk

        if (idleClip == null) Debug.LogWarning("Idle clip 'Sitting Disbelief' not found. Check Assets/Mixamo.");
        if (sadClip == null) Debug.LogWarning("Sad clip 'Sad Idle' not found.");
        if (stressClip == null) Debug.LogWarning("Stress clip 'Sitting anxious' not found.");
        if (talkClip == null) Debug.LogWarning("Talk clip 'Sitting Talking' not found.");

        // Create States
        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;
        
        AnimatorState sadState = stateMachine.AddState("Sad");
        sadState.motion = sadClip;

        AnimatorState stressState = stateMachine.AddState("Stress");
        stressState.motion = stressClip;
        
        AnimatorState talkState = stateMachine.AddState("Talking");
        talkState.motion = talkClip;

        // Transitions
        // Any State -> Talking (when speaking)
        var anyToTalk = stateMachine.AddAnyStateTransition(talkState);
        anyToTalk.AddCondition(AnimatorConditionMode.If, 0, "IsTalking");
        anyToTalk.duration = 0.2f;

        // Talking -> Exit/Back (managed by returning to emotion state)
        // Actually, AnyState transitions can be tricky.
        // Let's use a BlendTree or simple transitions from Idle.
        // For simplicity: A central "Hub" or just logic based on StressLevel.

        // Logic:
        // If IsTalking -> Talking Loop
        // If !IsTalking -> Check StressLevel
        // Stress < 4: Idle/Calm
        // Stress >= 4 && Stress < 8: Sad? Or just Anxious?
        // Actually, "Sad" might be a specific emotion, not just stress level.
        // But the current system uses "StressLevel" (int).
        // Let's map StressLevel:
        // 0-3: Idle (Calm)
        // 4-7: Sad (Moderate distress?) -> User said "Sad" is one of them.
        // 8-10: Stress (Anxious)

        // Talking -> Exit to Idle (and then immediate transition to correct state?)
        var talkToExit = talkState.AddTransition(idleState);
        talkToExit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTalking");

        // Idle -> Stress (High Stress)
        var idleToStress = idleState.AddTransition(stressState);
        idleToStress.AddCondition(AnimatorConditionMode.Greater, 7, "StressLevel");
        idleToStress.duration = 0.5f;

        var stressToIdle = stressState.AddTransition(idleState);
        stressToIdle.AddCondition(AnimatorConditionMode.Less, 8, "StressLevel");
        stressToIdle.duration = 0.5f;

        // Idle -> Sad (Medium Stress?) - Let's use 4-7 range for Sad/Concerned
        /* 
           This logic implies specific stress levels map to animations.
           Let's simplify: 
           Stress 0-4: Idle
           Stress 5-10: Anxious
           Where does "Sad" fit? Maybe I need an "Emotion" parameter separate from Stress.
           The NetworkManager returns "user_emotion".
           But the controller mostly uses StressLevel integer.
           I'll add "EmotionID" parameter to be safe.
           0: Neutral/Calm
           1: Sad
           2: Stress/Anxious
        */
        controller.AddParameter("EmotionID", AnimatorControllerParameterType.Int);

        // Re-do Transitions based on EmotionID
        
        // Clear old transitions from Idle (keeping AnyState -> Talk)
        idleState.transitions = new AnimatorStateTransition[0];
        
        // Idle is Default.
        // Idle -> Sad
        var idleToSad = idleState.AddTransition(sadState);
        idleToSad.AddCondition(AnimatorConditionMode.Equals, 1, "EmotionID");
        
        // Idle -> Stress
        var idleToStress2 = idleState.AddTransition(stressState);
        idleToStress2.AddCondition(AnimatorConditionMode.Equals, 2, "EmotionID");

        // Sad -> Idle
        var sadToIdle = sadState.AddTransition(idleState);
        sadToIdle.AddCondition(AnimatorConditionMode.NotEqual, 1, "EmotionID");
        
        // Stress -> Idle
        var stressToIdle2 = stressState.AddTransition(idleState);
        stressToIdle2.AddCondition(AnimatorConditionMode.NotEqual, 2, "EmotionID");

        Debug.Log($"[RocketboxSetup] Animator Controller created at {controllerPath}");
    }

    private static AnimationClip FindClip(string partialName)
    {
        // 1. Busqueda directa de archivos .anim
        string[] guidAnims = AssetDatabase.FindAssets("t:AnimationClip " + partialName);
        foreach (string guid in guidAnims)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Mixamo"))
            {
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }
        }

        // 2. Busqueda en FBX (Mixamo suele ser .fbx)
        // Buscamos Modelos que contengan el nombre en el nombre de archivo
        string[] guidModels = AssetDatabase.FindAssets("t:Model " + partialName);
        foreach (string guid in guidModels)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Mixamo") || path.Contains("Rocketbox")) 
            {
                // Cargar todos los sub-activos del FBX
                Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach(var o in objs)
                {
                    if(o is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        return clip;
                    }
                }
            }
        }
        
        // 3. Fallback: buscar manualmente en carpeta Mixamo si FindAssets falla por nombre parcial
        if (Directory.Exists("Assets/Mixamo"))
        {
             string[] files = Directory.GetFiles("Assets/Mixamo", "*.fbx", SearchOption.AllDirectories);
             foreach(string file in files)
             {
                 if (file.Contains(partialName))
                 {
                     string assetPath = file.Replace("\\", "/");
                     Object[] objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                     foreach(var o in objs)
                     {
                         if(o is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                         {
                             return clip;
                         }
                     }
                 }
             }
        }

        return null;
    }
}
