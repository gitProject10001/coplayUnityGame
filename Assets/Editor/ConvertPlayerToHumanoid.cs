using UnityEngine;
using UnityEditor;

public class ConvertPlayerToHumanoid
{
    public static void Execute()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        // Remove existing mesh components from the root so it's just an invisible collider
        MeshRenderer mr = player.GetComponent<MeshRenderer>();
        if (mr != null) Object.DestroyImmediate(mr);
        MeshFilter mf = player.GetComponent<MeshFilter>();
        if (mf != null) Object.DestroyImmediate(mf);

        // Clear existing children if any (to avoid duplicates if run multiple times)
        for (int i = player.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(player.transform.GetChild(i).gameObject);
        }

        // Create material for the player
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material playerMat = new Material(shader);
        playerMat.color = new Color(0.2f, 0.5f, 0.8f); // Blueish

        // Helper to create body parts
        GameObject CreatePart(string name, PrimitiveType type, Vector3 pos, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(player.transform);
            part.transform.localPosition = pos;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>()); // Remove colliders from visual parts
            part.GetComponent<MeshRenderer>().material = playerMat;
            return part;
        }

        // Create parts
        GameObject head = CreatePart("Head", PrimitiveType.Cube, new Vector3(0, 0.7f, 0), new Vector3(0.4f, 0.4f, 0.4f));
        CreatePart("Body", PrimitiveType.Cube, new Vector3(0, 0.1f, 0), new Vector3(0.6f, 0.8f, 0.4f));
        CreatePart("LeftArm", PrimitiveType.Cube, new Vector3(-0.4f, 0.1f, 0), new Vector3(0.2f, 0.7f, 0.2f));
        CreatePart("RightArm", PrimitiveType.Cube, new Vector3(0.4f, 0.1f, 0), new Vector3(0.2f, 0.7f, 0.2f));
        CreatePart("LeftLeg", PrimitiveType.Cube, new Vector3(-0.15f, -0.6f, 0), new Vector3(0.25f, 0.8f, 0.25f));
        CreatePart("RightLeg", PrimitiveType.Cube, new Vector3(0.15f, -0.6f, 0), new Vector3(0.25f, 0.8f, 0.25f));
        
        // Add a "visor" so we know which way it's looking
        Material visorMat = new Material(shader);
        visorMat.color = Color.cyan;
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor";
        visor.transform.SetParent(head.transform);
        visor.transform.localPosition = new Vector3(0, 0, 0.5f); // Relative to head
        visor.transform.localScale = new Vector3(0.8f, 0.3f, 0.1f);
        Object.DestroyImmediate(visor.GetComponent<Collider>());
        visor.GetComponent<MeshRenderer>().material = visorMat;

        Debug.Log("Player converted to Humanoid successfully!");
    }
}
