using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [CustomEditor(typeof(ConvoCoreCharacterProfileBaseData))]
    public class ConvoCoreCharacterProfileBaseDataEditor : UnityEditor.Editor
    {
        SerializedProperty isPlayerProp;
        SerializedProperty characterNameProp;
        SerializedProperty playerPlaceholderProp;

        private void OnEnable()
        {
            // Cache references to the serialized properties (make sure the property names match)
            isPlayerProp = serializedObject.FindProperty("IsPlayerCharacter");
            characterNameProp = serializedObject.FindProperty("CharacterName");
            playerPlaceholderProp = serializedObject.FindProperty("PlayerPlaceholder");
        }

        public override void OnInspectorGUI()
        {
            // Update the serialized object
            serializedObject.Update();

            // Display the script reference (read-only)
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;
        
            // Always display the IsPlayer property
            EditorGUILayout.PropertyField(isPlayerProp);

            // Conditionally display the PlayerPlaceholder field if IsPlayer is true
            if (isPlayerProp.boolValue)
            {
                EditorGUILayout.PropertyField(playerPlaceholderProp, new GUIContent("Player Placeholder Phrase"));
            }
        
            // Always display the CharacterName field
            EditorGUILayout.PropertyField(characterNameProp);

            // Display the rest
            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, "m_Script", "IsPlayerCharacter", "CharacterName", "PlayerPlaceholder");

            // Apply changes to the serializedObject
            serializedObject.ApplyModifiedProperties();
        }
    }
}