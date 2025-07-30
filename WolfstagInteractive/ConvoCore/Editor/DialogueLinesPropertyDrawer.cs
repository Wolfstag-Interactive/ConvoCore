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
            // Updated to handle multiple representations
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
            // Add heights for individual character representations
            totalHeight +=
                GetCharacterRepresentationHeight(property.FindPropertyRelative("PrimaryCharacterRepresentation"));
            totalHeight +=
                GetCharacterRepresentationHeight(property.FindPropertyRelative("SecondaryCharacterRepresentation"));
            totalHeight +=
                GetCharacterRepresentationHeight(property.FindPropertyRelative("TertiaryCharacterRepresentation"));

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
            // Draw Primary Character Representation using CharacterID
            rect = DrawSingleCharacterRepresentation(
                rect,
                property.FindPropertyRelative("PrimaryCharacterRepresentation"),
                "Primary Character",
                property.FindPropertyRelative("characterID"),
                spacing,
                useRepresentationNameInsteadOfID: false);

            // Draw Secondary Character Representation - pass a separate identifier property
            rect = DrawSingleCharacterRepresentation(
                rect,
                property.FindPropertyRelative("SecondaryCharacterRepresentation"),
                "Secondary Character",
                property.FindPropertyRelative("SecondaryCharacterRepresentation"), 
                spacing,
                useRepresentationNameInsteadOfID: true);

            // Draw Tertiary Character Representation - pass a separate identifier property
            rect = DrawSingleCharacterRepresentation(
                rect,
                property.FindPropertyRelative("TertiaryCharacterRepresentation"),
                "Tertiary Character",
                property.FindPropertyRelative("TertiaryCharacterRepresentation"), 
                spacing,
                useRepresentationNameInsteadOfID: true);

            return rect;
        }



  private Rect DrawSingleCharacterRepresentation(
    Rect rect, 
    SerializedProperty representationProp, 
    string label, 
    SerializedProperty identifierProp, 
    float spacing, 
    bool useRepresentationNameInsteadOfID
)
{
    SerializedObject serializedObject = representationProp.serializedObject;

    SerializedProperty selectedCharacterIDProp = representationProp.FindPropertyRelative("SelectedCharacterID");
    SerializedProperty selectedRepNameProp = representationProp.FindPropertyRelative("SelectedRepresentationName");
    SerializedProperty selectedRepEmotionProp = representationProp.FindPropertyRelative("SelectedRepresentationEmotion");

    // Draw Header
    EditorGUI.LabelField(rect, $"{label} Representation:", EditorStyles.boldLabel);
    rect.y += EditorGUIUtility.singleLineHeight + spacing;

    // Fetch the Conversation Data
    var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;
    if (conversationObject == null)
    {
        EditorGUI.LabelField(rect, "Error: Conversation data is missing.");
        rect.y += EditorGUIUtility.singleLineHeight + spacing;
        return rect;
    }

    // Filter valid profiles, ensuring null check
    var validProfiles = conversationObject.ConversationParticipantProfiles
        .Where(p => p != null)
        .ToList();

    if (validProfiles.Count == 0)
    {
        EditorGUI.LabelField(rect, "No participants available.");
        rect.y += EditorGUIUtility.singleLineHeight + spacing;
        return rect;
    }

    // For secondary/tertiary characters, we need to show a profile selection dropdown first
    if (useRepresentationNameInsteadOfID)
    {
        // Get the current selected character ID
        string currentCharacterID = selectedCharacterIDProp.stringValue;
        ConvoCoreCharacterProfileBaseData currentProfile = null;
        
        if (!string.IsNullOrEmpty(currentCharacterID))
        {
            currentProfile = validProfiles.FirstOrDefault(p => p.CharacterID == currentCharacterID);
        }

        // Show profile selection dropdown
        var profileNames = validProfiles
            .Where(p => !string.IsNullOrEmpty(p.CharacterName))
            .Select(p => p.CharacterName)
            .ToList();

        profileNames.Insert(0, "None"); // Add "None" option

        int currentProfileIndex = 0;
        if (currentProfile != null)
        {
            int foundIndex = profileNames.IndexOf(currentProfile.CharacterName);
            if (foundIndex > 0) currentProfileIndex = foundIndex;
        }

        int newProfileIndex = EditorGUI.Popup(rect, $"{label} Profile:", currentProfileIndex, profileNames.ToArray());
        rect.y += EditorGUIUtility.singleLineHeight + spacing;

        // Handle profile selection change
        if (newProfileIndex != currentProfileIndex)
        {
            if (newProfileIndex == 0) // "None" selected
            {
                selectedCharacterIDProp.stringValue = "";
                selectedRepNameProp.stringValue = "";
                selectedRepEmotionProp.stringValue = "";
                serializedObject.ApplyModifiedProperties();
                return rect;
            }
            else
            {
                var selectedProfile = validProfiles[newProfileIndex - 1]; // -1 because of "None" option
                selectedCharacterIDProp.stringValue = selectedProfile.CharacterID;
                
                // Set to first representation of selected profile
                var firstRep = selectedProfile.Representations?.FirstOrDefault(r => r != null);
                selectedRepNameProp.stringValue = firstRep?.CharacterRepresentationName ?? "";
                selectedRepEmotionProp.stringValue = "";
                serializedObject.ApplyModifiedProperties();
                currentProfile = selectedProfile;
            }
        }

        // If no profile selected, stop here
        if (newProfileIndex == 0 || currentProfile == null)
        {
            return rect;
        }

        // Continue with representation and emotion dropdowns using the selected profile
        var representationNames = currentProfile.Representations
            .Where(r => r != null && !string.IsNullOrEmpty(r.CharacterRepresentationName))
            .Select(r => r.CharacterRepresentationName)
            .ToList();

        if (representationNames.Count > 0)
        {
            string selectedRepresentationName = selectedRepNameProp.stringValue;
            int repIndex = Mathf.Max(0, representationNames.IndexOf(selectedRepresentationName));
            repIndex = EditorGUI.Popup(rect, "Representation:", repIndex, representationNames.ToArray());
            string newRepresentationName = representationNames[repIndex];
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            if (newRepresentationName != selectedRepresentationName)
            {
                selectedRepNameProp.stringValue = newRepresentationName;
                selectedRepEmotionProp.stringValue = "";
                serializedObject.ApplyModifiedProperties();
            }

            // Show emotion dropdown
            var selectedRepresentation = currentProfile.GetRepresentation(newRepresentationName);
            if (selectedRepresentation != null)
            {
                var emotionIDs = selectedRepresentation.GetEmotionIDs();
                if (emotionIDs != null && emotionIDs.Count > 0)
                {
                    string currentEmotionID = selectedRepEmotionProp.stringValue;
                    if (string.IsNullOrEmpty(currentEmotionID) || !emotionIDs.Contains(currentEmotionID))
                    {
                        currentEmotionID = emotionIDs[0];
                        selectedRepEmotionProp.stringValue = currentEmotionID;
                        serializedObject.ApplyModifiedProperties();
                    }

                    int emotionIndex = emotionIDs.IndexOf(currentEmotionID);
                    int newEmotionIndex = EditorGUI.Popup(rect, "Emotion:", emotionIndex, emotionIDs.ToArray());
                    if (newEmotionIndex != emotionIndex)
                    {
                        selectedRepEmotionProp.stringValue = emotionIDs[newEmotionIndex];
                        serializedObject.ApplyModifiedProperties();
                    }
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                }
            }
        }
    }
    else
    {
        // Primary character logic (existing code for characterID-based lookup)
        string characterID = identifierProp.stringValue;
        var profile = validProfiles.FirstOrDefault(p => p.CharacterID == characterID);

        if (profile == null)
        {
            var participantNames = validProfiles
                .Where(p => !string.IsNullOrEmpty(p.CharacterName))
                .Select(p => p.CharacterName)
                .ToArray();

            int participantIndex = 0;
            if (!string.IsNullOrEmpty(characterID))
            {
                participantIndex = System.Array.IndexOf(participantNames, characterID);
                participantIndex = Mathf.Max(participantIndex, 0);
            }

            participantIndex = EditorGUI.Popup(rect, $"{label} Participant:", participantIndex, participantNames);
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            if (participantIndex < participantNames.Length)
            {
                var selectedProfile = validProfiles[participantIndex];
                identifierProp.stringValue = selectedProfile.CharacterID;
                serializedObject.ApplyModifiedProperties();
            }
            return rect;
        }

        // Continue with representation dropdown for primary character
        var representationNames = profile.Representations
            .Where(r => r != null && !string.IsNullOrEmpty(r.CharacterRepresentationName))
            .Select(r => r.CharacterRepresentationName)
            .ToList();

        if (representationNames.Count > 0)
        {
            string selectedRepresentationName = selectedRepNameProp.stringValue;
            int repIndex = Mathf.Max(0, representationNames.IndexOf(selectedRepresentationName));
            repIndex = EditorGUI.Popup(rect, "Representation:", repIndex, representationNames.ToArray());
            string newRepresentationName = representationNames[repIndex];
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            if (newRepresentationName != selectedRepresentationName)
            {
                selectedRepNameProp.stringValue = newRepresentationName;
                selectedRepEmotionProp.stringValue = "";
                serializedObject.ApplyModifiedProperties();
            }

            // Show emotion dropdown
            var selectedRepresentation = profile.GetRepresentation(newRepresentationName);
            if (selectedRepresentation != null)
            {
                var emotionIDs = selectedRepresentation.GetEmotionIDs();
                if (emotionIDs != null && emotionIDs.Count > 0)
                {
                    string currentEmotionID = selectedRepEmotionProp.stringValue;
                    if (string.IsNullOrEmpty(currentEmotionID) || !emotionIDs.Contains(currentEmotionID))
                    {
                        currentEmotionID = emotionIDs[0];
                        selectedRepEmotionProp.stringValue = currentEmotionID;
                        serializedObject.ApplyModifiedProperties();
                    }

                    int emotionIndex = emotionIDs.IndexOf(currentEmotionID);
                    int newEmotionIndex = EditorGUI.Popup(rect, "Emotion:", emotionIndex, emotionIDs.ToArray());
                    if (newEmotionIndex != emotionIndex)
                    {
                        selectedRepEmotionProp.stringValue = emotionIDs[newEmotionIndex];
                        serializedObject.ApplyModifiedProperties();
                    }
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                }
            }
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
    if (property == null) return 0f;

    float height = 0f;
    SerializedObject serializedObject = property.serializedObject;
    var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;

    if (conversationObject == null)
    {
        return EditorGUIUtility.singleLineHeight + 2f; // Just for error message
    }

    var validProfiles = conversationObject.ConversationParticipantProfiles?.Where(p => p != null).ToList();
    if (validProfiles == null || validProfiles.Count == 0)
    {
        return EditorGUIUtility.singleLineHeight + 2f; // Just for "no participants" message
    }

    // Add height for the section header
    height += EditorGUIUtility.singleLineHeight + 2f;

    // Check if this is a secondary/tertiary character (has SelectedCharacterID field)
    SerializedProperty selectedCharacterIDProp = property.FindPropertyRelative("SelectedCharacterID");
    bool isSecondaryOrTertiary = selectedCharacterIDProp != null;

    if (isSecondaryOrTertiary)
    {
        // Add height for profile selection dropdown
        height += EditorGUIUtility.singleLineHeight + 2f;

        // Check if a profile is selected
        string selectedCharacterID = selectedCharacterIDProp.stringValue;
        if (!string.IsNullOrEmpty(selectedCharacterID))
        {
            var selectedProfile = validProfiles.FirstOrDefault(p => p.CharacterID == selectedCharacterID);
            if (selectedProfile != null)
            {
                // Add height for representation dropdown
                height += EditorGUIUtility.singleLineHeight + 2f;

                // Check if representation has emotions
                SerializedProperty selectedRepNameProp = property.FindPropertyRelative("SelectedRepresentationName");
                if (selectedRepNameProp != null && !string.IsNullOrEmpty(selectedRepNameProp.stringValue))
                {
                    var representation = selectedProfile.GetRepresentation(selectedRepNameProp.stringValue);
                    if (representation != null)
                    {
                        var emotionIDs = representation.GetEmotionIDs();
                        if (emotionIDs != null && emotionIDs.Count > 0)
                        {
                            // Add height for emotion dropdown
                            height += EditorGUIUtility.singleLineHeight + 2f;
                        }
                    }
                }
            }
        }
    }
    else
    {
        // Primary character logic - existing height calculation
        height += 2 * (EditorGUIUtility.singleLineHeight + 2f); // For representation and emotion dropdowns

        SerializedProperty selectedRepProp = property.FindPropertyRelative("SelectedRepresentation");
        if (selectedRepProp?.objectReferenceValue is CharacterRepresentationBase characterRepresentation)
        {
            List<string> emotionIDs = characterRepresentation.GetEmotionIDs();
            if (emotionIDs != null && emotionIDs.Count > 0)
            {
                height += EditorGUIUtility.singleLineHeight + 2f;
            }
        }

        // Add height for inline preview (if supported)
        if (selectedRepProp?.objectReferenceValue is IEditorPreviewableRepresentation previewableRepresentation)
        {
            height += previewableRepresentation.GetPreviewHeight() + 2f;
        }
    }

    return height;
}    }
}