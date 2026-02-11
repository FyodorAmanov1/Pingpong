using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool that validates and auto-fixes the game scene setup.
/// Run from menu: Tools → Ping Pong → Validate & Fix Setup
/// </summary>
public class GameSetupValidator : EditorWindow
{
    [MenuItem("Tools/Ping Pong/Validate && Fix Setup")]
    public static void ValidateAndFix()
    {
        Debug.Log("=== Ping Pong Setup Validator ===");

        EnsureTagsExist();
        AttachBootstrapToCamera();

        Debug.Log("=== Validation Complete! Save your scene (Ctrl+S) ===");
        Debug.Log("The GameBootstrap script on Main Camera will set up everything at runtime.");
    }

    static void EnsureTagsExist()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTagIfMissing(tagsProp, tagManager, "Ball");
        AddTagIfMissing(tagsProp, tagManager, "Paddle");
        AddTagIfMissing(tagsProp, tagManager, "Boundary");

        Debug.Log("[Tags] Ensured 'Ball', 'Paddle', 'Boundary' tags exist.");
    }

    static void AddTagIfMissing(SerializedProperty tagsProp, SerializedObject tagManager, string tag)
    {
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        }
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    static void AttachBootstrapToCamera()
    {
        GameObject cam = GameObject.FindWithTag("MainCamera");
        if (cam == null)
        {
            cam = GameObject.Find("Main Camera");
        }
        if (cam == null)
        {
            Debug.LogError("[Bootstrap] Cannot find Main Camera! Create one or add GameBootstrap manually.");
            return;
        }

        if (cam.GetComponent<GameBootstrap>() == null)
        {
            cam.AddComponent<GameBootstrap>();
            Debug.Log("[Bootstrap] Added GameBootstrap to Main Camera.");
        }
        else
        {
            Debug.Log("[Bootstrap] GameBootstrap already on Main Camera.");
        }

        EditorUtility.SetDirty(cam);
    }
}
