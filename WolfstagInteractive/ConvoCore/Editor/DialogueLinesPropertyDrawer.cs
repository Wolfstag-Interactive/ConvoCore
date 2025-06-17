using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [CustomPropertyDrawer(typeof(ConvoCoreConversationData.DialogueLines))]
    public class DialogueLinesPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Begin a group to confine drawing within the given position
            GUI.BeginGroup(position);
            EditorGUI.BeginProperty(position, label, property);

            // Configuration for spacing and line height
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;

            // Initial drawing rectangle offset within the group
            Rect currentRect = new Rect(0, 0, position.width, lineHeight);

            // Get serialized properties
            SerializedProperty keyProp = property.FindPropertyRelative("ConversationID");
            SerializedProperty indexProp = property.FindPropertyRelative("ConversationLineIndex");
            SerializedProperty characterIDProp = property.FindPropertyRelative("characterID");
            SerializedProperty selectedEmotionProp = property.FindPropertyRelative("SelectedEmotionName");
            SerializedProperty selectedLineProgressionProp = property.FindPropertyRelative("UserInputMethod");
            SerializedProperty timedValueProp = property.FindPropertyRelative("TimeBeforeNextLine");
            SerializedProperty alternateRepresentationProp = property.FindPropertyRelative("AlternateRepresentation");
            SerializedProperty clipProp = property.FindPropertyRelative("clip");
            SerializedProperty localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");

            // Draw Conversation ID
            EditorGUI.PropertyField(currentRect, keyProp, new GUIContent("Conversation ID:"));
            currentRect.y += lineHeight + spacing;

            // Draw Line Index
            EditorGUI.LabelField(currentRect, "Line Index: ", indexProp.intValue.ToString());
            currentRect.y += lineHeight + spacing;
            // Draw Character ID
            EditorGUI.LabelField(currentRect, "Character ID:", characterIDProp.stringValue);
            currentRect.y += lineHeight + spacing;

            // Draw Alternate Representation Dropdown
            SerializedObject serializedObject = property.serializedObject;
            var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;

            if (conversationObject != null)
            {
                string characterID = characterIDProp.stringValue;

                // Get the relevant character profile by character ID
                // Filter out any null entries before searching
                var profile = conversationObject.ConversationParticipantProfiles
                    .Where(p => p != null)
                    .FirstOrDefault(p => p.CharacterID == characterID);


                if (profile != null)
                {
                    // Get all available alternate representations from the profile
                    List<string> representations = profile.AlternateRepresentations
                        .Select(repr => profile.GetNameForRepresentation(repr.RepresentationID))
                        .ToList();

                    if (representations.Count > 0)
                    {
                        // Add the "Default" option to the dropdown
                        representations.Insert(0, "Default (No Override)");

                        // Find the currently selected representation index
                        int currentSelection = string.IsNullOrEmpty(alternateRepresentationProp.stringValue)
                            ? 0 // Default selected
                            : representations.IndexOf(
                                profile.GetNameForRepresentation(alternateRepresentationProp.stringValue));

                        // Ensure valid index
                        if (currentSelection < 0) currentSelection = 0;

                        // Draw the dropdown
                        currentSelection = EditorGUI.Popup(currentRect, "Alternate Representation:", currentSelection,
                            representations.ToArray());

                        // Update serialized field
                        if (currentSelection == 0)
                        {
                            // First option is "Default" (indicating no override)
                            alternateRepresentationProp.stringValue = string.Empty;
                        }
                        else
                        {
                            // Set the selected representation ID
                            alternateRepresentationProp.stringValue =
                                profile.AlternateRepresentations[currentSelection - 1]
                                    .RepresentationID; // Offset by 1 for the "Default" option
                        }
                    }
                    else
                    {
                        EditorGUI.LabelField(currentRect, "No available representations.");
                    }
                }
                else
                {
                    EditorGUI.LabelField(currentRect, "Character profile not found.");
                }
            }
            else
            {
                EditorGUI.LabelField(currentRect, "Error: Missing CharacterConversationObject.");
            }

            currentRect.y += lineHeight + spacing;
            // Input Method Dropdown
            ConvoCoreConversationData.DialogueLineProgressionMethod currentInputMethod = (ConvoCoreConversationData.DialogueLineProgressionMethod)selectedLineProgressionProp.enumValueIndex;
            currentInputMethod = (ConvoCoreConversationData.DialogueLineProgressionMethod)EditorGUI.EnumPopup(currentRect, "Input Method:", currentInputMethod);
            selectedLineProgressionProp.enumValueIndex = (int)currentInputMethod;
            currentRect.y += lineHeight + spacing;

            // Conditionally show the float field for timed input
            if (currentInputMethod == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
            {
                EditorGUI.PropertyField(currentRect, timedValueProp, new GUIContent("Display Duration (seconds):"));
                currentRect.y += lineHeight + spacing;
            }

            // Draw Audio Clip field
            float clipHeight = EditorGUI.GetPropertyHeight(clipProp);
            EditorGUI.PropertyField(new Rect(currentRect.x, currentRect.y, currentRect.width, clipHeight), clipProp,
                new GUIContent("Audio Clip:"));
            currentRect.y += clipHeight + spacing;

            // Draw Selected Emotion Dropdown
            if (conversationObject != null)
            {
                string characterID = characterIDProp.stringValue;
                var profile =
                    conversationObject.ConversationParticipantProfiles.Where(p=>p!=null).FirstOrDefault(p =>
                        p.CharacterID == characterID);

                if (profile != null)
                {
                    string representationID = alternateRepresentationProp.stringValue;

                    // Gather and display emotion names for the selected representation
                    List<string> emotionNames = string.IsNullOrEmpty(representationID)
                        ? profile.CharacterEmotions.Select(e => e.emotionName).ToList()
                        : profile.CharacterEmotions
                            .Where(e => e.RepresentationOverrides.Any(r => r.RepresentationID == representationID))
                            .Select(e => e.emotionName)
                            .ToList();

                    if (emotionNames.Count > 0)
                    {
                        int currentEmotionIndex = emotionNames.IndexOf(selectedEmotionProp.stringValue);
                        if (currentEmotionIndex == -1) currentEmotionIndex = 0; // Default to first emotion if invalid

                        currentEmotionIndex = EditorGUI.Popup(currentRect, "Emotion:", currentEmotionIndex,
                            emotionNames.ToArray());
                        selectedEmotionProp.stringValue = emotionNames[currentEmotionIndex];
                    }
                    else
                    {
                        EditorGUI.LabelField(currentRect, "No emotions found for this representation.");
                    }
                }
                else
                {
                    EditorGUI.LabelField(currentRect, "Character ID not found in profiles.");
                }
            }
            else
            {
                EditorGUI.LabelField(currentRect, "Error: Missing CharacterConversationObject.");
            }

            currentRect.y += lineHeight + spacing;

            // Draw the Localized Dialogue for the Current Language Only
            if (localizedDialoguesProp != null && localizedDialoguesProp.isArray)
            {
                string currentLanguage = ConvoCoreLanguageManager.Instance.CurrentLanguage;
                SerializedProperty matchingDialogue = null;

                // Iterate through array to find the localized dialogue in the selected language
                for (int i = 0; i < localizedDialoguesProp.arraySize; i++)
                {
                    SerializedProperty element = localizedDialoguesProp.GetArrayElementAtIndex(i);
                    SerializedProperty languageProp = element.FindPropertyRelative("Language");

                    if (languageProp != null && languageProp.stringValue == currentLanguage)
                    {
                        matchingDialogue = element;
                        break;
                    }
                }

                if (matchingDialogue != null)
                {
                    SerializedProperty textProp = matchingDialogue.FindPropertyRelative("Text");
                    EditorGUI.LabelField(currentRect, $"Localized Dialogue ({currentLanguage}):");
                    currentRect.y += lineHeight + spacing;

                    // Display the localized text (read-only)
                    EditorGUI.LabelField(new Rect(currentRect.x, currentRect.y, position.width, lineHeight),
                        textProp.stringValue);
                    currentRect.y += lineHeight + spacing;
                }
                else
                {
                    EditorGUI.LabelField(currentRect, $"No dialogue available for language: {currentLanguage}");
                }
            }
            else
            {
                EditorGUI.LabelField(currentRect, "Localized Dialogues not available.");
            }

            EditorGUI.EndProperty();
            GUI.EndGroup(); // End group to restrict rendering within bounds
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Base fields height
            int baseFieldCount = 5; // key, index, characterID, representation, clip
            float baseHeight = baseFieldCount * (EditorGUIUtility.singleLineHeight + 2f);

            // Calculate clip field height
            SerializedProperty clipProp = property.FindPropertyRelative("clip");
            float clipHeight = EditorGUI.GetPropertyHeight(clipProp);

            // Localized Dialogues height
            SerializedProperty localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            float localizedDialoguesHeight = 0f;
            if (localizedDialoguesProp != null && localizedDialoguesProp.isArray)
            {
                localizedDialoguesHeight = localizedDialoguesProp.arraySize * (EditorGUIUtility.singleLineHeight + 2f);
            }

            // Dropdown height for emotions
            float emotionHeight = EditorGUIUtility.singleLineHeight + 2f;

            return baseHeight + clipHeight + emotionHeight + localizedDialoguesHeight;
        }
    }
}