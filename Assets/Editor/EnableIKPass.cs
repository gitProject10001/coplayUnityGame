using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class EnableIKPass
{
    public static void Execute()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/PlayerAnimator.controller");
        if (controller != null)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            if (layers.Length > 0)
            {
                layers[0].iKPass = true;
                controller.layers = layers;
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                Debug.Log("IK Pass enabled on Base Layer!");
            }
        }
        else
        {
            Debug.LogError("Could not find PlayerAnimator.controller");
        }
    }
}
