using UnityEngine;
using UnityEditor;

public class FixAnimatorSetup
{
    public static void Execute()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        // Remove Animator from Player root (it should be on the model itself)
        Animator rootAnim = player.GetComponent<Animator>();
        if (rootAnim != null) Object.DestroyImmediate(rootAnim);

        // Remove the old X Bot instance
        Transform existingBot = player.transform.Find("X Bot");
        if (existingBot != null) Object.DestroyImmediate(existingBot.gameObject);

        // Re-instantiate X Bot now that it's properly set to Humanoid
        GameObject xBotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sword and Shield Pack/X Bot.fbx");
        if (xBotPrefab != null)
        {
            GameObject xBot = (GameObject)PrefabUtility.InstantiatePrefab(xBotPrefab);
            xBot.name = "X Bot";
            xBot.transform.SetParent(player.transform);
            xBot.transform.localPosition = new Vector3(0, -1f, 0);
            xBot.transform.localRotation = Quaternion.identity;
            
            // Assign the Animator Controller directly to the X Bot's Animator
            Animator botAnim = xBot.GetComponent<Animator>();
            if (botAnim == null) botAnim = xBot.AddComponent<Animator>();
            
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/PlayerAnimator.controller");
            botAnim.runtimeAnimatorController = controller;
        }
        
        Debug.Log("Animator setup fixed!");
    }
}
