using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CheckAnimatorController
{
    public static void Execute()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/PlayerAnimator.controller");
        if (controller != null)
        {
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState state in rootStateMachine.states)
            {
                Debug.Log("State: " + state.state.name);
                if (state.state.motion != null)
                {
                    Debug.Log("  Motion: " + state.state.motion.name);
                    if (state.state.motion is AnimationClip clip)
                    {
                        Debug.Log("  Clip isLooping: " + clip.isLooping);
                    }
                }
            }
        }
    }
}
