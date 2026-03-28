using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CheckAnimatorTransitions
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
                foreach (AnimatorStateTransition transition in state.state.transitions)
                {
                    Debug.Log("  Transition to: " + (transition.destinationState != null ? transition.destinationState.name : "Exit"));
                    Debug.Log("    Has Exit Time: " + transition.hasExitTime);
                    Debug.Log("    Exit Time: " + transition.exitTime);
                    Debug.Log("    Duration: " + transition.duration);
                }
            }
        }
    }
}
