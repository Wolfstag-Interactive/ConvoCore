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
            SerializedProperty selectedRepProp = property.FindPropertyRelative("SelectedRepresentation");
            SerializedProperty selectedRepNameProp = property.FindPropertyRelative("SelectedRepresentationName");

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
            if (localizedDialoguesProp is { isArray: true })
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

            // Ensure we are working on ConvoCoreConversationData
            var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;
            if (conversationObject == null)
            {
                EditorGUI.LabelField(currentRect, "Error: Conversation data is missing.");
                currentRect.y += lineHeight + spacing;
                return;
            }

            // Validate the conversation object (e.g., check missing data)
            if (conversationObject == null)
            {
                EditorGUI.LabelField(currentRect, "Error: Missing Conversation Data!");
                currentRect.y += lineHeight + spacing;
                return;
            }

            // Fetch the profile for the character using CharacterID
            var profile = conversationObject.ConversationParticipantProfiles
                ?.FirstOrDefault(p => p != null && p.CharacterID == characterIDProp.stringValue);

            // Validate the profile
            if (profile == null)
            {
                EditorGUI.LabelField(currentRect, "Error: Character profile not found!");
                currentRect.y += lineHeight + spacing;
                return;
            }

            // Fetch representation names from the profile and validate them
            List<string> representationNames = profile.Representations
                .Where(rep => rep != null && !string.IsNullOrEmpty(rep.CharacterRepresentationName))
                .Select(rep => rep.CharacterRepresentationName)
                .ToList();

            if (representationNames.Count == 0)
            {
                // No representations available
                EditorGUI.LabelField(currentRect, "No representations available for this character.");
                currentRect.y += lineHeight + spacing;
                return;
            }

            // Validate the selected representation
            string currentRepName = selectedRepNameProp.stringValue;

            if (string.IsNullOrEmpty(currentRepName) || !representationNames.Contains(currentRepName))
            {
                // Default to the first representation if none is selected or invalid
                currentRepName = representationNames[0];
                selectedRepNameProp.stringValue = currentRepName;
            }

            // Dropdown to display the available representations
            int currentIndex = representationNames.IndexOf(currentRepName);
            currentIndex = EditorGUI.Popup(currentRect, "Representation:", currentIndex, representationNames.ToArray());
            currentRepName = representationNames[currentIndex];
            selectedRepNameProp.stringValue = currentRepName; // Update property
            currentRect.y += lineHeight + spacing;

            // Fetch the selected representation object from the profile
            CharacterRepresentationBase selectedRepresentation = profile.GetRepresentation(currentRepName);

            if (selectedRepresentation == null)
            {
                EditorGUI.LabelField(currentRect, "Error: Selected representation is missing.");
                currentRect.y += lineHeight + spacing;
                return;
            }

            // Check if the selected representation implements IEditorPreviewableRepresentation
            if (selectedRepresentation is IEditorPreviewableRepresentation previewableRepresentation)
            {
                // Fetch height for the inline preview
                float previewHeight = previewableRepresentation.GetPreviewHeight();
                var mappingData = profile.Representations
                    .FirstOrDefault(r => r.CharacterRepresentationName == selectedRepNameProp.stringValue)
                    ?.CharacterRepresentation
                    ?.ProcessEmotion(selectedRepProp.name);

                // Draw inline preview (provided by the representation)
                Rect previewRect = new Rect(currentRect.x, currentRect.y, position.width, previewHeight);
                previewableRepresentation.DrawInlineEditorPreview(mappingData, previewRect);

                currentRect.y += previewHeight + spacing; // Adjust position after drawing preview
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
            if (localizedDialoguesProp is { isArray: true })
            {
                totalHeight += localizedDialoguesProp.arraySize * (EditorGUIUtility.singleLineHeight + 2f);
            }

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