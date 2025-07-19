using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [CustomPropertyDrawer(typeof(ConvoCoreConversationData.DialogueLineInfo))]
    public class DialogueLinesPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Begin a group to confine drawing within this position
            GUI.BeginGroup(position);
            EditorGUI.BeginProperty(position, label, property);

            // Configuration for line height and spacing
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;

            // Initial drawing rectangle, adjusted for group boundaries
            Rect currentRect = new Rect(0, 0, position.width, lineHeight);
            SerializedObject serializedObject = property.serializedObject;

            // Get serialized properties for all relevant fields
            SerializedProperty keyProp = property.FindPropertyRelative("ConversationID");
            SerializedProperty indexProp = property.FindPropertyRelative("ConversationLineIndex");
            SerializedProperty characterIDProp = property.FindPropertyRelative("characterID");
            SerializedProperty selectedLineProgressionProp = property.FindPropertyRelative("UserInputMethod");
            SerializedProperty timedValueProp = property.FindPropertyRelative("TimeBeforeNextLine");
            SerializedProperty clipProp = property.FindPropertyRelative("clip");
            SerializedProperty localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            SerializedProperty actionsBeforeDialogueLineProp = property.FindPropertyRelative("ActionsBeforeDialogueLine");
            SerializedProperty actionsAfterDialogueLineProp = property.FindPropertyRelative("ActionsAfterDialogueLine");
            SerializedProperty selectedRepProp = property.FindPropertyRelative("SelectedRepresentation");
            SerializedProperty selectedRepNameProp = property.FindPropertyRelative("SelectedRepresentationName");
            SerializedProperty selectedRepEmotionProp = property.FindPropertyRelative("SelectedRepresentationEmotion");

            // Draw Conversation ID (Editable Field)
            EditorGUI.LabelField(currentRect, "Conversation ID", keyProp.stringValue);
            currentRect.y += lineHeight + spacing;

            // Draw Line Index (Read-Only)
            EditorGUI.LabelField(currentRect, "Line Index:", indexProp.intValue.ToString());
            currentRect.y += lineHeight + spacing;

            // Draw Character ID (Read-Only)
            EditorGUI.LabelField(currentRect, "Character ID:", characterIDProp.stringValue);
            currentRect.y += lineHeight + spacing;

            // Input Method Dropdown
            var currentInputMethod =
                (ConvoCoreConversationData.DialogueLineProgressionMethod)selectedLineProgressionProp.enumValueIndex;
            currentInputMethod = (ConvoCoreConversationData.DialogueLineProgressionMethod)EditorGUI.EnumPopup(
                currentRect,
                "Input Method:",
                currentInputMethod
            );
            selectedLineProgressionProp.enumValueIndex = (int)currentInputMethod;
            currentRect.y += lineHeight + spacing;

            // Conditionally draw the float input field for timed input method
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

            // Draw Localized Dialogues
            if (localizedDialoguesProp != null && localizedDialoguesProp.isArray)
            {
                string currentLanguage = ConvoCoreLanguageManager.Instance.CurrentLanguage;
                SerializedProperty matchingDialogue = null;

                // Find the dialogue for the current language
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

                    // Draw the localized text in a read-only label
                    GUIContent content = new GUIContent(textProp.stringValue);
                    EditorGUI.LabelField(new Rect(currentRect.x, currentRect.y, position.width, lineHeight), content);
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
            
            // Draw Actions Before Dialogue Line
            EditorGUI.PropertyField(currentRect, actionsBeforeDialogueLineProp, new GUIContent("Actions Before Line:"), true);
            currentRect.y += EditorGUI.GetPropertyHeight(actionsBeforeDialogueLineProp, true) + spacing;

            // Draw Actions After Dialogue Line
            EditorGUI.PropertyField(currentRect, actionsAfterDialogueLineProp, new GUIContent("Actions After Line:"), true);
            currentRect.y += EditorGUI.GetPropertyHeight(actionsAfterDialogueLineProp, true) + spacing;

            

            // Ensure we are working on ConvoCoreConversationData
            var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;
            if (conversationObject == null)
            {
                EditorGUI.LabelField(currentRect, "Error: Conversation data is missing.");
                currentRect.y += lineHeight + spacing;
                return;
            }

            // Fetch the profile for the character using CharacterID
            var profile = conversationObject.ConversationParticipantProfiles
                ?.FirstOrDefault(p => p != null && p.CharacterID == characterIDProp.stringValue);

            if (profile == null)
            {
                EditorGUI.LabelField(currentRect, "Error: Character profile not found!");
                currentRect.y += lineHeight + spacing;
                return;
            }

            // Fetch representations
            var representationNames = profile.Representations
                .Where(rep => rep != null && !string.IsNullOrEmpty(rep.CharacterRepresentationName))
                .Select(rep => rep.CharacterRepresentationName)
                .ToList();
            string selectedRepresentationName = selectedRepNameProp.stringValue;

            if (representationNames.Count == 0)
            {
                EditorGUI.LabelField(currentRect, "No representations available for this character.");
                currentRect.y += lineHeight + spacing;
                return;
            }

            // Draw Representation Dropdown
            int selectedIndex = Mathf.Max(0, representationNames.IndexOf(selectedRepresentationName));
            selectedIndex = EditorGUI.Popup(currentRect, "Representation:", selectedIndex,
                representationNames.ToArray());
            string newRepresentationName = representationNames[selectedIndex];

            if (newRepresentationName != selectedRepresentationName)
            {
                selectedRepNameProp.stringValue = newRepresentationName;
                selectedRepEmotionProp.stringValue = ""; // Reset emotion when representation changes
                serializedObject.ApplyModifiedProperties();
            }

            currentRect.y += lineHeight + spacing;

            // Get the selected representation and fetch emotion IDs
            var selectedRepresentation = profile.GetRepresentation(newRepresentationName);
            if (selectedRepresentation != null)
            {
                var emotionIDs = selectedRepresentation.GetEmotionIDs();
                if (emotionIDs == null || emotionIDs.Count == 0)
                {
                    EditorGUI.LabelField(currentRect, "No emotions available for this representation.");
                    currentRect.y += lineHeight + spacing;
                    return;
                }

                // Use the local SelectedRepresentationEmotion for this line
                string currentEmotionID = selectedRepEmotionProp.stringValue;

                // Ensure emotion ID is valid
                if (string.IsNullOrEmpty(currentEmotionID) || !emotionIDs.Contains(currentEmotionID))
                {
                    currentEmotionID = emotionIDs[0];
                    selectedRepEmotionProp.stringValue = currentEmotionID; // Default to first emotion
                    serializedObject.ApplyModifiedProperties();
                }

                // Draw an Emotion Dropdown
                int currentEmotionIndex = emotionIDs.IndexOf(currentEmotionID);
                int newEmotionIndex =
                    EditorGUI.Popup(currentRect, "Emotion:", currentEmotionIndex, emotionIDs.ToArray());
                if (newEmotionIndex != currentEmotionIndex)
                {
                    string newEmotionID = emotionIDs[newEmotionIndex];
                    selectedRepEmotionProp.stringValue = newEmotionID;
                    serializedObject.ApplyModifiedProperties();
                }

                currentRect.y += lineHeight + spacing;
            }

            EditorGUI.EndProperty();
            GUI.EndGroup();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Base height for standard fields
            int baseFieldCount = 5; // conversationID, lineIndex, characterID, input method, clip
            float totalHeight = baseFieldCount * (EditorGUIUtility.singleLineHeight + 2f);

            // Add height for timed input field if applicable
            SerializedProperty selectedLineProgressionProp = property.FindPropertyRelative("UserInputMethod");
            var inputMethod =
                (ConvoCoreConversationData.DialogueLineProgressionMethod)selectedLineProgressionProp.enumValueIndex;
            if (inputMethod == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
            {
                totalHeight += EditorGUIUtility.singleLineHeight + 2f;
            }

            // Add height for localized dialogues
            SerializedProperty localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            if (localizedDialoguesProp != null && localizedDialoguesProp.isArray)
            {
                totalHeight += localizedDialoguesProp.arraySize * (EditorGUIUtility.singleLineHeight + 2f);
            }
            // Add height for Actions Before Dialogue Line
            SerializedProperty actionsBeforeDialogueLineProp = property.FindPropertyRelative("ActionsBeforeDialogueLine");
            totalHeight += EditorGUI.GetPropertyHeight(actionsBeforeDialogueLineProp, true);

            // Add height for Actions After Dialogue Line
            SerializedProperty actionsAfterDialogueLineProp = property.FindPropertyRelative("ActionsAfterDialogueLine");
            totalHeight += EditorGUI.GetPropertyHeight(actionsAfterDialogueLineProp, true);


            // Add height for representation dropdown
            totalHeight += EditorGUIUtility.singleLineHeight + 2f;

            // Add height for representation preview (if applicable)
            SerializedProperty selectedRepProp = property.FindPropertyRelative("SelectedRepresentation");
            if (selectedRepProp.objectReferenceValue is IEditorPreviewableRepresentation previewableRepresentation)
            {
                totalHeight += previewableRepresentation.GetPreviewHeight() + 2f;
            }

            return totalHeight;
        }
    }
}