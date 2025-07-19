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
            EditorGUI.BeginProperty(position, label, property);

            // Configuration for line height and spacing
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            Rect currentRect = new Rect(position.x, position.y, position.width, lineHeight);

            // Draw the properties in a modular way
            currentRect = DrawBasicInfo(currentRect, property, spacing);
            currentRect = DrawInputMethod(currentRect, property, spacing);
            currentRect = DrawAudioClip(currentRect, property, spacing);
            currentRect = DrawLocalizedDialogues(currentRect, property, spacing);
            currentRect = DrawActionsList(currentRect, property, spacing);
            currentRect = DrawCharacterRepresentation(currentRect, property, spacing);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Calculate the total height by summing up the heights of all sections
            float totalHeight = 0f;

            totalHeight += GetBasicInfoHeight(property);
            totalHeight += GetInputMethodHeight(property);
            totalHeight += GetAudioClipHeight(property);
            totalHeight += GetLocalizedDialoguesHeight(property);
            totalHeight += GetActionsListHeight(property);
            totalHeight += GetCharacterRepresentationHeight(property);

            return totalHeight;
        }

        // ---- Draw Methods ----

        private Rect DrawBasicInfo(Rect rect, SerializedProperty property, float spacing)
        {
            SerializedProperty keyProp = property.FindPropertyRelative("ConversationID");
            SerializedProperty indexProp = property.FindPropertyRelative("ConversationLineIndex");
            SerializedProperty characterIDProp = property.FindPropertyRelative("characterID");

            // Draw basic info fields
            EditorGUI.LabelField(rect, "Conversation ID", keyProp.stringValue);
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            EditorGUI.LabelField(rect, "Line Index:", indexProp.intValue.ToString());
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            EditorGUI.LabelField(rect, "Character ID:", characterIDProp.stringValue);
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            return rect;
        }

        private Rect DrawInputMethod(Rect rect, SerializedProperty property, float spacing)
        {
            SerializedProperty selectedLineProgressionProp = property.FindPropertyRelative("UserInputMethod");
            SerializedProperty timedValueProp = property.FindPropertyRelative("TimeBeforeNextLine");

            // Draw the input method dropdown
            var currentInputMethod =
                (ConvoCoreConversationData.DialogueLineProgressionMethod)selectedLineProgressionProp.enumValueIndex;
            currentInputMethod = (ConvoCoreConversationData.DialogueLineProgressionMethod)EditorGUI.EnumPopup(
                rect,
                "Input Method:",
                currentInputMethod
            );
            selectedLineProgressionProp.enumValueIndex = (int)currentInputMethod;
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            // Draw timed input field if applicable
            if (currentInputMethod == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
            {
                EditorGUI.PropertyField(rect, timedValueProp, new GUIContent("Display Duration (seconds):"));
                rect.y += EditorGUIUtility.singleLineHeight + spacing;
            }

            return rect;
        }

        private Rect DrawAudioClip(Rect rect, SerializedProperty property, float spacing)
        {
            SerializedProperty clipProp = property.FindPropertyRelative("clip");

            // Draw the Audio Clip field
            float clipHeight = EditorGUI.GetPropertyHeight(clipProp);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, clipHeight), clipProp,
                new GUIContent("Audio Clip:"));
            rect.y += clipHeight + spacing;

            return rect;
        }

        private Rect DrawLocalizedDialogues(Rect rect, SerializedProperty property, float spacing)
        {
            SerializedProperty localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");

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
                    EditorGUI.LabelField(rect, $"Localized Dialogue ({currentLanguage}):");
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;

                    // Draw the localized text
                    GUIContent content = new GUIContent(textProp.stringValue);
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        content);
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                }
                else
                {
                    EditorGUI.LabelField(rect, $"No dialogue available for language: {currentLanguage}");
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                }
            }
            else
            {
                EditorGUI.LabelField(rect, "Localized Dialogues not available.");
                rect.y += EditorGUIUtility.singleLineHeight + spacing;
            }

            return rect;
        }

        private Rect DrawActionsList(Rect rect, SerializedProperty property, float spacing)
        {
            SerializedProperty actionsBeforeDialogueLineProp =
                property.FindPropertyRelative("ActionsBeforeDialogueLine");
            SerializedProperty actionsAfterDialogueLineProp = property.FindPropertyRelative("ActionsAfterDialogueLine");

            // Draw Actions Before Dialogue Line
            EditorGUI.PropertyField(rect, actionsBeforeDialogueLineProp, new GUIContent("Actions Before Line:"), true);
            rect.y += EditorGUI.GetPropertyHeight(actionsBeforeDialogueLineProp, true) + spacing;

            // Draw Actions After Dialogue Line
            EditorGUI.PropertyField(rect, actionsAfterDialogueLineProp, new GUIContent("Actions After Line:"), true);
            rect.y += EditorGUI.GetPropertyHeight(actionsAfterDialogueLineProp, true) + spacing;

            return rect;
        }

        private Rect DrawCharacterRepresentation(Rect rect, SerializedProperty property, float spacing)
        {
            SerializedObject serializedObject = property.serializedObject;
            SerializedProperty characterIDProp = property.FindPropertyRelative("characterID");
            SerializedProperty selectedRepNameProp = property.FindPropertyRelative("SelectedRepresentationName");
            SerializedProperty selectedRepEmotionProp = property.FindPropertyRelative("SelectedRepresentationEmotion");
            EditorGUI.LabelField(rect, "Character Representation:", EditorStyles.boldLabel);
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            // Fetch the conversation object
            var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;
            if (conversationObject == null)
            {
                EditorGUI.LabelField(rect, "Error: Conversation data is missing.");
                rect.y += EditorGUIUtility.singleLineHeight + spacing;
                return rect;
            }

            // Fetch the profile for the character
            var profile = conversationObject.ConversationParticipantProfiles
                ?.FirstOrDefault(p => p != null && p.CharacterID == characterIDProp.stringValue);

            if (profile == null)
            {
                EditorGUI.LabelField(rect, "Error: Character profile not found!");
                rect.y += EditorGUIUtility.singleLineHeight + spacing;
                return rect;
            }

            // Fetch representations
            var representationNames = profile.Representations
                .Where(rep => rep != null && !string.IsNullOrEmpty(rep.CharacterRepresentationName))
                .Select(rep => rep.CharacterRepresentationName)
                .ToList();

            string selectedRepresentationName = selectedRepNameProp.stringValue;

            if (representationNames.Count == 0)
            {
                EditorGUI.LabelField(rect, "No representations available for this character.");
                rect.y += EditorGUIUtility.singleLineHeight + spacing;
                return rect;
            }

            // Draw Representation Dropdown
            int selectedIndex = Mathf.Max(0, representationNames.IndexOf(selectedRepresentationName));
            selectedIndex = EditorGUI.Popup(rect, "Representation:", selectedIndex, representationNames.ToArray());
            string newRepresentationName = representationNames[selectedIndex];
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            if (newRepresentationName != selectedRepresentationName)
            {
                selectedRepNameProp.stringValue = newRepresentationName;
                selectedRepEmotionProp.stringValue = ""; // Reset emotion when representation changes
                serializedObject.ApplyModifiedProperties();
            }

            // Draw Emotion Dropdown if applicable
            var selectedRepresentation = profile.GetRepresentation(newRepresentationName);
            if (selectedRepresentation != null)
            {
                var emotionIDs = selectedRepresentation.GetEmotionIDs();
                if (emotionIDs != null && emotionIDs.Count > 0)
                {
                    string currentEmotionID = selectedRepEmotionProp.stringValue;

                    // Ensure valid emotion
                    if (string.IsNullOrEmpty(currentEmotionID) || !emotionIDs.Contains(currentEmotionID))
                    {
                        currentEmotionID = emotionIDs[0];
                        selectedRepEmotionProp.stringValue = currentEmotionID;
                        serializedObject.ApplyModifiedProperties();
                    }

                    int currentEmotionIndex = emotionIDs.IndexOf(currentEmotionID);
                    int newEmotionIndex = EditorGUI.Popup(rect, "Emotion:", currentEmotionIndex, emotionIDs.ToArray());
                    if (newEmotionIndex != currentEmotionIndex)
                    {
                        string newEmotionID = emotionIDs[newEmotionIndex];
                        selectedRepEmotionProp.stringValue = newEmotionID;
                        serializedObject.ApplyModifiedProperties();
                    }

                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                }
                else
                {
                    EditorGUI.LabelField(rect, "No emotions available for this representation.");
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                }
            }

            return rect;
        }

        // ---- Height Calculation Methods ----

        private float GetBasicInfoHeight(SerializedProperty property)
        {
            return 3 * (EditorGUIUtility.singleLineHeight + 2f); // Conversation ID, Line Index, Character ID
        }

        private float GetInputMethodHeight(SerializedProperty property)
        {
            SerializedProperty selectedLineProgressionProp = property.FindPropertyRelative("UserInputMethod");
            var inputMethod =
                (ConvoCoreConversationData.DialogueLineProgressionMethod)selectedLineProgressionProp.enumValueIndex;
            float height = EditorGUIUtility.singleLineHeight + 2f;

            // Add extra height if input method is timed
            if (inputMethod == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
            {
                height += EditorGUIUtility.singleLineHeight + 2f;
            }

            return height;
        }

        private float GetAudioClipHeight(SerializedProperty property)
        {
            SerializedProperty clipProp = property.FindPropertyRelative("clip");
            return EditorGUI.GetPropertyHeight(clipProp) + 2f;
        }

        private float GetLocalizedDialoguesHeight(SerializedProperty property)
        {
            SerializedProperty localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            if (localizedDialoguesProp != null && localizedDialoguesProp.isArray)
            {
                return localizedDialoguesProp.arraySize * (EditorGUIUtility.singleLineHeight + 2f);
            }

            return EditorGUIUtility.singleLineHeight + 2f;
        }

        private float GetActionsListHeight(SerializedProperty property)
        {
            SerializedProperty actionsBeforeDialogueLineProp =
                property.FindPropertyRelative("ActionsBeforeDialogueLine");
            SerializedProperty actionsAfterDialogueLineProp = property.FindPropertyRelative("ActionsAfterDialogueLine");

            float height = 0f;
            height += EditorGUI.GetPropertyHeight(actionsBeforeDialogueLineProp, true);
            height += EditorGUI.GetPropertyHeight(actionsAfterDialogueLineProp, true);

            return height + 2f; // Spacing
        }

        private float GetCharacterRepresentationHeight(SerializedProperty property)
        {
            float height = 0f;

            // Add height for the label (Character Representation section header)
            height += EditorGUIUtility.singleLineHeight + 2f; // For the "Character Representation" label

            // Add height for the representation dropdown
            height += EditorGUIUtility.singleLineHeight + 2f;

            // Add height for the emotions dropdown (if applicable)
            SerializedProperty selectedRepNameProp = property.FindPropertyRelative("SelectedRepresentationName");
            SerializedProperty characterIDProp = property.FindPropertyRelative("characterID");

            SerializedObject serializedObject = property.serializedObject;
            var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;

            if (conversationObject != null)
            {
                var profile = conversationObject.ConversationParticipantProfiles
                    ?.FirstOrDefault(p => p != null && p.CharacterID == characterIDProp.stringValue);

                if (profile != null)
                {
                    string selectedRepresentationName = selectedRepNameProp.stringValue;
                    var selectedRepresentation = profile.GetRepresentation(selectedRepresentationName);

                    if (selectedRepresentation != null)
                    {
                        var emotionIDs = selectedRepresentation.GetEmotionIDs();

                        // Add height for the dropdown (if there are emotions available)
                        if (emotionIDs != null && emotionIDs.Count > 0)
                        {
                            height += EditorGUIUtility.singleLineHeight + 2f;
                        }
                    }
                }
            }

            // Add height for the inline preview (if applicable)
            SerializedProperty selectedRepProp = property.FindPropertyRelative("SelectedRepresentation");
            if (selectedRepProp.objectReferenceValue is IEditorPreviewableRepresentation previewableRepresentation)
            {
                height += previewableRepresentation.GetPreviewHeight() + 2f;
            }

            return height;
        }
    }
}