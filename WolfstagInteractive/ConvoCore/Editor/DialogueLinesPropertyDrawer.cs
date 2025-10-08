using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [HelpURL(
        "https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1DialogueLinesPropertyDrawer.html")]
    [CustomPropertyDrawer(typeof(ConvoCoreConversationData.DialogueLineInfo))]
    public class DialogueLinesPropertyDrawer : PropertyDrawer
    {
        // ──────────────────────────────────────────────
        // Constants & Static Caches
        // ──────────────────────────────────────────────
        private const float k_Spacing = 2f;
        private const float k_Pad = 4f;
        private const float k_MinPreviewHeight = 100f;
        private const float k_MaxPreviewHeight = 120f;

        // Foldout states per line
        private static readonly Dictionary<string, bool> CharacterRepresentationFoldouts = new();
        private static readonly Dictionary<string, bool> DialogueLineFoldouts = new();

        // Cached styles
        private static readonly GUIStyle s_HeaderStyle = new(EditorStyles.boldLabel)
        {
            clipping = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft
        };

        private static readonly GUIStyle s_WrappedLabel = new(EditorStyles.label) { wordWrap = true };
        private static readonly GUIStyle s_HelpBox = new(EditorStyles.helpBox) { wordWrap = true, fontSize = 11 };
        private static readonly GUIStyle s_FoldoutBold = new(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

        // Header colors
        private static readonly Color s_HeaderColorPro = new(1f, 1f, 1f, 0.06f);
        private static readonly Color s_HeaderColorLight = new(0f, 0f, 0f, 0.08f);

        // Cached GUIContent
        private static readonly GUIContent GC_ConversationID = new("Conversation ID");
        private static readonly GUIContent GC_LineIndex = new("Line Index");
        private static readonly GUIContent GC_CharacterID = new("Character ID");
        private static readonly GUIContent GC_InputMethod = new("Input Method:");
        private static readonly GUIContent GC_DisplayDuration = new("Display Duration (seconds):");
        private static readonly GUIContent GC_AudioClip = new("Audio Clip:");
        private static readonly GUIContent GC_BeforeActions = new("Actions Before Line:");
        private static readonly GUIContent GC_AfterActions = new("Actions After Line:");
        private static readonly GUIContent GC_Emotion = new("Emotion");

        // Preview header text cache
        private static readonly Dictionary<int, string> s_PreviewCache = new(128);
        private static double s_LastCachePurgeTime;
        private static readonly GUIContent s_HeaderContent = new();

        private static int GetIndexFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return -1;

            // find last closing bracket
            int end = path.LastIndexOf(']');
            if (end <= 0)
                return -1;

            // find matching opening bracket
            int start = path.LastIndexOf('[', end);
            if (start < 0 || end - start < 2)
                return -1;

            // extract number substring efficiently
            int length = end - start - 1;
            int result = -1;
            if (int.TryParse(path.AsSpan(start + 1, length), out int idx))
                result = idx;

            return result;
        }

        // ──────────────────────────────────────────────
        // OnGUI
        // ──────────────────────────────────────────────
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || property.serializedObject == null)
                return;

            var evt = Event.current;
            var so = property.serializedObject;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect rect = new(position.x, position.y, position.width, lineHeight);

            // Header
            int lineIndex = GetIndexFromPath(property.propertyPath);
            string previewText = GetCachedPreviewText(property, lineIndex);
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? s_HeaderColorPro : s_HeaderColorLight);
            s_HeaderContent.text = $"Dialogue Line {lineIndex}: {previewText}";
            EditorGUI.LabelField(rect, s_HeaderContent, s_HeaderStyle);
            rect.y += lineHeight + k_Spacing;

            if (evt.type == EventType.Layout)
                return;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            rect = DrawBasicInfo(rect, property);
            rect = DrawInputMethod(rect, property);
            rect = DrawAudioClip(rect, property);
            rect = DrawLocalizedDialogues(rect, property);
            rect = DrawActionsList(rect, property);
            rect = DrawCharacterRepresentation(rect, property);

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                if (so.targetObject is ConvoCoreConversationData convo)
                {
                    convo.ValidateAndFixDialogueLines();
                    EditorUtility.SetDirty(convo);
                }
            }

            EditorGUI.EndProperty();
        }

        // ──────────────────────────────────────────────
        // Height
        // ──────────────────────────────────────────────
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float spacing = k_Spacing;
            float total = EditorGUIUtility.singleLineHeight + spacing; // Header

            total += GetBasicInfoHeight();
            total += GetInputMethodHeight(property);
            total += GetAudioClipHeight(property);
            total += GetLocalizedDialoguesHeight(property);
            total += GetActionsListHeight(property);
            total += GetCharacterRepresentationSectionHeight(property);

            return total;
        }

        private float GetBasicInfoHeight() =>
            3 * (EditorGUIUtility.singleLineHeight + k_Spacing);

        private float GetInputMethodHeight(SerializedProperty property)
        {
            var methodProp = property.FindPropertyRelative("UserInputMethod");
            var method = (ConvoCoreConversationData.DialogueLineProgressionMethod)methodProp.enumValueIndex;
            float h = EditorGUIUtility.singleLineHeight + k_Spacing;
            if (method == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
                h += EditorGUIUtility.singleLineHeight + k_Spacing;
            return h;
        }

        private float GetAudioClipHeight(SerializedProperty property)
        {
            var clipProp = property.FindPropertyRelative("clip");
            return EditorGUI.GetPropertyHeight(clipProp) + k_Spacing;
        }

        private float GetLocalizedDialoguesHeight(SerializedProperty property)
        {
            var localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            if (localizedDialoguesProp == null || !localizedDialoguesProp.isArray ||
                localizedDialoguesProp.arraySize == 0)
                return EditorGUIUtility.singleLineHeight + k_Spacing;

            float height = EditorGUIUtility.singleLineHeight + k_Spacing; // label

            string lang = ConvoCoreLanguageManager.Instance?.CurrentLanguage ?? "EN";
            SerializedProperty match = null;

            for (int i = 0; i < localizedDialoguesProp.arraySize; i++)
            {
                var el = localizedDialoguesProp.GetArrayElementAtIndex(i);
                var langProp = el.FindPropertyRelative("Language");
                if (langProp != null && string.Equals(langProp.stringValue, lang, StringComparison.OrdinalIgnoreCase))
                {
                    match = el;
                    break;
                }
            }

            match ??= localizedDialoguesProp.arraySize > 0 ? localizedDialoguesProp.GetArrayElementAtIndex(0) : null;

            if (match != null)
            {
                var textProp = match.FindPropertyRelative("Text");
                var text = textProp?.stringValue ?? "";
                float textHeight = s_WrappedLabel.CalcHeight(new GUIContent(text),
                    Mathf.Max(10f, EditorGUIUtility.currentViewWidth - 80f));
                height += textHeight + k_Spacing;
            }
            else
            {
                height += EditorGUIUtility.singleLineHeight + k_Spacing;
            }

            return height;
        }

        private float GetActionsListHeight(SerializedProperty property)
        {
            var beforeProp = property.FindPropertyRelative("ActionsBeforeDialogueLine");
            var afterProp = property.FindPropertyRelative("ActionsAfterDialogueLine");
            float h = 0f;
            float line = EditorGUIUtility.singleLineHeight + k_Spacing;

            // Header foldouts
            h += line;
            if (beforeProp.isExpanded)
            {
                for (int i = 0; i < beforeProp.arraySize; i++)
                    h += EditorGUI.GetPropertyHeight(beforeProp.GetArrayElementAtIndex(i), true) + k_Spacing;
                h += line; // buttons
            }

            h += line;
            if (afterProp.isExpanded)
            {
                for (int i = 0; i < afterProp.arraySize; i++)
                    h += EditorGUI.GetPropertyHeight(afterProp.GetArrayElementAtIndex(i), true) + k_Spacing;
                h += line; // buttons
            }

            return h;
        }



        private float GetCharacterRepresentationSectionHeight(SerializedProperty property)
        {
            float h = 0f;
            h += EditorGUIUtility.singleLineHeight + k_Spacing; // foldout

            string key =
                $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_CharacterRep";
            if (CharacterRepresentationFoldouts.TryGetValue(key, out bool open) && open)
            {
                h += 1 + k_Spacing * 3; // separator

                var convo = property.serializedObject.targetObject as ConvoCoreConversationData;
                if (convo == null)
                {
                    h += EditorGUIUtility.singleLineHeight + k_Spacing;
                }
                else
                {
                    var profiles = convo.ConversationParticipantProfiles?.Where(p => p != null).ToList();
                    if (profiles == null || profiles.Count == 0)
                    {
                        h += EditorGUIUtility.singleLineHeight * 2.5f + k_Spacing;
                    }
                    else
                    {
                        h += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("PrimaryCharacterRepresentation"), property, false);
                        h += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("SecondaryCharacterRepresentation"), property, true);
                        h += GetSingleCharacterRepresentationHeight(
                            property.FindPropertyRelative("TertiaryCharacterRepresentation"), property, true);
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

        private float GetSingleCharacterRepresentationHeight(
            SerializedProperty repProp,
            SerializedProperty mainProperty,
            bool useRepresentationNameInsteadOfID)
        {
            if (repProp == null) return 0f;

            float h = 0f;

            h += EditorGUIUtility.singleLineHeight + k_Spacing; // section label

            var convo = repProp.serializedObject.targetObject as ConvoCoreConversationData;
            if (convo == null) return h + EditorGUIUtility.singleLineHeight + k_Spacing;

            var profiles = convo.ConversationParticipantProfiles?.Where(p => p != null).ToList();
            if (profiles == null || profiles.Count == 0) return h + EditorGUIUtility.singleLineHeight + k_Spacing;

            var selectedRepNameProp = repProp.FindPropertyRelative("SelectedRepresentationName");
            var selectedRepProp = repProp.FindPropertyRelative("SelectedRepresentation");
            var selectedEmotionGuidProp = repProp.FindPropertyRelative("SelectedEmotionId");

            if (useRepresentationNameInsteadOfID)
            {
                var selectedCharacterIDProp = repProp.FindPropertyRelative("SelectedCharacterID");
                // profile popup
                h += EditorGUIUtility.singleLineHeight + k_Spacing;

                string charId = selectedCharacterIDProp?.stringValue ?? "";
                var profile = !string.IsNullOrEmpty(charId)
                    ? profiles.FirstOrDefault(p => p.CharacterID == charId)
                    : null;
                if (profile != null)
                {
                    // representation popup
                    h += EditorGUIUtility.singleLineHeight + k_Spacing;

                    // emotion popup
                    float emoH = (selectedEmotionGuidProp != null)
                        ? EditorGUI.GetPropertyHeight(selectedEmotionGuidProp, true)
                        : EditorGUIUtility.singleLineHeight;
                    h += emoH + k_Spacing;

                    // preview height (if any)
                    IEditorPreviewableRepresentation previewable = null;
                    if (selectedRepProp != null &&
                        selectedRepProp.objectReferenceValue is IEditorPreviewableRepresentation prA)
                        previewable = prA;
                    else
                    {
                        var repName = selectedRepNameProp?.stringValue ?? "";
                        var repObj = profile.GetRepresentation(repName);
                        if (repObj is IEditorPreviewableRepresentation prB) previewable = prB;
                    }

                    h += GetPreviewBlockHeight(previewable) + k_Spacing;

                    // representation-specific options height
                    string emoId = selectedEmotionGuidProp?.stringValue ?? "";
                    var selName = selectedRepNameProp?.stringValue ?? "";
                    var repType = profile.GetRepresentation(selName);
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
                    h += EditorGUIUtility.singleLineHeight + k_Spacing;
                    return h;
                }

                // representation popup
                h += EditorGUIUtility.singleLineHeight + k_Spacing;

                // emotion popup
                float emoH = (selectedEmotionGuidProp != null)
                    ? EditorGUI.GetPropertyHeight(selectedEmotionGuidProp, true)
                    : EditorGUIUtility.singleLineHeight;
                h += emoH + k_Spacing;

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

                h += GetPreviewBlockHeight(previewable) + k_Spacing;

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

        // ──────────────────────────────────────────────
        // Sections (draw)
        // ──────────────────────────────────────────────
        private static Rect DrawBasicInfo(Rect rect, SerializedProperty property)
        {
            DrawLabelValue(ref rect, GC_ConversationID.text,
                property.FindPropertyRelative("ConversationID").stringValue);
            DrawLabelValue(ref rect, GC_LineIndex.text,
                property.FindPropertyRelative("ConversationLineIndex").intValue.ToString());
            DrawLabelValue(ref rect, GC_CharacterID.text, property.FindPropertyRelative("characterID").stringValue);
            return rect;
        }

        private static Rect DrawInputMethod(Rect rect, SerializedProperty property)
        {
            var methodProp = property.FindPropertyRelative("UserInputMethod");
            var timedValueProp = property.FindPropertyRelative("TimeBeforeNextLine");

            var method = (ConvoCoreConversationData.DialogueLineProgressionMethod)methodProp.enumValueIndex;
            method = (ConvoCoreConversationData.DialogueLineProgressionMethod)EditorGUI.EnumPopup(rect, GC_InputMethod,
                method);
            methodProp.enumValueIndex = (int)method;
            rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;

            if (method == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
            {
                EditorGUI.DelayedFloatField(rect, timedValueProp, GC_DisplayDuration);
                rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;
            }

            return rect;
        }

        private static Rect DrawAudioClip(Rect rect, SerializedProperty property)
        {
            var clipProp = property.FindPropertyRelative("clip");
            EditorGUI.ObjectField(rect, clipProp, typeof(AudioClip), GC_AudioClip);
            rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;
            return rect;
        }

        private static Rect DrawLocalizedDialogues(Rect rect, SerializedProperty property)
        {
            var localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            if (localizedDialoguesProp == null || !localizedDialoguesProp.isArray)
            {
                EditorGUI.LabelField(rect, "Localized Dialogues not available.");
                rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;
                return rect;
            }

            string lang = ConvoCoreLanguageManager.Instance?.CurrentLanguage ?? "EN";
            SerializedProperty match = null;

            for (int i = 0; i < localizedDialoguesProp.arraySize; i++)
            {
                var el = localizedDialoguesProp.GetArrayElementAtIndex(i);
                var langProp = el.FindPropertyRelative("Language");
                if (langProp != null && string.Equals(langProp.stringValue, lang, StringComparison.OrdinalIgnoreCase))
                {
                    match = el;
                    break;
                }
            }

            match ??= localizedDialoguesProp.arraySize > 0 ? localizedDialoguesProp.GetArrayElementAtIndex(0) : null;

            if (match != null)
            {
                var textProp = match.FindPropertyRelative("Text");
                string text = textProp?.stringValue ?? "";
                EditorGUI.LabelField(rect, $"Localized Dialogue ({lang}):");
                rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;

                float textHeight = s_WrappedLabel.CalcHeight(new GUIContent(text), rect.width);
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, textHeight), text, s_WrappedLabel);
                rect.y += textHeight + k_Spacing;
            }
            else
            {
                EditorGUI.LabelField(rect, $"No dialogue available for language: {lang}");
                rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;
            }

            return rect;
        }

        private static Rect DrawActionsList(Rect rect, SerializedProperty property)
        {
            var beforeProp = property.FindPropertyRelative("ActionsBeforeDialogueLine");
            var afterProp = property.FindPropertyRelative("ActionsAfterDialogueLine");
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // ---- Actions Before Line ----
            beforeProp.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                beforeProp.isExpanded,
                GC_BeforeActions,
                true
            );
            rect.y += lineHeight + k_Spacing;

            if (beforeProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < beforeProp.arraySize; i++)
                {
                    var el = beforeProp.GetArrayElementAtIndex(i);
                    float h = EditorGUI.GetPropertyHeight(el, true);
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width, h),
                        el,
                        new GUIContent($"Element {i}"),
                        true
                    );
                    rect.y += h + k_Spacing;
                }

                // Add/Remove buttons
                Rect buttons = new(rect.x + 14, rect.y, rect.width - 14, lineHeight);
                if (GUI.Button(new Rect(buttons.x, buttons.y, 20, lineHeight), "+"))
                    beforeProp.InsertArrayElementAtIndex(beforeProp.arraySize);
                if (GUI.Button(new Rect(buttons.x + 25, buttons.y, 20, lineHeight), "-") && beforeProp.arraySize > 0)
                    beforeProp.DeleteArrayElementAtIndex(beforeProp.arraySize - 1);

                rect.y += lineHeight + k_Spacing;
                EditorGUI.indentLevel--;
            }

            // ---- Actions After Line ----
            afterProp.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, rect.width, lineHeight),
                afterProp.isExpanded,
                GC_AfterActions,
                true
            );
            rect.y += lineHeight + k_Spacing;

            if (afterProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < afterProp.arraySize; i++)
                {
                    var el = afterProp.GetArrayElementAtIndex(i);
                    float h = EditorGUI.GetPropertyHeight(el, true);
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width, h),
                        el,
                        new GUIContent($"Element {i}"),
                        true
                    );
                    rect.y += h + k_Spacing;
                }

                Rect buttons = new(rect.x + 14, rect.y, rect.width - 14, lineHeight);
                if (GUI.Button(new Rect(buttons.x, buttons.y, 20, lineHeight), "+"))
                    afterProp.InsertArrayElementAtIndex(afterProp.arraySize);
                if (GUI.Button(new Rect(buttons.x + 25, buttons.y, 20, lineHeight), "-") && afterProp.arraySize > 0)
                    afterProp.DeleteArrayElementAtIndex(afterProp.arraySize - 1);

                rect.y += lineHeight + k_Spacing;
                EditorGUI.indentLevel--;
            }

            return rect;
        }

        private Rect DrawCharacterRepresentation(Rect rect, SerializedProperty property)
        {
            string foldoutKey =
                $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_CharacterRep";
            if (!CharacterRepresentationFoldouts.ContainsKey(foldoutKey))
                CharacterRepresentationFoldouts[foldoutKey] = true;

            bool isExpanded = CharacterRepresentationFoldouts[foldoutKey];
            bool newExpanded = EditorGUI.Foldout(rect, isExpanded, "Character Representations", true, s_FoldoutBold);
            if (newExpanded != isExpanded)
                CharacterRepresentationFoldouts[foldoutKey] = newExpanded;

            rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;

            // skip heavy UI when collapsed
            if (!newExpanded)
                return rect;

            rect.y += k_Spacing;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0.5f, 0.5f, 0.5f, 1));
            rect.y += 1 + k_Spacing * 2;

            var convo = property.serializedObject.targetObject as ConvoCoreConversationData;
            if (convo == null)
            {
                EditorGUI.indentLevel++;
                EditorGUI.LabelField(rect, "Error: Conversation data is missing.");
                rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;
                EditorGUI.indentLevel--;
                return rect;
            }

            var validProfiles = convo.ConversationParticipantProfiles.Where(p => p != null).ToList();
            if (validProfiles.Count == 0)
            {
                EditorGUI.indentLevel++;
                Rect helpRect = new(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight * 2.5f);
                EditorGUI.LabelField(helpRect,
                    "No conversation participants are configured. Please add character profiles to the ConversationParticipantProfiles list.",
                    s_HelpBox);
                rect.y += EditorGUIUtility.singleLineHeight * 2.5f + k_Spacing;
                EditorGUI.indentLevel--;
                return rect;
            }

            EditorGUI.indentLevel++;

            rect = DrawSingleCharacterRepresentation(
                rect,
                property.FindPropertyRelative("PrimaryCharacterRepresentation"),
                "Primary Character",
                property.FindPropertyRelative("characterID"),
                k_Spacing,
                useRepresentationNameInsteadOfID: false,
                convo);

            rect = DrawSingleCharacterRepresentation(
                rect,
                property.FindPropertyRelative("SecondaryCharacterRepresentation"),
                "Secondary Character",
                property.FindPropertyRelative("SecondaryCharacterRepresentation"),
                k_Spacing,
                useRepresentationNameInsteadOfID: true,
                convo);

            rect = DrawSingleCharacterRepresentation(
                rect,
                property.FindPropertyRelative("TertiaryCharacterRepresentation"),
                "Tertiary Character",
                property.FindPropertyRelative("TertiaryCharacterRepresentation"),
                k_Spacing,
                useRepresentationNameInsteadOfID: true,
                convo);

            EditorGUI.indentLevel--;
            rect.y += 5f;
            return rect;
        }
    
        private static readonly Dictionary<int, GUIContent[]> _profilePopupCache = new();

// helper cache builder
        private static GUIContent[] GetCachedProfileNames(ConvoCoreConversationData convo, List<ConvoCoreCharacterProfileBaseData> profiles)
        {
            int id = convo.GetInstanceID();
            if (_profilePopupCache.TryGetValue(id, out var arr))
                return arr;

            var list = new List<GUIContent>(profiles.Count + 1) { new GUIContent("None") };
            foreach (var p in profiles)
            {
                if (p != null && !string.IsNullOrEmpty(p.CharacterName))
                    list.Add(new GUIContent(p.CharacterName));
            }

            arr = list.ToArray();
            _profilePopupCache[id] = arr;
            return arr;
        }
    // ──────────────────────────────────────────────
        // Your current DrawSingleCharacterRepresentation (unchanged)
        // ──────────────────────────────────────────────
        private Rect DrawSingleCharacterRepresentation(
    Rect rect,
    SerializedProperty representationProp,
    string label,
    SerializedProperty identifierProp,
    float spacing,
    bool useRepresentationNameInsteadOfID,
    ConvoCoreConversationData convo)
{
    var so = representationProp.serializedObject;

    var selectedCharacterIDProp = representationProp.FindPropertyRelative("SelectedCharacterID");
    var selectedRepNameProp = representationProp.FindPropertyRelative("SelectedRepresentationName");
    var selectedRepProp = representationProp.FindPropertyRelative("SelectedRepresentation");
    var selectedEmotionGuidProp = representationProp.FindPropertyRelative("SelectedEmotionId");

    EditorGUI.LabelField(rect, $"{label}:");
    rect.y += EditorGUIUtility.singleLineHeight + spacing;

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
        // secondary / tertiary
        string currentCharacterID = selectedCharacterIDProp.stringValue;
        var currentProfile = !string.IsNullOrEmpty(currentCharacterID)
            ? validProfiles.FirstOrDefault(p => p.CharacterID == currentCharacterID)
            : null;

        // cached popup entries
        var popupNames = GetCachedProfileNames(convo, validProfiles);
        int currentIndex = 0;
        if (currentProfile != null)
        {
            for (int i = 1; i < popupNames.Length; i++)
                if (popupNames[i].text == currentProfile.CharacterName)
                { currentIndex = i; break; }
        }

        int newIndex = EditorGUI.Popup(rect, new GUIContent($"{label} Profile:"), currentIndex, popupNames);
        rect.y += EditorGUIUtility.singleLineHeight + spacing;

        if (newIndex != currentIndex)
        {
            if (newIndex == 0)
            {
                selectedCharacterIDProp.stringValue = "";
                selectedRepNameProp.stringValue = "";
                selectedEmotionGuidProp.stringValue = "";
                if (selectedRepProp != null) selectedRepProp.objectReferenceValue = null;
                so.ApplyModifiedProperties();
                return rect;
            }
            else
            {
                var selProfile = validProfiles[newIndex - 1];
                selectedCharacterIDProp.stringValue = selProfile.CharacterID;

                var firstRep = selProfile.Representations?.FirstOrDefault(r => r != null);
                selectedRepNameProp.stringValue = firstRep?.CharacterRepresentationName ?? "";
                selectedEmotionGuidProp.stringValue = "";
                if (selectedRepProp != null)
                    selectedRepProp.objectReferenceValue = firstRep?.CharacterRepresentationType;
                so.ApplyModifiedProperties();
                currentProfile = selProfile;
            }
        }

        if (currentProfile == null)
            return rect;

        // representation popup
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
                selectedRepNameProp.stringValue = newRepName;
                selectedEmotionGuidProp.stringValue = "";
                if (selectedRepProp != null)
                    selectedRepProp.objectReferenceValue = currentProfile.GetRepresentation(newRepName);
                so.ApplyModifiedProperties();
            }

            selectedRepresentation = currentProfile.GetRepresentation(newRepName);

            // emotion
            if (selectedEmotionGuidProp != null)
            {
                float emoH = EditorGUI.GetPropertyHeight(selectedEmotionGuidProp, true);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, emoH),
                    selectedEmotionGuidProp, GC_Emotion, true);
                rect.y += emoH + spacing;
            }

            // inline preview
            IEditorPreviewableRepresentation previewable = null;
            if (selectedRepProp?.objectReferenceValue is IEditorPreviewableRepresentation prA)
                previewable = prA;
            else if (selectedRepresentation is IEditorPreviewableRepresentation prB)
                previewable = prB;

            if (previewable != null)
                rect = DrawInlinePreviewBlock(rect, previewable, selectedEmotionGuidProp?.stringValue ?? "", spacing);
        }
    }
    else
    {
        // primary
        string characterID = identifierProp.stringValue;
        var profile = validProfiles.FirstOrDefault(p => p.CharacterID == characterID);
        if (profile == null)
        {
            var names = validProfiles.Where(p => !string.IsNullOrEmpty(p.CharacterName))
                .Select(p => p.CharacterName).ToArray();
            int idx = EditorGUI.Popup(rect, $"{label} Participant:", 0, names);
            rect.y += EditorGUIUtility.singleLineHeight + spacing;
            if (names.Length > 0)
            {
                var chosen = validProfiles[idx];
                identifierProp.stringValue = chosen.CharacterID;
                so.ApplyModifiedProperties();
            }
            return rect;
        }

        var repNames = profile.Representations
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
                selectedRepNameProp.stringValue = newRepName;
                if (selectedRepProp != null)
                    selectedRepProp.objectReferenceValue = profile.GetRepresentation(newRepName);
                selectedEmotionGuidProp.stringValue = "";
                so.ApplyModifiedProperties();
            }

            selectedRepresentation = profile.GetRepresentation(newRepName);

            if (selectedEmotionGuidProp != null)
            {
                float emoH = EditorGUI.GetPropertyHeight(selectedEmotionGuidProp, true);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, emoH),
                    selectedEmotionGuidProp, GC_Emotion, true);
                rect.y += emoH + spacing;
            }

            IEditorPreviewableRepresentation previewable = null;
            if (selectedRepProp?.objectReferenceValue is IEditorPreviewableRepresentation prA)
                previewable = prA;
            else if (selectedRepresentation is IEditorPreviewableRepresentation prB)
                previewable = prB;

            if (previewable != null)
                rect = DrawInlinePreviewBlock(rect, previewable, selectedEmotionGuidProp?.stringValue ?? "", spacing);
        }
    }

    if (selectedRepresentation != null && selectedEmotionGuidProp != null &&
        !string.IsNullOrEmpty(selectedEmotionGuidProp.stringValue))
    {
        rect = DrawRepresentationSpecificOptions(rect, representationProp, selectedRepresentation,
            selectedEmotionGuidProp.stringValue, spacing);
    }

    return rect;
}

        // ──────────────────────────────────────────────
        // Representation-specific options (draw/height)
        // ──────────────────────────────────────────────
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

        // ──────────────────────────────────────────────
        // Inline preview helpers
        // ──────────────────────────────────────────────
        private static float GetPreviewBlockHeight(IEditorPreviewableRepresentation previewable)
        {
            if (previewable == null) return 0f;
            float desired = previewable.GetPreviewHeight();
            if (desired <= 0f) return 0f;
            return Mathf.Clamp(desired, k_MinPreviewHeight, k_MaxPreviewHeight);
        }

        private static Rect DrawInlinePreviewBlock(Rect rect, IEditorPreviewableRepresentation previewable,
            string emotionGuid, float spacing)
        {
            float h = GetPreviewBlockHeight(previewable);
            if (h <= 0f) return rect;

            var indented = EditorGUI.IndentedRect(new Rect(rect.x, rect.y, rect.width, h));
            var outer = new Rect(indented.x, indented.y, indented.width, h);
            var inner = new Rect(outer.x + k_Pad, outer.y + k_Pad, outer.width - k_Pad * 2f, outer.height - k_Pad * 2f);

            EditorGUI.DrawRect(outer, new Color(0f, 0f, 0f, 0.06f));

            object emotionMapping = null;
            if (!string.IsNullOrEmpty(emotionGuid) && previewable is CharacterRepresentationBase repBase)
                emotionMapping = repBase.GetEmotionMappingByGuid(emotionGuid);

            previewable.DrawInlineEditorPreview(emotionMapping, inner);

            rect.y += h + spacing;
            return rect;
        }

        // ──────────────────────────────────────────────
        // Preview text cache
        // ──────────────────────────────────────────────
        private string GetCachedPreviewText(SerializedProperty property, int lineIndex)
        {
            int id = HashCacheKey(property);
            double now = EditorApplication.timeSinceStartup;

            if (s_PreviewCache.TryGetValue(id, out string cached))
                return cached;

            string preview = GetPreviewText(property);
            s_PreviewCache[id] = preview;

            if (now - s_LastCachePurgeTime > 5.0)
            {
                if (s_PreviewCache.Count > 256)
                    s_PreviewCache.Clear();
                s_LastCachePurgeTime = now;
            }
            return preview;
        }

        private string GetPreviewText(SerializedProperty property)
        {
            var localizedDialoguesProp = property.FindPropertyRelative("LocalizedDialogues");
            if (localizedDialoguesProp == null || !localizedDialoguesProp.isArray || localizedDialoguesProp.arraySize == 0)
                return "(No dialogue text)";

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
                    return text.Length > 60 ? text[..60] + "..." : text;
                }
            }

            var firstText = localizedDialoguesProp.GetArrayElementAtIndex(0)?.FindPropertyRelative("Text");
            string fallback = firstText?.stringValue ?? "";
            return fallback.Length > 60 ? fallback[..60] + "..." : fallback;
        }

        private static int HashCacheKey(SerializedProperty property)
        {
            unchecked
            {
                int id = property.serializedObject.targetObject.GetInstanceID();
                int pathHash = property.propertyPath != null ? property.propertyPath.GetHashCode() : 0;
                return (id * 397) ^ pathHash;
            }
        }
        

        // ──────────────────────────────────────────────
        // Small label helper
        // ──────────────────────────────────────────────
        private static void DrawLabelValue(ref Rect rect, string label, string value)
        {
            EditorGUI.LabelField(rect, label, value);
            rect.y += EditorGUIUtility.singleLineHeight + k_Spacing;
        }
    }
}