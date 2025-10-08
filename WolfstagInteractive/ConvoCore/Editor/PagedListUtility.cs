#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace WolfstagInteractive.ConvoCore.Editor
{
    public static class PagedListUtility
    {
        private const string PREF_KEY_PREFIX = "PagedListUtility_";
        private const string PAGE_SIZE_KEY_SUFFIX = "_PageSize";

        private static readonly Dictionary<string, bool> FoldoutStates = new();
        private static readonly List<SerializedProperty> TempVisibleProps = new();
        private static readonly GUIContent TempLabel = new();

        public static void DrawPagedList(SerializedProperty listProp, int defaultPageSize = 20)
        {
            if (listProp == null || !listProp.isArray)
            {
                EditorGUILayout.PropertyField(listProp, true);
                return;
            }

            string uniqueKey = PREF_KEY_PREFIX +
                               listProp.serializedObject.targetObject.GetInstanceID() + "_" +
                               listProp.propertyPath;

            int pageSize = Mathf.Max(1, EditorPrefs.GetInt(uniqueKey + PAGE_SIZE_KEY_SUFFIX, defaultPageSize));
            int total = listProp.arraySize;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(total / (float)pageSize));
            int currentPage = Mathf.Clamp(SessionState.GetInt(uniqueKey, 0), 0, totalPages - 1);

            listProp.isExpanded = EditorGUILayout.Foldout(
                listProp.isExpanded,
                $"{ObjectNames.NicifyVariableName(listProp.displayName)} ({total})",
                true);
            if (!listProp.isExpanded)
                return;

            EditorGUI.indentLevel++;

            // ---------- Toolbar ----------
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("First", EditorStyles.toolbarButton, GUILayout.Width(45))) currentPage = 0;
            if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25))) currentPage = Mathf.Max(currentPage - 1, 0);
            GUILayout.Label($"{currentPage + 1}/{totalPages}", GUILayout.Width(60));
            if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25))) currentPage = Mathf.Min(currentPage + 1, totalPages - 1);
            if (GUILayout.Button("Last", EditorStyles.toolbarButton, GUILayout.Width(45))) currentPage = totalPages - 1;

            GUILayout.Space(10);
            int start = Mathf.Clamp(currentPage * pageSize, 0, Mathf.Max(0, total - 1));
            int end = Mathf.Min(start + pageSize, total);
            GUILayout.Label($"Showing {start + 1}–{end} of {total}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            GUILayout.Label("Items per page", EditorStyles.miniLabel, GUILayout.Width(90));
            Rect sizeRect = GUILayoutUtility.GetRect(45, EditorGUIUtility.singleLineHeight);
            int newPageSize = EditorGUI.DelayedIntField(sizeRect, pageSize);
            EditorGUIUtility.AddCursorRect(sizeRect, MouseCursor.SlideArrow);
            if (Event.current.type == EventType.MouseDrag && sizeRect.Contains(Event.current.mousePosition))
            {
                newPageSize = Mathf.Clamp(pageSize + Mathf.RoundToInt(Event.current.delta.x), 1, 200);
                Event.current.Use();
            }

            if (newPageSize != pageSize && newPageSize > 0)
            {
                pageSize = newPageSize;
                EditorPrefs.SetInt(uniqueKey + PAGE_SIZE_KEY_SUFFIX, pageSize);
                totalPages = Mathf.Max(1, Mathf.CeilToInt(total / (float)pageSize));
                currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
            }
            EditorGUILayout.EndHorizontal();

            SessionState.SetInt(uniqueKey, currentPage);
            GUILayout.Space(3);

            // ---------- Cache visible range ----------
            TempVisibleProps.Clear();
            for (int i = start; i < end; i++)
            {
                if (i >= listProp.arraySize) break;
                TempVisibleProps.Add(listProp.GetArrayElementAtIndex(i));
            }

            // ---------- Body ----------
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var element in TempVisibleProps)
            {
                string id = element.propertyPath;
                if (!FoldoutStates.ContainsKey(id))
                    FoldoutStates[id] = false;

                // Get preview text from LocalizedDialogues
                string preview = TryGetPreviewText(element);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                FoldoutStates[id] = EditorGUILayout.Foldout(FoldoutStates[id], $"Dialogue Line {GetIndex(element)}", true);
                GUILayout.FlexibleSpace();
                GUILayout.Label(preview, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                EditorGUILayout.EndHorizontal();

                if (FoldoutStates[id])
                {
                    // Only draw full property when expanded
                    EditorGUILayout.PropertyField(element, GUIContent.none, true);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }
            EditorGUILayout.EndVertical();

            // ---------- Footer ----------
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Page {currentPage + 1} of {totalPages}  •  Total: {total} lines", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        // Helper: extract preview text for collapsed lines
        private static string TryGetPreviewText(SerializedProperty prop)
        {
            var locProp = prop.FindPropertyRelative("LocalizedDialogues");
            if (locProp == null || !locProp.isArray || locProp.arraySize == 0)
                return "(no text)";

            string lang = ConvoCoreLanguageManager.Instance?.CurrentLanguage ?? "EN";
            for (int i = 0; i < locProp.arraySize; i++)
            {
                var el = locProp.GetArrayElementAtIndex(i);
                var langProp = el.FindPropertyRelative("Language");
                var textProp = el.FindPropertyRelative("Text");
                if (langProp != null && textProp != null &&
                    string.Equals(langProp.stringValue, lang, System.StringComparison.OrdinalIgnoreCase))
                {
                    var text = textProp.stringValue ?? "";
                    if (text.Length > 60) text = text.Substring(0, 60) + "...";
                    return text;
                }
            }
            // fallback to first
            var first = locProp.GetArrayElementAtIndex(0);
            var firstText = first.FindPropertyRelative("Text");
            if (firstText == null) return "(no text)";
            var preview = firstText.stringValue;
            if (preview.Length > 60) preview = preview.Substring(0, 60) + "...";
            return preview;
        }

        private static string GetIndex(SerializedProperty prop)
        {
            string path = prop.propertyPath;
            int start = path.LastIndexOf('[') + 1;
            int end = path.LastIndexOf(']');
            return start >= 0 && end > start ? path[start..end] : "?";
        }
    }
}
#endif