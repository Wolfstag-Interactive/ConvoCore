using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1ConvoCoreCharacterProfileBaseDataEditor.html")]
[CustomEditor(typeof(ConvoCoreCharacterProfileBaseData))]
    public class ConvoCoreCharacterProfileBaseDataEditor : UnityEditor.Editor
    {
        SerializedProperty isPlayerProp;
        SerializedProperty characterNameProp;
        SerializedProperty playerPlaceholderProp;
        SerializedProperty characterEmotionsProp;
        SerializedProperty characterDescriptionProp;

        private void OnEnable()
        {
            // Cache references to the serialized properties.
            isPlayerProp = serializedObject.FindProperty("IsPlayerCharacter");
            characterNameProp = serializedObject.FindProperty("CharacterName");
            playerPlaceholderProp = serializedObject.FindProperty("PlayerPlaceholder");
            characterDescriptionProp = serializedObject.FindProperty("CharacterDescription");
        }

        public override void OnInspectorGUI()
        {
            // Update the serialized object
            serializedObject.Update();

            // Display the script reference (read-only)
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;

            EditorGUILayout.PropertyField(isPlayerProp);
            if (isPlayerProp.boolValue)
            {
                EditorGUILayout.PropertyField(playerPlaceholderProp, new GUIContent("Player Placeholder Phrase"));
            }
            EditorGUILayout.PropertyField(characterNameProp);
            DrawCharacterDescription();
            // Check for duplicate emotion names
            CheckAndDisplayDuplicateEmotionNames();

            // Draw the rest of the properties excluding script and the ones already shown
            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, "m_Script", "IsPlayerCharacter", "CharacterName", "PlayerPlaceholder", "CharacterDescription");

            // Apply changes to the serialized object
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCharacterDescription()
        {
            // Draw CharacterDescription as editable multiline text with tooltip
            var content = new GUIContent("Character Description", "An optional field used to store character information such as a character description or biography.");
            var r = EditorGUILayout.GetControlRect(true, Mathf.Max(60, EditorGUIUtility.singleLineHeight));
            r = EditorGUI.IndentedRect(r);
            var labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(r.x, r.y, labelWidth, EditorGUIUtility.singleLineHeight);
            var fieldRect = new Rect(r.x + labelWidth, r.y, r.width - labelWidth, r.height);
            EditorGUI.PrefixLabel(labelRect, content); // shows tooltip on hover
            characterDescriptionProp.stringValue = EditorGUI.TextArea(fieldRect, characterDescriptionProp.stringValue);
        }

        private void CheckAndDisplayDuplicateEmotionNames()
        {
            // Dictionary to count occurrences of each emotion name
            Dictionary<string, int> nameCounts = new Dictionary<string, int>();

            // Make sure the property is valid and is an array
            if (characterEmotionsProp != null && characterEmotionsProp.isArray)
            {
                for (int i = 0; i < characterEmotionsProp.arraySize; i++)
                {
                    // Get the element reference (a ScriptableObject asset)
                    var emotionReferenceProperty = characterEmotionsProp.GetArrayElementAtIndex(i);
                    var emotionAsset = emotionReferenceProperty.objectReferenceValue as ConvoCoreCharacterEmotion;
                    if (emotionAsset != null && !string.IsNullOrEmpty(emotionAsset.emotionName))
                    {
                        // Count duplicate names
                        if (nameCounts.ContainsKey(emotionAsset.emotionName))
                        {
                            nameCounts[emotionAsset.emotionName]++;
                        }
                        else
                        {
                            nameCounts.Add(emotionAsset.emotionName, 1);
                        }
                    }
                }

                // Display a HelpBox for any duplicate emotion names found
                foreach (var pair in nameCounts)
                {
                    if (pair.Value > 1)
                    {
                        EditorGUILayout.HelpBox(
                            $"Duplicate emotion name found: '{pair.Key}' is used {pair.Value} times. Ensure each emotion has a unique name to prevent conflicts.",
                            MessageType.Warning);
                    }
                }
            }
        }
    }
}