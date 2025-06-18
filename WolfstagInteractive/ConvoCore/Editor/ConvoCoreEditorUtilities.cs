using UnityEditor;
using UnityEngine;
using System.IO;

namespace WolfstagInteractive.ConvoCore.Editor
{
    public static class ConvoCoreEditorUtilities
    {
        [MenuItem("Tools/Wolfstag Interactive/ConvoCore/Create LanguageSettings")]
        public static void CreateLanguageSettingsAsset()
        {
            // Define the Resources folder path relative to the project root.
            string resourcesFolder = Path.Combine("Assets", "Resources");

            // Ensure the Resources folder exists.
            if (!Directory.Exists(resourcesFolder))
            {
                Directory.CreateDirectory(resourcesFolder);
                Debug.Log($"Created folder: {resourcesFolder}");
            }

            // Define the full asset path for LanguageSettings.
            string assetPath = Path.Combine(resourcesFolder, "LanguageSettings.asset");

            // Check if the asset already exists.
            ConvoCoreLanguageSettings existingAsset = AssetDatabase.LoadAssetAtPath<ConvoCoreLanguageSettings>(assetPath);
            if (existingAsset != null)
            {
                Debug.LogWarning($"A LanguageSettings asset already exists at: {assetPath}");
                // Optionally, you could select the asset or prompt the user to overwrite it.
                Selection.activeObject = existingAsset;
                return;
            }

            // Create an instance of the ScriptableObject.
            ConvoCoreLanguageSettings settings = ScriptableObject.CreateInstance<ConvoCoreLanguageSettings>();

            // Create the asset at the defined path.
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Focus the Project window and select the newly created asset.
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;

            Debug.Log($"LanguageSettings asset was successfully created at: {assetPath}");
        }
        [MenuItem("Tools/Wolfstag Interactive/ConvoCore/Create Sample Dialogue YAML")]
        public static void CreateSampleYamlFile()
        {
            // Determine the StreamingAssets folder path.
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
        
            // Create the StreamingAssets folder if it doesn't exist.
            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
                Debug.Log($"Created StreamingAssets folder at: {streamingAssetsPath}");
            }

            // Define the full file path.
            string filePath = Path.Combine(streamingAssetsPath, "DialogueScriptSample.yml");

            // Define the YAML content.
            string yamlContent = @"contents:

Conversation1:
  - CharacterID: ""NPC1""
    LocalizedDialogue:
      EN: ""Hello there!""
      FR: ""Bonjour!""
      ES: ""¡Hola!""
  - CharacterID: ""NPC1""
    LocalizedDialogue:
      EN: ""Good to meet you!""
      FR: ""We we!""
      ES: ""¡Ey Campadre!""
  - CharacterID: ""NPC1""
    LocalizedDialogue:
      EN: ""Goodbye!""
      FR: ""Au revoir!""
      ES: ""¡Adiós!""
";

            // Write the YAML content to the file.
            File.WriteAllText(filePath, yamlContent);
            Debug.Log($"Sample YAML file created at: {filePath}");

            // Refresh the asset database so that the file appears in the Editor.
            AssetDatabase.Refresh();
        }
        [MenuItem("Tools/Wolfstag Interactive/ConvoCore/Create ConvoCore Conversation GameObject")]
        public static void CreateConvoCoreConversationGameObject()
        {
            // Create a new GameObject with the specified name
            GameObject convoObject = new GameObject("ConvoCore Conversation");
            Debug.Log("Created GameObject: ConvoCore Conversation");
            ConvoCore convoCoreComponent = convoObject.AddComponent<ConvoCore>();
            if (convoCoreComponent != null)
            {
                Debug.Log("ConvoCore component was successfully added.");
            }
            else
            {
                Debug.LogError("Failed to add ConvoCore component. Please ensure the ConvoCore script exists.");
            }
            Selection.activeGameObject = convoObject;
        }
    }
}