using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1DialogueLinesPropertyDrawer.html")]
[CustomPropertyDrawer(typeof(ConvoCoreConversationData.DialogueLineInfo))]
    public class DialogueLinesPropertyDrawer : PropertyDrawer
    {
        // Foldout states per line
        private static readonly Dictionary<string, bool> CharacterRepresentationFoldouts = new();
        private static readonly Dictionary<string, bool> DialogueLineFoldouts = new();

public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            Rect currentRect = new Rect(position.x, position.y, position.width, lineHeight);

            // Main foldout with preview text
            string foldoutKey = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_Line";
            if (!DialogueLineFoldouts.ContainsKey(foldoutKey))
                DialogueLineFoldouts[foldoutKey] = true;

            bool isExpanded = DialogueLineFoldouts[foldoutKey];
            
            // Get preview text for collapsed state
            string previewText = GetPreviewText(property);
            
            var boldFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            bool newExpanded = EditorGUI.Foldout(currentRect, isExpanded, 
                isExpanded ? "Dialogue Line" : $"Dialogue Line: {previewText}", 
                true, boldFoldout);
            
            if (newExpanded != isExpanded)
            {
                DialogueLineFoldouts[foldoutKey] = newExpanded;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            
            currentRect.y += lineHeight + spacing;

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                
                currentRect = DrawBasicInfo(currentRect, property, spacing);
                currentRect = DrawInputMethod(currentRect, property, spacing);
                currentRect = DrawAudioClip(currentRect, property, spacing);
                currentRect = DrawLocalizedDialogues(currentRect, property, spacing);
                currentRect = DrawActionsList(currentRect, property, spacing);
                currentRect = DrawCharacterRepresentation(currentRect, property, spacing);
                
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();

                if (property.serializedObject.targetObject is ConvoCoreConversationData conversationData)
                {
                    conversationData.ValidateAndFixDialogueLines();
                    EditorUtility.SetDirty(conversationData);
                }
            }

            EditorGUI.EndProperty();
        }
        private string GetPreviewText(SerializedProperty property)
        {
            var localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            if (localizedDialoguesProp != null && localizedDialoguesProp.isArray && localizedDialoguesProp.arraySize > 0)
            {
                string lang = ConvoCoreLanguageManager.Instance?.CurrentLanguage ?? "EN";
                
                for (int i = 0; i < localizedDialoguesProp.arraySize; i++)
                {
                    var el = localizedDialoguesProp.GetArrayElementAtIndex(i);
                    var langProp = el.FindPropertyRelative("Language");
                    var textProp = el.FindPropertyRelative("Text");
                    
                    if (langProp != null && textProp != null && 
                        string.Equals(langProp.stringValue, lang, StringComparison.OrdinalIgnoreCase))
                    {
                        string text = textProp.stringValue ?? "";
                        // Truncate if too long
                        return text.Length > 60 ? text.Substring(0, 60) + "..." : text;
                    }
                }
                
                // Fallback to first available
                var firstEl = localizedDialoguesProp.GetArrayElementAtIndex(0);
                var firstText = firstEl.FindPropertyRelative("Text");
                if (firstText != null)
                {
                    string text = firstText.stringValue ?? "";
                    return text.Length > 60 ? text.Substring(0, 60) + "..." : text;
                }
            }
            
            return "(No dialogue text)";
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float spacing = 2f;
            float total = EditorGUIUtility.singleLineHeight + spacing; // Main foldout
            
            string foldoutKey = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_Line";
            if (DialogueLineFoldouts.TryGetValue(foldoutKey, out bool isExpanded) && isExpanded)
            {
                total += GetBasicInfoHeight();
                total += GetInputMethodHeight(property);
                total += GetAudioClipHeight(property);
                total += GetLocalizedDialoguesHeight(property);
                total += GetActionsListHeight(property);
                total += GetCharacterRepresentationSectionHeight(property);
            }
            
            return total;
        }
        private static float GetPreviewBlockHeight(IEditorPreviewableRepresentation previewable)
        {
            if (previewable == null) return 0f;
            // ask the rep, then clamp to a stable range (so lists don't jump)
            float desired = previewable.GetPreviewHeight();
            if (desired <= 0f) return 0f;
            return Mathf.Clamp(desired, 100f, 120f);
        }
       
        // ----- Sections -----

        private Rect DrawBasicInfo(Rect rect, SerializedProperty property, float spacing)
        {
            var keyProp = property.FindPropertyRelative("ConversationID");
            var indexProp = property.FindPropertyRelative("ConversationLineIndex");
            var characterIDProp = property.FindPropertyRelative("characterID");

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
            var selectedLineProgressionProp = property.FindPropertyRelative("UserInputMethod");
            var timedValueProp = property.FindPropertyRelative("TimeBeforeNextLine");

            var method = (ConvoCoreConversationData.DialogueLineProgressionMethod)selectedLineProgressionProp.enumValueIndex;
            method = (ConvoCoreConversationData.DialogueLineProgressionMethod)EditorGUI.EnumPopup(
                rect, "Input Method:", method);
            selectedLineProgressionProp.enumValueIndex = (int)method;
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            if (method == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
            {
                EditorGUI.PropertyField(rect, timedValueProp, new GUIContent("Display Duration (seconds):"));
                rect.y += EditorGUIUtility.singleLineHeight + spacing;
            }

            return rect;
        }

        private Rect DrawAudioClip(Rect rect, SerializedProperty property, float spacing)
        {
            var clipProp = property.FindPropertyRelative("clip");
            float h = EditorGUI.GetPropertyHeight(clipProp);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, h), clipProp, new GUIContent("Audio Clip:"));
            rect.y += h + spacing;
            return rect;
        }

        private Rect DrawLocalizedDialogues(Rect rect, SerializedProperty property, float spacing)
        {
            var localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            
            if (localizedDialoguesProp == null || !localizedDialoguesProp.isArray)
            {
                EditorGUI.LabelField(rect, "Localized Dialogues not available.");
                rect.y += EditorGUIUtility.singleLineHeight + spacing;
                return rect;
            }

            string lang = ConvoCoreLanguageManager.Instance?.CurrentLanguage ?? "EN";
            SerializedProperty match = null;

            // Try case-insensitive match first
            for (int i = 0; i < localizedDialoguesProp.arraySize; i++)
            {
                var el = localizedDialoguesProp.GetArrayElementAtIndex(i);
                var langProp = el.FindPropertyRelative("Language");
                if (langProp != null && 
                    string.Equals(langProp.stringValue, lang, StringComparison.OrdinalIgnoreCase))
                {
                    match = el;
                    break;
                }
            }

            if (match != null)
            {
                var textProp = match.FindPropertyRelative("Text");
                if (textProp != null)
                {
                    EditorGUI.LabelField(rect, $"Localized Dialogue ({lang}):");
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                    
                    // Use a word-wrapped label style for better readability
                    var textStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
                    float textHeight = textStyle.CalcHeight(new GUIContent(textProp.stringValue), rect.width);
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, textHeight),
                        textProp.stringValue, textStyle);
                    rect.y += textHeight + spacing;
                }
                else
                {
                    EditorGUI.LabelField(rect, $"Localized Dialogue ({lang}): (Text field not found)");
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                }
            }
            else if (localizedDialoguesProp.arraySize > 0)
            {
                // Fallback to first available
                var firstEl = localizedDialoguesProp.GetArrayElementAtIndex(0);
                var firstLangProp = firstEl.FindPropertyRelative("Language");
                var firstTextProp = firstEl.FindPropertyRelative("Text");
                
                if (firstLangProp != null && firstTextProp != null)
                {
                    string fallbackLang = firstLangProp.stringValue ?? "Unknown";
                    EditorGUI.LabelField(rect, $"Localized Dialogue (Fallback: {fallbackLang}):");
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                    
                    var textStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
                    float textHeight = textStyle.CalcHeight(new GUIContent(firstTextProp.stringValue), rect.width);
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, textHeight),
                        firstTextProp.stringValue, textStyle);
                    rect.y += textHeight + spacing;
                }
                else
                {
                    EditorGUI.LabelField(rect, $"No dialogue available for language: {lang}");
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                }
            }
            else
            {
                EditorGUI.LabelField(rect, $"No dialogue available for language: {lang}");
                rect.y += EditorGUIUtility.singleLineHeight + spacing;
            }
            
            return rect;
        }
        private Rect DrawActionsList(Rect rect, SerializedProperty property, float spacing)
        {
            var beforeProp = property.FindPropertyRelative("ActionsBeforeDialogueLine");
            var afterProp  = property.FindPropertyRelative("ActionsAfterDialogueLine");

            EditorGUI.PropertyField(rect, beforeProp, new GUIContent("Actions Before Line:"), true);
            rect.y += EditorGUI.GetPropertyHeight(beforeProp, true) + spacing;

            EditorGUI.PropertyField(rect, afterProp, new GUIContent("Actions After Line:"), true);
            rect.y += EditorGUI.GetPropertyHeight(afterProp, true) + spacing;

            return rect;
        }

        private Rect DrawCharacterRepresentation(Rect rect, SerializedProperty property, float spacing)
        {
            string foldoutKey = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_CharacterRep";
            if (!CharacterRepresentationFoldouts.ContainsKey(foldoutKey))
                CharacterRepresentationFoldouts[foldoutKey] = true;

            bool isExpanded = CharacterRepresentationFoldouts[foldoutKey];

            var boldFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            bool newExpanded = EditorGUI.Foldout(rect, isExpanded, "Character Representations", true, boldFoldout);
            if (newExpanded != isExpanded)
            {
                CharacterRepresentationFoldouts[foldoutKey] = newExpanded;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            if (newExpanded)
            {
                rect.y += spacing;
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0.5f, 0.5f, 0.5f, 1));
                rect.y += 1 + spacing * 2;

                var convo = property.serializedObject.targetObject as ConvoCoreConversationData;
                if (convo == null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.LabelField(rect, "Error: Conversation data is missing.");
                    rect.y += EditorGUIUtility.singleLineHeight + spacing;
                    EditorGUI.indentLevel--;
                    return rect;
                }

                var validProfiles = convo.ConversationParticipantProfiles.Where(p => p != null).ToList();
                if (validProfiles.Count == 0)
                {
                    EditorGUI.indentLevel++;
                    var helpBox = new GUIStyle(EditorStyles.helpBox) { fontSize = 11, wordWrap = true };
                    Rect helpRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight * 2.5f);
                    EditorGUI.LabelField(helpRect,
                        "No conversation participants are configured. Please add character profiles to the ConversationParticipantProfiles list in the Conversation Data to configure character representations.",
                        helpBox);
                    rect.y += EditorGUIUtility.singleLineHeight * 2.5f + spacing;
                    EditorGUI.indentLevel--;
                    return rect;
                }

                EditorGUI.indentLevel++;

                rect = DrawSingleCharacterRepresentation(
                    rect,
                    property.FindPropertyRelative("PrimaryCharacterRepresentation"),
                    "Primary Character",
                    property.FindPropertyRelative("characterID"),
                    spacing,
                    useRepresentationNameInsteadOfID: false);

                rect = DrawSingleCharacterRepresentation(
                    rect,
                    property.FindPropertyRelative("SecondaryCharacterRepresentation"),
                    "Secondary Character",
                    property.FindPropertyRelative("SecondaryCharacterRepresentation"),
                    spacing,
                    useRepresentationNameInsteadOfID: true);

                rect = DrawSingleCharacterRepresentation(
                    rect,
                    property.FindPropertyRelative("TertiaryCharacterRepresentation"),
                    "Tertiary Character",
                    property.FindPropertyRelative("TertiaryCharacterRepresentation"),
                    spacing,
                    useRepresentationNameInsteadOfID: true);

                EditorGUI.indentLevel--;
                rect.y += 5f;
            }
            else
            {
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
    var so = representationProp.serializedObject;

    var selectedCharacterIDProp = representationProp.FindPropertyRelative("SelectedCharacterID");
    var selectedRepNameProp     = representationProp.FindPropertyRelative("SelectedRepresentationName");
    var selectedRepProp         = representationProp.FindPropertyRelative("SelectedRepresentation");
    var selectedEmotionGuidProp = representationProp.FindPropertyRelative("SelectedEmotionId"); // GUID

    // Section header
    EditorGUI.LabelField(rect, $"{label}:");
    rect.y += EditorGUIUtility.singleLineHeight + spacing;

    // Resolve conversation + profiles
    var convo = so.targetObject as ConvoCoreConversationData;
    if (convo == null)
    {
        EditorGUI.LabelField(rect, "Error: Conversation data is missing.");
        rect.y += EditorGUIUtility.singleLineHeight + spacing;
        return rect;
    }
    var validProfiles = convo.ConversationParticipantProfiles.Where(p => p != null).ToList();
    if (validProfiles.Count == 0)
    {
        EditorGUI.LabelField(rect, "No participants available.");
        rect.y += EditorGUIUtility.singleLineHeight + spacing;
        return rect;
    }

    CharacterRepresentationBase selectedRepresentation = null;

    if (useRepresentationNameInsteadOfID)
    {
        // -------- Secondary / Tertiary --------
        string currentCharacterID = selectedCharacterIDProp.stringValue;
        var currentProfile = !string.IsNullOrEmpty(currentCharacterID)
            ? validProfiles.FirstOrDefault(p => p.CharacterID == currentCharacterID)
            : null;

        // Profile popup
        var profileNames = new List<string> {"None"};
        profileNames.AddRange(validProfiles.Where(p => !string.IsNullOrEmpty(p.CharacterName)).Select(p => p.CharacterName));

        int currentProfileIndex = 0;
        if (currentProfile != null)
        {
            int idx = profileNames.IndexOf(currentProfile.CharacterName);
            if (idx > 0) currentProfileIndex = idx;
        }

        int newProfileIndex = EditorGUI.Popup(rect, $"{label} Profile:", currentProfileIndex, profileNames.ToArray());
        rect.y += EditorGUIUtility.singleLineHeight + spacing;

        if (newProfileIndex != currentProfileIndex)
        {
            if (newProfileIndex == 0) // None
            {
                selectedCharacterIDProp.stringValue = "";
                selectedRepNameProp.stringValue     = "";
                selectedEmotionGuidProp.stringValue = "";
                if (selectedRepProp != null) selectedRepProp.objectReferenceValue = null;
                so.ApplyModifiedProperties();
                return rect;
            }
            else
            {
                var selProfile = validProfiles[newProfileIndex - 1];
                selectedCharacterIDProp.stringValue = selProfile.CharacterID;

                var firstRep = selProfile.Representations?.FirstOrDefault(r => r != null);
                selectedRepNameProp.stringValue     = firstRep?.CharacterRepresentationName ?? "";
                selectedEmotionGuidProp.stringValue = "";
                if (selectedRepProp != null)
                    selectedRepProp.objectReferenceValue = firstRep?.CharacterRepresentationType;
                so.ApplyModifiedProperties();
                currentProfile = selProfile;
            }
        }
        if (currentProfile == null) return rect;

        // Representation popup
        var repNames = currentProfile.Representations
            .Where(r => r != null && !string.IsNullOrEmpty(r.CharacterRepresentationName))
            .Select(r => r.CharacterRepresentationName)
            .ToList();

        if (repNames.Count > 0)
        {
            string repName = selectedRepNameProp.stringValue;
            int repIdx = Mathf.Max(0, repNames.IndexOf(repName));
            repIdx = EditorGUI.Popup(rect, "Representation:", repIdx, repNames.ToArray());
            string newRepName = repNames[repIdx];
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            if (newRepName != repName)
            {
                selectedRepNameProp.stringValue     = newRepName;
                selectedEmotionGuidProp.stringValue = "";
                if (selectedRepProp != null)
                    selectedRepProp.objectReferenceValue = currentProfile.GetRepresentation(newRepName);
                so.ApplyModifiedProperties();
            }

            selectedRepresentation = currentProfile.GetRepresentation(newRepName);

            // Emotion (GUID) – attribute-backed popup
            if (selectedEmotionGuidProp != null)
            {
                float emoH = EditorGUI.GetPropertyHeight(selectedEmotionGuidProp, true);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, emoH),
                    selectedEmotionGuidProp, new GUIContent("Emotion"), true);
                rect.y += emoH + spacing;

            }

            // Inline preview for Secondary/Tertiary as well
            IEditorPreviewableRepresentation previewable = null;
            if (selectedRepProp != null && selectedRepProp.objectReferenceValue is IEditorPreviewableRepresentation prA)
                previewable = prA;
            else if (selectedRepresentation is IEditorPreviewableRepresentation prB)
                previewable = prB;

            if (previewable != null)
                rect = DrawInlinePreviewBlock(rect, previewable, selectedEmotionGuidProp?.stringValue ?? "", spacing);
        }
    }
    else
    {
        // -------- Primary --------
        string characterID = identifierProp.stringValue;
        var profile = validProfiles.FirstOrDefault(p => p.CharacterID == characterID);
        if (profile == null)
        {
            // Let user select participant if missing
            var names = validProfiles.Where(p => !string.IsNullOrEmpty(p.CharacterName)).Select(p => p.CharacterName).ToArray();
            int idx = 0;
            idx = EditorGUI.Popup(rect, $"{label} Participant:", idx, names);
            rect.y += EditorGUIUtility.singleLineHeight + spacing;
            if (names.Length > 0)
            {
                var chosen = validProfiles[idx];
                identifierProp.stringValue = chosen.CharacterID;
                so.ApplyModifiedProperties();
            }
            return rect;
        }

        // Representation popup
        var repNames = profile.Representations
            .Where(r => r != null && !string.IsNullOrEmpty(r.CharacterRepresentationName))
            .Select(r => r.CharacterRepresentationName)
            .ToList();

        if (repNames.Count > 0)
        {
            string repName = representationProp.FindPropertyRelative("SelectedRepresentationName").stringValue;

            // Back-compat: if the name is empty but object exists, infer name
            if (string.IsNullOrEmpty(repName) && selectedRepProp?.objectReferenceValue is CharacterRepresentationBase oldObj)
            {
                var match = profile.Representations.FirstOrDefault(r => r.CharacterRepresentationType == oldObj);
                if (match != null)
                {
                    repName = match.CharacterRepresentationName;
                    representationProp.FindPropertyRelative("SelectedRepresentationName").stringValue = repName;
                    so.ApplyModifiedProperties();
                }
            }

            int repIdx = Mathf.Max(0, repNames.IndexOf(repName));
            repIdx = EditorGUI.Popup(rect, "Representation:", repIdx, repNames.ToArray());
            string newRepName = repNames[repIdx];
            rect.y += EditorGUIUtility.singleLineHeight + spacing;

            if (newRepName != repName)
            {
                representationProp.FindPropertyRelative("SelectedRepresentationName").stringValue = newRepName;
                // keep object field synced for any legacy code
                if (selectedRepProp != null)
                    selectedRepProp.objectReferenceValue = profile.GetRepresentation(newRepName);
                selectedEmotionGuidProp.stringValue = "";
                so.ApplyModifiedProperties();
            }

            selectedRepresentation = profile.GetRepresentation(newRepName);

            // Emotion (GUID) – attribute-backed popup
            if (selectedEmotionGuidProp != null)
            {
                float emoH = EditorGUI.GetPropertyHeight(selectedEmotionGuidProp, true);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, emoH),
                    selectedEmotionGuidProp, new GUIContent("Emotion"), true);
                rect.y += emoH + spacing;
            }

            // Inline preview (Primary)
            if (selectedRepProp?.objectReferenceValue is IEditorPreviewableRepresentation previewablePrimary)
            {
                rect = DrawInlinePreviewBlock(rect, previewablePrimary,selectedEmotionGuidProp?.stringValue ?? "", spacing);
            }
            else if (selectedRepresentation is IEditorPreviewableRepresentation previewableByType)
            {
                rect = DrawInlinePreviewBlock(rect, previewableByType,selectedEmotionGuidProp?.stringValue ?? "", spacing);
            }
        }
    }

    // Per-line representation-specific options (GUID passed through)
    if (selectedRepresentation != null && selectedEmotionGuidProp != null && !string.IsNullOrEmpty(selectedEmotionGuidProp.stringValue))
    {
        rect = DrawRepresentationSpecificOptions(rect, representationProp, selectedRepresentation,
            selectedEmotionGuidProp.stringValue, spacing);
    }

    return rect;
}


        private Rect DrawRepresentationSpecificOptions(
            Rect rect,
            SerializedProperty representationProp,
            CharacterRepresentationBase representation,
            string emotionGuid,
            float spacing)
        {
#if UNITY_EDITOR
            if (representation is IDialogueLineEditorCustomizable customizable)
            {
                var displayOptionsProp = representationProp.FindPropertyRelative("LineSpecificDisplayOptions");
                if (displayOptionsProp != null)
                {
                    rect = customizable.DrawDialogueLineOptions(rect, emotionGuid, displayOptionsProp, spacing);
                }
            }
#endif
            return rect;
        }

        private float GetRepresentationSpecificOptionsHeight(
            CharacterRepresentationBase representation,
            string emotionGuid,
            SerializedProperty representationProp)
        {
#if UNITY_EDITOR
            if (representation is IDialogueLineEditorCustomizable customizable)
            {
                var displayOptionsProp = representationProp.FindPropertyRelative("LineSpecificDisplayOptions");
                if (displayOptionsProp != null)
                {
                    return customizable.GetDialogueLineOptionsHeight(emotionGuid, displayOptionsProp);
                }
            }
#endif
            return 0f;
        }

        // ----- Heights -----

        private float GetBasicInfoHeight() =>
            3 * (EditorGUIUtility.singleLineHeight + 2f);

        private float GetInputMethodHeight(SerializedProperty property)
        {
            var methodProp = property.FindPropertyRelative("UserInputMethod");
            var method = (ConvoCoreConversationData.DialogueLineProgressionMethod)methodProp.enumValueIndex;
            float h = EditorGUIUtility.singleLineHeight + 2f;
            if (method == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
                h += EditorGUIUtility.singleLineHeight + 2f;
            return h;
        }

        private float GetAudioClipHeight(SerializedProperty property)
        {
            var clipProp = property.FindPropertyRelative("clip");
            return EditorGUI.GetPropertyHeight(clipProp) + 2f;
        }

        private float GetLocalizedDialoguesHeight(SerializedProperty property)
        {
            var localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            
            if (localizedDialoguesProp == null || !localizedDialoguesProp.isArray || localizedDialoguesProp.arraySize == 0)
            {
                return EditorGUIUtility.singleLineHeight + 2f;
            }

            float height = EditorGUIUtility.singleLineHeight + 2f; // Label line
            
            string lang = ConvoCoreLanguageManager.Instance?.CurrentLanguage ?? "EN";
            SerializedProperty match = null;

            // Try case-insensitive match
            for (int i = 0; i < localizedDialoguesProp.arraySize; i++)
            {
                var el = localizedDialoguesProp.GetArrayElementAtIndex(i);
                var langProp = el.FindPropertyRelative("Language");
                if (langProp != null && 
                    string.Equals(langProp.stringValue, lang, StringComparison.OrdinalIgnoreCase))
                {
                    match = el;
                    break;
                }
            }

            // Fallback to first if no match
            if (match == null && localizedDialoguesProp.arraySize > 0)
            {
                match = localizedDialoguesProp.GetArrayElementAtIndex(0);
            }

            if (match != null)
            {
                var textProp = match.FindPropertyRelative("Text");
                if (textProp != null && !string.IsNullOrEmpty(textProp.stringValue))
                {
                    // Calculate wrapped text height
                    var textStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
                    // Approximate width (accounting for indent and margins)
                    float approximateWidth = EditorGUIUtility.currentViewWidth - 80f;
                    float textHeight = textStyle.CalcHeight(new GUIContent(textProp.stringValue), approximateWidth);
                    height += textHeight + 2f;
                }
                else
                {
                    height += EditorGUIUtility.singleLineHeight + 2f;
                }
            }
            else
            {
                height += EditorGUIUtility.singleLineHeight + 2f;
            }
            
            return height;
        }

        private float GetActionsListHeight(SerializedProperty property)
        {
            var beforeProp = property.FindPropertyRelative("ActionsBeforeDialogueLine");
            var afterProp  = property.FindPropertyRelative("ActionsAfterDialogueLine");

            float h = 0f;
            h += EditorGUI.GetPropertyHeight(beforeProp, true);
            h += EditorGUI.GetPropertyHeight(afterProp,  true);
            return h + 2f;
        }

        private float GetCharacterRepresentationSectionHeight(SerializedProperty property)
        {
            float h = 0f;
            float spacing = 2f;

            h += EditorGUIUtility.singleLineHeight + spacing; // foldout

            string key = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_CharacterRep";
            if (CharacterRepresentationFoldouts.TryGetValue(key, out bool open) && open)
            {
                h += 1 + spacing * 3; // separator

                var convo = property.serializedObject.targetObject as ConvoCoreConversationData;
                if (convo == null)
                {
                    h += EditorGUIUtility.singleLineHeight + spacing;
                }
                else
                {
                    var profiles = convo.ConversationParticipantProfiles?.Where(p => p != null).ToList();
                    if (profiles == null || profiles.Count == 0)
                    {
                        h += EditorGUIUtility.singleLineHeight * 2.5f + spacing;
                    }
                    else
                    {
                        h += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("PrimaryCharacterRepresentation"), property, false);
                        h += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("SecondaryCharacterRepresentation"), property, true);
                        h += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("TertiaryCharacterRepresentation"),  property, true);
                    }
                }
                h += 5f; // padding
            }
            else
            {
                h += 2f;
            }

            return h;
        }
        private static Rect DrawInlinePreviewBlock(Rect rect, IEditorPreviewableRepresentation previewable,string emotionGuid, float spacing)
        {
            float h = GetPreviewBlockHeight(previewable);
            if (h <= 0f) return rect;

            var indented = EditorGUI.IndentedRect(new Rect(rect.x, rect.y, rect.width, h));
            const float pad = 4f;
            var outer = new Rect(indented.x, indented.y, indented.width, h);
            var inner = new Rect(outer.x + pad, outer.y + pad, outer.width - pad * 2f, outer.height - pad * 2f);

            // subtle background
            EditorGUI.DrawRect(outer, new Color(0f, 0f, 0f, 0.06f));

            // Resolve emotion mapping from the representation
            object emotionMapping = null;
            if (!string.IsNullOrEmpty(emotionGuid) && previewable is CharacterRepresentationBase repBase)
            {
                emotionMapping = repBase.GetEmotionMappingByGuid(emotionGuid);
            }

            // Let representation draw with the resolved emotion mapping
            previewable.DrawInlineEditorPreview(emotionMapping, inner);

            rect.y += h + spacing;
            return rect;
        }


       private float GetSingleCharacterRepresentationHeight(
    SerializedProperty repProp,
    SerializedProperty mainProperty,
    bool useRepresentationNameInsteadOfID)
{
    if (repProp == null) return 0f;

    float h = 0f;
    float spacing = 2f;

    h += EditorGUIUtility.singleLineHeight + spacing; // section label

    var convo = repProp.serializedObject.targetObject as ConvoCoreConversationData;
    if (convo == null) return h + EditorGUIUtility.singleLineHeight + spacing;

    var profiles = convo.ConversationParticipantProfiles?.Where(p => p != null).ToList();
    if (profiles == null || profiles.Count == 0) return h + EditorGUIUtility.singleLineHeight + spacing;

    var selectedRepNameProp     = repProp.FindPropertyRelative("SelectedRepresentationName");
    var selectedRepProp         = repProp.FindPropertyRelative("SelectedRepresentation");
    var selectedEmotionGuidProp = repProp.FindPropertyRelative("SelectedEmotionId");

    if (useRepresentationNameInsteadOfID)
    {
        var selectedCharacterIDProp = repProp.FindPropertyRelative("SelectedCharacterID");
        // profile popup
        h += EditorGUIUtility.singleLineHeight + spacing;

        string charId = selectedCharacterIDProp?.stringValue ?? "";
        var profile = !string.IsNullOrEmpty(charId) ? profiles.FirstOrDefault(p => p.CharacterID == charId) : null;
        if (profile != null)
        {
            // representation popup
            h += EditorGUIUtility.singleLineHeight + spacing;

            // emotion popup (GUID) – use the drawer's actual height
            float emoH = (selectedEmotionGuidProp != null)
                ? EditorGUI.GetPropertyHeight(selectedEmotionGuidProp, true)
                : EditorGUIUtility.singleLineHeight;
            h += emoH + spacing;

            // preview height (if any)
            IEditorPreviewableRepresentation previewable = null;
            if (selectedRepProp != null && selectedRepProp.objectReferenceValue is IEditorPreviewableRepresentation prA)
                previewable = prA;
            else
            {
                var repName = selectedRepNameProp?.stringValue ?? "";
                var repObj  = profile.GetRepresentation(repName);
                if (repObj is IEditorPreviewableRepresentation prB) previewable = prB;
            }

            h += GetPreviewBlockHeight(previewable) + spacing;

            // representation-specific options height
            string emoId = selectedEmotionGuidProp?.stringValue ?? "";
            var selName  = selectedRepNameProp?.stringValue ?? "";
            var repType  = profile.GetRepresentation(selName);
            if (repType != null && !string.IsNullOrEmpty(emoId))
            {
                h += GetRepresentationSpecificOptionsHeight(repType, emoId, repProp);
            }
        }
    }
    else
    {
        // Primary
        var characterIDProp = mainProperty.FindPropertyRelative("characterID");
        var profile = profiles.FirstOrDefault(p => p.CharacterID == (characterIDProp?.stringValue ?? ""));

        if (profile == null)
        {
            // participant popup only
            h += EditorGUIUtility.singleLineHeight + spacing;
            return h;
        }

        // representation popup
        h += EditorGUIUtility.singleLineHeight + spacing;

        // emotion popup (GUID) – use the drawer's actual height
        float emoH = (selectedEmotionGuidProp != null)
            ? EditorGUI.GetPropertyHeight(selectedEmotionGuidProp, true)
            : EditorGUIUtility.singleLineHeight;
        h += emoH + spacing;

        // preview height (if any)
        IEditorPreviewableRepresentation previewable = null;
        if (selectedRepProp?.objectReferenceValue is IEditorPreviewableRepresentation prA)
            previewable = prA;
        else
        {
            var repName = selectedRepNameProp?.stringValue ?? "";
            var repType = profile.GetRepresentation(repName);
            if (repType is IEditorPreviewableRepresentation prB) previewable = prB;
        }
        h += GetPreviewBlockHeight(previewable) + spacing;

        // representation-specific options height
        string emoId = selectedEmotionGuidProp?.stringValue ?? "";
        var repType2 = profile.GetRepresentation(selectedRepNameProp?.stringValue ?? "");
        if (repType2 != null && !string.IsNullOrEmpty(emoId))
        {
            h += GetRepresentationSpecificOptionsHeight(repType2, emoId, repProp);
        }
    }

    return h;
}

    }
}