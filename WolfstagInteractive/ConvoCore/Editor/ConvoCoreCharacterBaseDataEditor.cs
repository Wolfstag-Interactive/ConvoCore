using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [CustomEditor(typeof(ConvoCoreCharacterProfileBaseData))]
    public class ConvoCoreCharacterProfileBaseDataEditor : UnityEditor.Editor
    {
        SerializedProperty isPlayerProp;
        SerializedProperty characterNameProp;
        SerializedProperty playerPlaceholderProp;
        SerializedProperty characterEmotionsProp;

        private void OnEnable()
        {
            // Cache references to the serialized properties.
            isPlayerProp = serializedObject.FindProperty("IsPlayerCharacter");
            characterNameProp = serializedObject.FindProperty("CharacterName");
            playerPlaceholderProp = serializedObject.FindProperty("PlayerPlaceholder");
            characterEmotionsProp = serializedObject.FindProperty("CharacterEmotions");
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
            
            // Check for duplicate emotion names
            CheckAndDisplayDuplicateEmotionNames();

            // Draw the rest of the properties excluding script and the ones already shown
            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, "m_Script", "IsPlayerCharacter", "CharacterName", "PlayerPlaceholder");

            // Apply changes to the serialized object
            serializedObject.ApplyModifiedProperties();
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