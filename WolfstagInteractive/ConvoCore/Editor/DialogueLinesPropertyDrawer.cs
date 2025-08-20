using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [CustomPropertyDrawer(typeof(ConvoCoreConversationData.DialogueLineInfo))]
    public class DialogueLinesPropertyDrawer : PropertyDrawer
    {
        // Add static field to store foldout states
        private static Dictionary<string, bool> _characterRepresentationFoldouts = new Dictionary<string, bool>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            // Track if any changes are made
            EditorGUI.BeginChangeCheck();

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
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();

                // Get the conversation data object and validate
                var conversationData = property.serializedObject.targetObject as ConvoCoreConversationData;
                if (conversationData != null)
                {
                    conversationData.ValidateAndFixDialogueLines();
                    EditorUtility.SetDirty(conversationData);
                }
            }

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
            // Add height for character representation section
            totalHeight += GetCharacterRepresentationSectionHeight(property);

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
            // Create a unique key for this property's foldout state
            string foldoutKey =
                $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_CharacterRep";

            // Get or initialize the foldout state
            if (!_characterRepresentationFoldouts.ContainsKey(foldoutKey))
            {
                _characterRepresentationFoldouts[foldoutKey] = true; // Default to expanded
            }

            // Draw the foldout header with bold style
            bool isExpanded = _characterRepresentationFoldouts[foldoutKey];
            EditorGUI.BeginChangeCheck();

            // Create a header style that's bold
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;

            bool newExpanded = EditorGUI.Foldout(rect, isExpanded, "Character Representations", true, foldoutStyle);

            if (EditorGUI.EndChangeCheck())
            {
                _characterRepresentationFoldouts[foldoutKey] = newExpanded;
                // Force repaint to update immediately
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            // Only draw the content if expanded
            if (newExpanded)
            {
                // Draw a separator line
                rect.y += spacing;
                Rect separatorRect = new Rect(rect.x, rect.y, rect.width, 1);
                EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 1));
                rect.y += 1 + spacing * 2;

                // Check for valid profiles before drawing character representation fields
                SerializedObject serializedObject = property.serializedObject;
                var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;

                if (conversationObject == null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.LabelField(rect, "Error: Conversation data is missing.");
                    EditorGUI.indentLevel--;
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                    return rect;
                }

                // Filter valid profiles, ensuring null check
                var validProfiles = conversationObject.ConversationParticipantProfiles
                    .Where(p => p != null)
                    .ToList();

                if (validProfiles.Count == 0)
                {
                    EditorGUI.indentLevel++;

                    // Show a helpful message with styling
                    GUIStyle helpBoxStyle = new GUIStyle(EditorStyles.helpBox);
                    helpBoxStyle.fontSize = 11;
                    helpBoxStyle.wordWrap = true;

                    Rect helpBoxRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight * 2.5f);
                    EditorGUI.LabelField(helpBoxRect,
                        "No conversation participants are configured. Please add character profiles to the ConversationParticipantProfiles list in the Conversation Data to configure character representations.",
                        helpBoxStyle);

                    rect.y += EditorGUIUtility.singleLineHeight * 2.5f + spacing;

                    EditorGUI.indentLevel--;
                    return rect;
                }

                // Indent the content
                EditorGUI.indentLevel++;

                // Draw Primary Character Representation using CharacterID
                rect = DrawSingleCharacterRepresentation(
                    rect,
                    property.FindPropertyRelative("PrimaryCharacterRepresentation"),
                    "Primary Character",
                    property.FindPropertyRelative("characterID"),
                    spacing,
                    useRepresentationNameInsteadOfID: false);

                // Draw Secondary Character Representation
                rect = DrawSingleCharacterRepresentation(
                    rect,
                    property.FindPropertyRelative("SecondaryCharacterRepresentation"),
                    "Secondary Character",
                    property.FindPropertyRelative("SecondaryCharacterRepresentation"),
                    spacing,
                    useRepresentationNameInsteadOfID: true);

                // Draw Tertiary Character Representation
                rect = DrawSingleCharacterRepresentation(
                    rect,
                    property.FindPropertyRelative("TertiaryCharacterRepresentation"),
                    "Tertiary Character",
                    property.FindPropertyRelative("TertiaryCharacterRepresentation"),
                    spacing,
                    useRepresentationNameInsteadOfID: true);

                // Restore indent level
                EditorGUI.indentLevel--;

                // Add space after the expanded section
                rect.y += 5f;
            }
            else
            {
                // Add a small space after collapsed foldout for better visual separation
                rect.y += 2f;
            }

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
            SerializedProperty selectedRepNameProp =
                representationProp.FindPropertyRelative("SelectedRepresentationName");
            SerializedProperty selectedRepEmotionProp =
                representationProp.FindPropertyRelative("SelectedRepresentationEmotion");

            // Draw Header (no longer need bold style since it's inside a foldout)
            EditorGUI.LabelField(rect, $"{label}:");
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

            CharacterRepresentationBase selectedRepresentation = null;
            string currentEmotionID = selectedRepEmotionProp.stringValue;

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

                int newProfileIndex =
                    EditorGUI.Popup(rect, $"{label} Profile:", currentProfileIndex, profileNames.ToArray());
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

                    // Get the selected representation for display options
                    selectedRepresentation = currentProfile.GetRepresentation(newRepresentationName);

                    // Show emotion dropdown
                    if (selectedRepresentation != null)
                    {
                        var emotionIDs = selectedRepresentation.GetEmotionIDs();
                        if (emotionIDs != null && emotionIDs.Count > 0)
                        {
                            currentEmotionID = selectedRepEmotionProp.stringValue;
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
                                currentEmotionID = emotionIDs[newEmotionIndex];
                                serializedObject.ApplyModifiedProperties();
                            }

                            rect.y += EditorGUIUtility.singleLineHeight + spacing;
                        }
                    }
                }
            }
            else
            {
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

                    participantIndex =
                        EditorGUI.Popup(rect, $"{label} Participant:", participantIndex, participantNames);
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

                    // If no representation name is set, try to get it from the old SelectedRepresentation field
                    if (string.IsNullOrEmpty(selectedRepresentationName))
                    {
                        SerializedProperty selectedRepProp =
                            representationProp.FindPropertyRelative("SelectedRepresentation");
                        if (selectedRepProp != null &&
                            selectedRepProp.objectReferenceValue is CharacterRepresentationBase oldRep)
                        {
                            // Find the representation name from the profile's representations list
                            var matchingPair =
                                profile.Representations.FirstOrDefault(r => r.CharacterRepresentationType == oldRep);
                            if (matchingPair != null)
                            {
                                selectedRepresentationName = matchingPair.CharacterRepresentationName;
                                selectedRepNameProp.stringValue = selectedRepresentationName;
                                serializedObject.ApplyModifiedProperties();
                            }
                        }
                    }

                    int repIndex = Mathf.Max(0, representationNames.IndexOf(selectedRepresentationName));
                    repIndex = EditorGUI.Popup(rect, "Representation:", repIndex, representationNames.ToArray());
                    string newRepresentationName = representationNames[repIndex];
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;

                    if (newRepresentationName != selectedRepresentationName)
                    {
                        selectedRepNameProp.stringValue = newRepresentationName;

                        // Also update the old SelectedRepresentation field for backward compatibility
                        SerializedProperty selectedRepProp =
                            representationProp.FindPropertyRelative("SelectedRepresentation");
                        if (selectedRepProp != null)
                        {
                            var newRepresentation = profile.GetRepresentation(newRepresentationName);
                            selectedRepProp.objectReferenceValue = newRepresentation;
                        }

                        selectedRepEmotionProp.stringValue = "";
                        serializedObject.ApplyModifiedProperties();
                    }

                    // Get the selected representation for display options
                    selectedRepresentation = profile.GetRepresentation(newRepresentationName);

                    // Show emotion dropdown
                    if (selectedRepresentation != null)
                    {
                        var emotionIDs = selectedRepresentation.GetEmotionIDs();
                        if (emotionIDs != null && emotionIDs.Count > 0)
                        {
                            currentEmotionID = selectedRepEmotionProp.stringValue;
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
                                currentEmotionID = emotionIDs[newEmotionIndex];
                                serializedObject.ApplyModifiedProperties();
                            }

                            rect.y += EditorGUIUtility.singleLineHeight + spacing;
                        }
                    }
                }
            }

            // NEW SECTION: Display representation-specific display options
            if (selectedRepresentation != null && !string.IsNullOrEmpty(currentEmotionID))
            {
                rect = DrawRepresentationSpecificOptions(rect, representationProp, selectedRepresentation,
                    currentEmotionID, spacing);
            }

            return rect;
        }

        /// <summary>
        /// Draws representation-specific display options if the representation supports it
        /// </summary>
        private Rect DrawRepresentationSpecificOptions(Rect rect, SerializedProperty representationProp,
            CharacterRepresentationBase representation, string emotionID, float spacing)
        {
#if UNITY_EDITOR
            // Check if this representation supports custom dialogue line options
            if (representation is IDialogueLineEditorCustomizable customizable)
            {
                // Get the LineSpecificDisplayOptions property
                var displayOptionsProp = representationProp.FindPropertyRelative("LineSpecificDisplayOptions");
                if (displayOptionsProp != null)
                {
                    rect = customizable.DrawDialogueLineOptions(rect, emotionID, displayOptionsProp, spacing);
                }
            }
#endif

            return rect;
        }

        /// <summary>
        /// Calculates additional height needed for representation-specific display options
        /// </summary>
        private float GetRepresentationSpecificOptionsHeight(CharacterRepresentationBase representation,
            string emotionID, SerializedProperty representationProp)
        {
#if UNITY_EDITOR
            if (representation is IDialogueLineEditorCustomizable customizable)
            {
                var displayOptionsProp = representationProp.FindPropertyRelative("LineSpecificDisplayOptions");
                if (displayOptionsProp != null)
                {
                    return customizable.GetDialogueLineOptionsHeight(emotionID, displayOptionsProp);
                }
            }
#endif

            return 0f;
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


        private float GetCharacterRepresentationSectionHeight(SerializedProperty property)
        {
            float height = 0f;
            float spacing = 2f;

            // Always add height for the foldout header
            height += EditorGUIUtility.singleLineHeight + spacing;

            // Create a unique key for this property's foldout state
            string foldoutKey =
                $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_CharacterRep";

            // Check if the foldout is expanded
            if (_characterRepresentationFoldouts.ContainsKey(foldoutKey) &&
                _characterRepresentationFoldouts[foldoutKey])
            {
                // Add height for separator line + spacing
                height += 1 + spacing * 3;

                // Check if we have valid profiles
                SerializedObject serializedObject = property.serializedObject;
                var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;

                if (conversationObject == null)
                {
                    // Add height for error message
                    height += EditorGUIUtility.singleLineHeight + spacing;
                }
                else
                {
                    var validProfiles = conversationObject.ConversationParticipantProfiles?.Where(p => p != null)
                        .ToList();

                    if (validProfiles == null || validProfiles.Count == 0)
                    {
                        // Add height for the help message (2.5 lines)
                        height += EditorGUIUtility.singleLineHeight * 2.5f + spacing;
                    }
                    else
                    {
                        // Add heights for individual character representations when expanded
                        height += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("PrimaryCharacterRepresentation"), property, false);
                        height += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("SecondaryCharacterRepresentation"), property, true);
                        height += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("TertiaryCharacterRepresentation"), property, true);
                    }
                }

                // Add final space after expanded content
                height += 5f;
            }
            else
            {
                // Add small space after collapsed foldout
                height += 2f;
            }

            return height;
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

            // Add height for the section header within foldout (just character label)
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
                        SerializedProperty selectedRepNameProp =
                            property.FindPropertyRelative("SelectedRepresentationName");
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

                                    // Add height for representation-specific display options
                                    SerializedProperty selectedRepEmotionProp = property.FindPropertyRelative("SelectedRepresentationEmotion");
                                    if (selectedRepEmotionProp != null && !string.IsNullOrEmpty(selectedRepEmotionProp.stringValue))
                                    {
                                        height += GetRepresentationSpecificOptionsHeight(representation, selectedRepEmotionProp.stringValue, property);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Primary character logic
                height += 2 * (EditorGUIUtility.singleLineHeight + 2f); // For representation and emotion dropdowns

                SerializedProperty selectedRepProp = property.FindPropertyRelative("SelectedRepresentation");
                if (selectedRepProp?.objectReferenceValue is CharacterRepresentationBase characterRepresentation)
                {
                    List<string> emotionIDs = characterRepresentation.GetEmotionIDs();
                    if (emotionIDs != null && emotionIDs.Count > 0)
                    {
                        height += EditorGUIUtility.singleLineHeight + 2f;

                        // Add height for representation-specific display options
                        SerializedProperty selectedRepEmotionProp = property.FindPropertyRelative("SelectedRepresentationEmotion");
                        if (selectedRepEmotionProp != null && !string.IsNullOrEmpty(selectedRepEmotionProp.stringValue))
                        {
                            height += GetRepresentationSpecificOptionsHeight(characterRepresentation, selectedRepEmotionProp.stringValue, property);
                        }
                    }
                }

                // Add height for inline preview (if supported)
                if (selectedRepProp?.objectReferenceValue is IEditorPreviewableRepresentation previewableRepresentation)
                {
                    height += previewableRepresentation.GetPreviewHeight() + 2f;
                }
            }

            return height;
        }

        /// <summary>
        ///Calculates height for a single character representation, including representation-specific options
        /// </summary>
        private float GetSingleCharacterRepresentationHeight(SerializedProperty property, SerializedProperty mainProperty, bool useRepresentationNameInsteadOfID)
        {
            if (property == null) return 0f;

            float height = 0f;
            float spacing = 2f;
            
            // Add height for character label
            height += EditorGUIUtility.singleLineHeight + spacing;

            SerializedObject serializedObject = property.serializedObject;
            var conversationObject = serializedObject.targetObject as ConvoCoreConversationData;

            if (conversationObject == null)
            {
                return height + EditorGUIUtility.singleLineHeight + spacing; // Just for error message
            }

            var validProfiles = conversationObject.ConversationParticipantProfiles?.Where(p => p != null).ToList();
            if (validProfiles == null || validProfiles.Count == 0)
            {
                return height + EditorGUIUtility.singleLineHeight + spacing; // Just for "no participants" message
            }

            CharacterRepresentationBase selectedRepresentation = null;
            string currentEmotionID = "";

            if (useRepresentationNameInsteadOfID)
            {
                // Secondary/Tertiary character logic
                var selectedCharacterIDProp = property.FindPropertyRelative("SelectedCharacterID");
                var selectedRepNameProp = property.FindPropertyRelative("SelectedRepresentationName");
                var selectedRepEmotionProp = property.FindPropertyRelative("SelectedRepresentationEmotion");

                // Profile selection dropdown
                height += EditorGUIUtility.singleLineHeight + spacing;

                string selectedCharacterID = selectedCharacterIDProp?.stringValue ?? "";
                if (!string.IsNullOrEmpty(selectedCharacterID))
                {
                    var selectedProfile = validProfiles.FirstOrDefault(p => p.CharacterID == selectedCharacterID);
                    if (selectedProfile != null)
                    {
                        // Representation dropdown
                        height += EditorGUIUtility.singleLineHeight + spacing;

                        string selectedRepresentationName = selectedRepNameProp?.stringValue ?? "";
                        if (!string.IsNullOrEmpty(selectedRepresentationName))
                        {
                            selectedRepresentation = selectedProfile.GetRepresentation(selectedRepresentationName);
                            if (selectedRepresentation != null)
                            {
                                var emotionIDs = selectedRepresentation.GetEmotionIDs();
                                if (emotionIDs != null && emotionIDs.Count > 0)
                                {
                                    // Emotion dropdown
                                    height += EditorGUIUtility.singleLineHeight + spacing;
                                    currentEmotionID = selectedRepEmotionProp?.stringValue ?? "";
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Primary character logic
                var characterIDProp = mainProperty.FindPropertyRelative("characterID");
                var selectedRepNameProp = property.FindPropertyRelative("SelectedRepresentationName");
                var selectedRepEmotionProp = property.FindPropertyRelative("SelectedRepresentationEmotion");

                string characterID = characterIDProp?.stringValue ?? "";
                var profile = validProfiles.FirstOrDefault(p => p.CharacterID == characterID);

                if (profile != null)
                {
                    // Representation dropdown
                    height += EditorGUIUtility.singleLineHeight + spacing;

                    string selectedRepresentationName = selectedRepNameProp?.stringValue ?? "";
                    if (!string.IsNullOrEmpty(selectedRepresentationName))
                    {
                        selectedRepresentation = profile.GetRepresentation(selectedRepresentationName);
                        if (selectedRepresentation != null)
                        {
                            var emotionIDs = selectedRepresentation.GetEmotionIDs();
                            if (emotionIDs != null && emotionIDs.Count > 0)
                            {
                                // Emotion dropdown
                                height += EditorGUIUtility.singleLineHeight + spacing;
                                currentEmotionID = selectedRepEmotionProp?.stringValue ?? "";
                            }
                        }
                    }
                }
                else
                {
                    // Just participant selection dropdown
                    height += EditorGUIUtility.singleLineHeight + spacing;
                }
            }

            // Add height for representation-specific display options
            if (selectedRepresentation != null && !string.IsNullOrEmpty(currentEmotionID))
            {
                height += GetRepresentationSpecificOptionsHeight(selectedRepresentation, currentEmotionID, property);
            }

            return height;
        }
    }
}