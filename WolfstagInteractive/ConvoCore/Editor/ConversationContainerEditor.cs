#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [CustomEditor(typeof(ConversationContainer))]
    public sealed class ConversationContainerEditor : UnityEditor.Editor
    {
        private SerializedProperty _containerModeProp;
        private SerializedProperty _selectionModeProp;

        private SerializedProperty _conversationsProp;

        private SerializedProperty _loopProp;
        private SerializedProperty _defaultStartProp;

        private ReorderableList _list;

        private static readonly float Spacing = 4f;

        private void OnEnable()
        {
            _containerModeProp = serializedObject.FindProperty("ContainerMode");
            _selectionModeProp = serializedObject.FindProperty("SelectionMode");

            _conversationsProp = serializedObject.FindProperty("Conversations");

            _loopProp = serializedObject.FindProperty("Loop");
            _defaultStartProp = serializedObject.FindProperty("DefaultStart");

            BuildList();
        }
        private int _containerModeLabelFontSize = 20;

        private GUIStyle _containerModeLabelStyle;

        private void EnsureStyles()
        {
            if (_containerModeLabelStyle == null || _containerModeLabelStyle.fontSize != _containerModeLabelFontSize)
            {
                _containerModeLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = _containerModeLabelFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
            }
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTopSection();

            EditorGUILayout.Space(8);

            DrawModeSpecificSection();

            EditorGUILayout.Space(8);

            DrawListSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTopSection()
        {
            EditorGUILayout.LabelField("Conversation Container", EditorStyles.boldLabel);
            EnsureStyles();

            EditorGUILayout.BeginVertical("box");

            var totalRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 1.5f);

            var labelWidth = EditorGUIUtility.labelWidth;

            var labelRect = new Rect(
                totalRect.x,
                totalRect.y,
                labelWidth,
                totalRect.height
            );

            var fieldRect = new Rect(
                totalRect.x + labelWidth,
                totalRect.y,
                totalRect.width - labelWidth,
                totalRect.height
            );

            EditorGUI.LabelField(labelRect, "Container Mode:", _containerModeLabelStyle);

            var current = (ConversationContainerMode)_containerModeProp.enumValueIndex;

            var popupStyle = new GUIStyle(EditorStyles.popup)
            {
                fixedHeight = totalRect.height,
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft
            };

            var next = (ConversationContainerMode)EditorGUI.EnumPopup(fieldRect, current, popupStyle);

            if (next != current)
                _containerModeProp.enumValueIndex = (int)next;

            EditorGUILayout.EndVertical();

            GUILayout.Space(6f);
            DrawSeparator();
            GUILayout.Space(6f);
        }



        private static void DrawSeparator()
        {
            var r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.25f));
        }


        private void DrawModeSpecificSection()
        {
            var mode = (ConversationContainerMode)_containerModeProp.enumValueIndex;

            EditorGUILayout.BeginVertical("box");

            if (mode == ConversationContainerMode.Playlist)
            {
                EditorGUILayout.LabelField("Playlist Settings:", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_loopProp);
                EditorGUILayout.PropertyField(_defaultStartProp, new GUIContent("Default Start (Alias or Name)"));
            }
            else
            {
                EditorGUILayout.LabelField("Selector Settings:", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox(
                    "Selector mode chooses a single conversation when a dialogue line branches into this container.",
                    MessageType.Info
                );

                // Put SelectionMode inside the same boxed area
                EditorGUILayout.Space(6f);
                EditorGUILayout.PropertyField(_selectionModeProp, new GUIContent("Selection Mode:"));
            }

            EditorGUILayout.EndVertical();
        }


        private void DrawListSection()
        {
            if (_list == null)
                BuildList();

            _list.DoLayoutList();

            DrawValidation();
        }

        private void BuildList()
        {
            _list = new ReorderableList(serializedObject, _conversationsProp, true, true, true, true);

            _list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Entries");
            };

            _list.elementHeightCallback = index =>
            {
                var element = _conversationsProp.GetArrayElementAtIndex(index);
                if (element == null)
                    return EditorGUIUtility.singleLineHeight;

                var mode = (ConversationContainerMode)_containerModeProp.enumValueIndex;
                var sel = (ConversationSelectionMode)_selectionModeProp.enumValueIndex;

                float h = 0f;
                float space = Spacing;

                var aliasProp = element.FindPropertyRelative("Alias");
                var convoProp = element.FindPropertyRelative("ConversationData");
                var enabledProp = element.FindPropertyRelative("Enabled");
                var delayProp = element.FindPropertyRelative("DelayAfterEndSeconds");
                var startIndexProp = element.FindPropertyRelative("StartLineIndex");
                var weightProp = element.FindPropertyRelative("Weight");
                var tagsProp = element.FindPropertyRelative("Tags");

                h += EditorGUI.GetPropertyHeight(aliasProp, true) + space;
                h += EditorGUI.GetPropertyHeight(convoProp, true) + space;
                h += EditorGUI.GetPropertyHeight(enabledProp, true) + space;

                if (mode == ConversationContainerMode.Playlist)
                {
                    h += EditorGUI.GetPropertyHeight(delayProp, true) + space;
                }
                else
                {
                    h += EditorGUI.GetPropertyHeight(startIndexProp, true) + space;

                    if (sel == ConversationSelectionMode.WeightedRandom)
                        h += EditorGUI.GetPropertyHeight(weightProp, true) + space;
                }

                if (tagsProp != null)
                    h += EditorGUI.GetPropertyHeight(tagsProp, true) + space;

                h += 2f;
                return h;
            };


            _list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = _conversationsProp.GetArrayElementAtIndex(index);
                if (element == null)
                    return;

                var mode = (ConversationContainerMode)_containerModeProp.enumValueIndex;
                var sel = (ConversationSelectionMode)_selectionModeProp.enumValueIndex;

                var aliasProp = element.FindPropertyRelative("Alias");
                var convoProp = element.FindPropertyRelative("ConversationData");
                var enabledProp = element.FindPropertyRelative("Enabled");
                var delayProp = element.FindPropertyRelative("DelayAfterEndSeconds");
                var startIndexProp = element.FindPropertyRelative("StartLineIndex");
                var weightProp = element.FindPropertyRelative("Weight");
                var tagsProp = element.FindPropertyRelative("Tags");

                rect.height = EditorGUIUtility.singleLineHeight;

                DrawProperty(ref rect, aliasProp, "Alias");
                DrawProperty(ref rect, convoProp, "Conversation");
                DrawProperty(ref rect, enabledProp, "Enabled");

                if (mode == ConversationContainerMode.Playlist)
                {
                    DrawProperty(ref rect, delayProp, "Delay After End (sec)");
                }
                else
                {
                    DrawProperty(ref rect, startIndexProp, "Start Line Index");

                    if (sel == ConversationSelectionMode.WeightedRandom)
                        DrawProperty(ref rect, weightProp, "Weight");
                }

                DrawProperty(ref rect, tagsProp, "Tags");


            };
        }

        
        private static void DrawProperty(ref Rect rect, SerializedProperty prop, string label)
        {
            if (prop == null)
                return;

            float h = EditorGUI.GetPropertyHeight(prop, true);
            rect.height = h;
            EditorGUI.PropertyField(rect, prop, new GUIContent(label), true);
            rect.y += h + Spacing;
            rect.height = EditorGUIUtility.singleLineHeight;
        }

       

        private void DrawValidation()
        {
            if (_conversationsProp == null || _conversationsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No entries. This container will do nothing.", MessageType.Warning);
                return;
            }

            bool anyValidEnabled = false;
            for (int i = 0; i < _conversationsProp.arraySize; i++)
            {
                var e = _conversationsProp.GetArrayElementAtIndex(i);
                if (e == null) continue;

                var enabledProp = e.FindPropertyRelative("Enabled");
                var convoProp = e.FindPropertyRelative("ConversationData");

                if (enabledProp != null &&
                    enabledProp.boolValue &&
                    convoProp != null &&
                    convoProp.objectReferenceValue != null)
                {
                    anyValidEnabled = true;
                    break;
                }
            }

            if (!anyValidEnabled)
            {
                EditorGUILayout.HelpBox("No enabled entries with a valid Conversation assigned.", MessageType.Warning);
            }

            var mode = (ConversationContainerMode)_containerModeProp.enumValueIndex;
            if (mode == ConversationContainerMode.Selector)
            {
                var sel = (ConversationSelectionMode)_selectionModeProp.enumValueIndex;
                if (sel == ConversationSelectionMode.WeightedRandom)
                {
                    bool anyPositiveWeight = false;
                    for (int i = 0; i < _conversationsProp.arraySize; i++)
                    {
                        var e = _conversationsProp.GetArrayElementAtIndex(i);
                        if (e == null) continue;

                        var enabledProp = e.FindPropertyRelative("Enabled");
                        var weightProp = e.FindPropertyRelative("Weight");

                        if (enabledProp != null && enabledProp.boolValue && weightProp != null && weightProp.floatValue > 0f)
                        {
                            anyPositiveWeight = true;
                            break;
                        }
                    }

                    if (!anyPositiveWeight)
                    {
                        EditorGUILayout.HelpBox("WeightedRandom is active but no enabled entries have Weight > 0. It will behave like First.", MessageType.Info);
                    }
                }
            }
        }
    }
}
#endif