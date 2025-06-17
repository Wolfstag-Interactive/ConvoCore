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

            // Initialize specific serialized properties
            _filePath = serializedObject.FindProperty("FilePath");
            _conversationKey = serializedObject.FindProperty("ConversationKey");

            // Iterate through all serialized properties and draw them (including custom property drawers)
            SerializedProperty property = serializedObject.GetIterator(); // Get all properties of this object
            property.NextVisible(true); // Move to the first visible property

            do
            {
                // Check if the current property is being overridden and skip it
                if (property.name == "FilePath" || property.name == "ConversationKey") 
                    continue;

                // Draw the property (including custom drawers)
                EditorGUILayout.PropertyField(property, true);
            }
            while (property.NextVisible(false)); // Move to the next visible property

            // Manually draw the FilePath field with an extra button
            EditorGUILayout.LabelField("YML Dialogue Data File Path", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The path to the YML file that contains the conversation data. The file should be located somewhere within the /StreamingAssets/ folder to function correctly.", MessageType.Info);
            EditorGUILayout.PropertyField(_filePath, new GUIContent("File Path"));

            if (GUILayout.Button("Browse YML File"))
            {
                string filePath = EditorUtility.OpenFilePanel("Select YML File", Application.streamingAssetsPath, "yml");
                if (!string.IsNullOrEmpty(filePath))
                {
                    if (filePath.StartsWith(Application.streamingAssetsPath))
                    {
                        _filePath.stringValue = filePath.Substring(Application.streamingAssetsPath.Length + 1);
                        serializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        Debug.LogError("Selected file must reside inside the StreamingAssets folder.");
                    }
                }
            }

            // Manually draw the ConversationKey field and import button
            EditorGUILayout.LabelField("Conversation Key", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The Identifier of the conversation to load",MessageType.Info);
            EditorGUILayout.PropertyField(_conversationKey, new GUIContent("Conversation Key"));

            if (GUILayout.Button("Import From YAML For Key"))
            {
                if (string.IsNullOrEmpty(_conversationKey.stringValue))
                {
                    Debug.LogError("Please provide a valid conversation key.");
                }
                else
                {
                    ConvoCoreConversationData obj = (ConvoCoreConversationData)target;
                    obj.ImportFromYamlForKey(_conversationKey.stringValue);
                }
            }

            // Apply any modified properties
            serializedObject.ApplyModifiedProperties();
        }
    }
}