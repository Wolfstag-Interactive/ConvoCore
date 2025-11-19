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
        SerializedProperty characterExpressionsProp;
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
            // Check for duplicate expression names
            CheckAndDisplayDuplicateExpressionNames();

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

        private void CheckAndDisplayDuplicateExpressionNames()
        {
            // Dictionary to count occurrences of each expression name
            Dictionary<string, int> nameCounts = new Dictionary<string, int>();

            // Make sure the property is valid and is an array
            if (characterExpressionsProp != null && characterExpressionsProp.isArray)
            {
                for (int i = 0; i < characterExpressionsProp.arraySize; i++)
                {
                    // Get the element reference (a ScriptableObject asset)
                    var expressionReferenceProperty = characterExpressionsProp.GetArrayElementAtIndex(i);
                    var expressionAsset = expressionReferenceProperty.objectReferenceValue as ConvoCoreCharacterExpression;
                    if (expressionAsset != null && !string.IsNullOrEmpty(expressionAsset.expressionName))
                    {
                        // Count duplicate names
                        if (nameCounts.ContainsKey(expressionAsset.expressionName))
                        {
                            nameCounts[expressionAsset.expressionName]++;
                        }
                        else
                        {
                            nameCounts.Add(expressionAsset.expressionName, 1);
                        }
                    }
                }

                // Display a HelpBox for any duplicate expression names found
                foreach (var pair in nameCounts)
                {
                    if (pair.Value > 1)
                    {
                        EditorGUILayout.HelpBox(
                            $"Duplicate expression name found: '{pair.Key}' is used {pair.Value} times. Ensure each expression has a unique name to prevent conflicts.",
                            MessageType.Warning);
                    }
                }
            }
        }
    }
}