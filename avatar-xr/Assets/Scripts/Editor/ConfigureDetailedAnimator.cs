using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class ConfigureDetailedAnimator
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

        // 1. Ensure Layers & Parameters (Recap)
        CreateParameter(controller, "IsTalking", AnimatorControllerParameterType.Bool);
        
        // 2. Base Layer - Rigid Setup for Sitting
        var rootStateMachine = controller.layers[0].stateMachine;
        
        // Find or Create Sitting State
        var sittingState = FindState(rootStateMachine, "Sitting");
        if (sittingState == null)
        {
            sittingState = rootStateMachine.AddState("Sitting");
            // Try to find a Sitting clip
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip Sitting"); // Generic search
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:AnimationClip Idle"); // Fallback
            
            if (guids.Length > 0)
            {
                sittingState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        
        rootStateMachine.defaultState = sittingState;
        Debug.Log($"Set Default State to: {sittingState.name}");

        // 3. UpperBody - Talking Logic
        // We need a "Talking" state that is triggered when IsTalking = true
        // And goes back to Empty/Idle when IsTalking = false
        
        // Ensure UpperBody layer exists
        int upperLayerIndex = -1;
        for(int i=0; i<controller.layers.Length; i++)
        {
            if (controller.layers[i].name == "UpperBody") { upperLayerIndex = i; break; }
        }
        
        if (upperLayerIndex != -1)
        {
            var upperStateMachine = controller.layers[upperLayerIndex].stateMachine;
            var emptyState = FindState(upperStateMachine, "Empty");
            if (emptyState == null)
            {
                emptyState = upperStateMachine.AddState("Empty");
                upperStateMachine.defaultState = emptyState;
            }

            var talkingState = FindState(upperStateMachine, "Talking");
            if (talkingState == null)
            {
                talkingState = upperStateMachine.AddState("Talking");
                // Find a Talking clip
                string[] guids = AssetDatabase.FindAssets("t:AnimationClip Talking"); 
                if (guids.Length > 0)
                {
                    talkingState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            // Transitions
            // Empty -> Talking
            bool hasTransition = false;
            foreach(var t in emptyState.transitions) { if(t.destinationState == talkingState) hasTransition = true; }
            
            if (!hasTransition)
            {
                var trans = emptyState.AddTransition(talkingState);
                trans.AddCondition(AnimatorConditionMode.If, 0, "IsTalking");
                trans.duration = 0.2f;
            }

            // Talking -> Empty
            hasTransition = false;
            foreach(var t in talkingState.transitions) { if(t.destinationState == emptyState) hasTransition = true; }
            
            if (!hasTransition)
            {
                var trans = talkingState.AddTransition(emptyState);
                trans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTalking");
                trans.duration = 0.2f;
            }
             Debug.Log("Configured UpperBody Talking transitions");
        }
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach(var s in sm.states)
        {
            if (s.state.name == name) return s.state;
        }
        return null;
    }

    private static void CreateParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach(var p in controller.parameters) if(p.name == name) return;
        controller.AddParameter(name, type);
    }
}
