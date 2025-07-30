using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [CustomEditor(typeof(ConvoCoreConversationData))]
    public class CharacterConversationObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty _conversationKey;
        private SerializedProperty _filePath;

        public override void OnInspectorGUI()
        {
            // Update the serialized object
            serializedObject.Update();

            // Initialize required serialized properties
            _filePath = serializedObject.FindProperty("FilePath");
            _conversationKey = serializedObject.FindProperty("ConversationKey");

            // Draw a help box for additional context
            EditorGUILayout.LabelField("Conversation Data Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Edit and manage YAML-based conversation data. Ensure 'FilePath' points to a valid file inside the StreamingAssets folder.", MessageType.Info);

            // Track if any changes are made for validation
            EditorGUI.BeginChangeCheck();

            // Draw properties using custom iteration to handle overrides
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true); // Skip script field

            do
            {
                // Skip custom-handled properties
                if (property.name == "FilePath" || property.name == "ConversationKey")
                    continue;

                // Default property field rendering
                EditorGUILayout.PropertyField(property, true);
            }
            while (property.NextVisible(false));

            // Draw 'FilePath' field with the browse button
            DrawFilePathField();

            // Draw 'ConversationKey' field with the import button
            DrawConversationKeyField();

            // If any changes were made, validate the data
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                var conversationData = (ConvoCoreConversationData)target;
                conversationData.ValidateAndFixDialogueLines();
                EditorUtility.SetDirty(target);
            }

            // Add validation tools section
            DrawValidationToolsSection();

            // Apply any modified properties
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws validation tools section with buttons for manual validation
        /// </summary>
        private void DrawValidationToolsSection()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Validation Tools", EditorStyles.boldLabel);
    
            EditorGUILayout.HelpBox(
                "Use these tools to validate and debug your dialogue data. " +
                "The validation will automatically fix missing primary character representations.",
                MessageType.Info
            );

            // Create a horizontal layout for the buttons
            EditorGUILayout.BeginHorizontal();

            // Validate and Fix button
            if (GUILayout.Button("Validate & Fix All Dialogue Lines", GUILayout.Height(25)))
            {
                var conversationData = (ConvoCoreConversationData)target;
                conversationData.ValidateAndFixDialogueLines();
                EditorUtility.SetDirty(target);
                Debug.Log($"Manual validation completed for {conversationData.name}");
            }

            // Debug profiles button
            if (GUILayout.Button("Debug Character Profiles", GUILayout.Height(25)))
            {
                var conversationData = (ConvoCoreConversationData)target;
                conversationData.DebugCharacterProfiles();
            }

            EditorGUILayout.EndHorizontal();

            // Second row of buttons
            EditorGUILayout.BeginHorizontal();

            // Sync object references button
            if (GUILayout.Button("Sync Object References", GUILayout.Height(25)))
            {
                var conversationData = (ConvoCoreConversationData)target;
                conversationData.SyncAllRepresentationObjectReferences();
                EditorUtility.SetDirty(target);
                Debug.Log($"Object reference sync completed for {conversationData.name}");
            }

            // Force save button
            if (GUILayout.Button("Force Save Asset", GUILayout.Height(25)))
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                Debug.Log($"Forced save completed for {target.name}");
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
        }


        /// <summary>
        /// Draws the File Path field with browse functionality.
        /// </summary>
        private void DrawFilePathField()
        {
            // Section header
            EditorGUILayout.LabelField("YML Dialogue Data File Path", EditorStyles.boldLabel);

            // Description
            EditorGUILayout.HelpBox(
                "Specify the path to the YML file containing conversation data. " +
                "Ensure this file exists inside the /StreamingAssets/ folder.",
                MessageType.Info
            );

            // Draw the FilePath property field
            EditorGUILayout.PropertyField(_filePath, new GUIContent("File Path"));

            // Browse button to load file path
            if (GUILayout.Button("Browse YML File"))
            {
                // Open file panel for YML files
                string filePath = EditorUtility.OpenFilePanel("Select YML File", Application.streamingAssetsPath, "yml");
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Ensure the file resides in the StreamingAssets folder
                    if (filePath.StartsWith(Application.streamingAssetsPath))
                    {
                        _filePath.stringValue = filePath.Substring(Application.streamingAssetsPath.Length + 1); // Relative path
                        serializedObject.ApplyModifiedProperties(); // Apply changes
                    }
                    else
                    {
                        Debug.LogError("Selected file must reside inside the StreamingAssets folder.");
                    }
                }
            }
        }

        /// <summary>
        /// Draws the Conversation Key field with an import button.
        /// </summary>
        private void DrawConversationKeyField()
        {
            // Section header
            EditorGUILayout.LabelField("Conversation Key", EditorStyles.boldLabel);

            // Description
            EditorGUILayout.HelpBox(
                "Specify the unique identifier for the conversation. " +
                "Ensure it matches an existing key in your YML data file.",
                MessageType.Info
            );

            // Draw the ConversationKey property field
            EditorGUILayout.PropertyField(_conversationKey, new GUIContent("Conversation Key"));

            // Button to import data using the ConversationKey
            if (GUILayout.Button("Import From YAML For Key"))
            {
                // Ensure the ConversationKey is not empty
                if (string.IsNullOrEmpty(_conversationKey.stringValue))
                {
                    Debug.LogError("Please provide a valid conversation key.");
                }
                else
                {
                    // Safely import the conversation using the provided key
                    ConvoCoreConversationData obj = (ConvoCoreConversationData)target;
                    obj.ConvoCoreYamlUtilities.ImportFromYamlForKey(_conversationKey.stringValue);
                    
                    // After import, validate the data
                    obj.ValidateAndFixDialogueLines();
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}