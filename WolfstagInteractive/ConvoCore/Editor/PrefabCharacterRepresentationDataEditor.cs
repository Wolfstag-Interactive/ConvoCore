#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1PrefabCharacterRepresentationDataEditor.html")]
    [CustomEditor(typeof(PrefabCharacterRepresentationData))]
    public class PrefabCharacterRepresentationDataEditor : UnityEditor.Editor
    {
        private ReorderableList _sharedList;
        private ReorderableList _entriesList;

        private void OnEnable()
        {
            BuildSharedList();
            BuildEntriesList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Safety rebuild if lists were cleared by domain reload
            if (_sharedList == null) BuildSharedList();
            if (_entriesList == null) BuildEntriesList();

            // ── Configuration Entries ──────────────────────────────────────
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Configuration Entries", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Each entry defines a prefab (spawned when the character is not in the scene registry), " +
                "a presence that controls 3D placement, and optional expression overrides.",
                MessageType.None);

            _entriesList.DoLayoutList();

            EditorGUILayout.Space(8f);

            // ── Shared Expression Pool ─────────────────────────────────────
            EditorGUILayout.LabelField("Shared Expression Pool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Expressions available to all entries. An entry's own ExpressionOverrides take priority.",
                MessageType.None);

            var sharedProp = serializedObject.FindProperty("SharedExpressionMappings");
            EnsureGuids(sharedProp);

            var sharedDuplicates = GetDuplicateDisplayNames(sharedProp);
            if (sharedDuplicates.Count > 0)
                EditorGUILayout.HelpBox(
                    $"Duplicate shared expression names: {string.Join(", ", sharedDuplicates)}",
                    MessageType.Warning);

            _sharedList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        // ------------------------------------------------------------------
        // Shared expression pool list
        // ------------------------------------------------------------------

        private void BuildSharedList()
        {
            var listProp = serializedObject.FindProperty("SharedExpressionMappings");
            if (listProp == null) return;

            _sharedList = new ReorderableList(serializedObject, listProp, true, true, true, true);
            _sharedList.drawHeaderCallback  = rect => EditorGUI.LabelField(rect, "Shared Expressions");
            _sharedList.elementHeightCallback = index => ExpressionMappingHeight(listProp, index);
            _sharedList.drawElementCallback   = (rect, index, active, focused) =>
                DrawExpressionMappingElement(rect, listProp, index, null);
        }

        // ------------------------------------------------------------------
        // Configuration entries list
        // ------------------------------------------------------------------

        private void BuildEntriesList()
        {
            var entriesProp = serializedObject.FindProperty("ConfigurationEntries");
            if (entriesProp == null) return;

            _entriesList = new ReorderableList(serializedObject, entriesProp, true, true, true, true);
            _entriesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Configuration Entries");

            _entriesList.elementHeightCallback = index =>
            {
                if (!entriesProp.isArray || index >= entriesProp.arraySize) return EditorGUIUtility.singleLineHeight;

                var el          = entriesProp.GetArrayElementAtIndex(index);
                float lineH     = EditorGUIUtility.singleLineHeight;
                float pad       = 8f;
                float spacing   = 3f;
                // EntryName + CharacterPrefab + Presence = 3 rows
                float rows      = 3f * (lineH + spacing);
                // Preview
                float preview   = 68f;
                // ExpressionOverrides
                var overridesProp = el.FindPropertyRelative("ExpressionOverrides");
                float overridesH = overridesProp != null
                    ? EditorGUI.GetPropertyHeight(overridesProp, true) + spacing
                    : 0f;

                return pad + rows + preview + spacing + overridesH + pad;
            };

            _entriesList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (!entriesProp.isArray || index >= entriesProp.arraySize) return;

                var el          = entriesProp.GetArrayElementAtIndex(index);
                float lineH     = EditorGUIUtility.singleLineHeight;
                float pad       = 8f;
                float spacing   = 3f;

                rect = new Rect(rect.x + 4f, rect.y + pad, rect.width - 8f, rect.height - pad * 2f);

                var nameProp      = el.FindPropertyRelative("EntryName");
                var prefabProp    = el.FindPropertyRelative("CharacterPrefab");
                var presenceProp  = el.FindPropertyRelative("Presence");
                var overridesProp = el.FindPropertyRelative("ExpressionOverrides");

                float y = rect.y;

                // Row: EntryName
                EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineH), nameProp, new GUIContent("Entry Name"));
                y += lineH + spacing;

                // Row: CharacterPrefab
                EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineH), prefabProp);
                y += lineH + spacing;

                // Row: Presence
                EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineH), presenceProp);
                y += lineH + spacing;

                // Prefab preview
                var previewRect = new Rect(rect.x, y, rect.width, 68f);
                DrawPrefabPreview(previewRect, prefabProp);
                y += 68f + spacing;

                // ExpressionOverrides
                if (overridesProp != null)
                {
                    EnsureGuids(overridesProp);
                    float overridesH = EditorGUI.GetPropertyHeight(overridesProp, true);
                    EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, overridesH),
                        overridesProp, new GUIContent("Expression Overrides"), true);
                }
            };
        }

        // ------------------------------------------------------------------
        // Shared expression element helpers (reused for both lists)
        // ------------------------------------------------------------------

        private static float ExpressionMappingHeight(SerializedProperty listProp, int index)
        {
            const float pad     = 6f;
            const float preview = 68f;
            float header        = EditorGUIUtility.singleLineHeight;
            float height        = pad + header + 6f + preview + pad;

            if (listProp.isArray && index >= 0 && index < listProp.arraySize)
            {
                var actionsProp = listProp.GetArrayElementAtIndex(index).FindPropertyRelative("ExpressionActions");
                if (actionsProp != null)
                    height += 4f + EditorGUI.GetPropertyHeight(actionsProp, true);
            }

            return height;
        }

        private static void DrawExpressionMappingElement(Rect rect, SerializedProperty listProp,
            int index, SerializedProperty prefabPropOverride)
        {
            var el         = listProp.GetArrayElementAtIndex(index);
            var nameProp   = el.FindPropertyRelative("DisplayName") ?? el.FindPropertyRelative("Name");
            var guidProp   = el.FindPropertyRelative("expressionID");
            var actionsProp = el.FindPropertyRelative("ExpressionActions");

            bool isDuplicate = !string.IsNullOrWhiteSpace(nameProp?.stringValue) &&
                               CountName(listProp, index, nameProp.stringValue) > 0;

            const float pad = 6f;
            rect = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f);

            var headerRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            float warnW  = isDuplicate ? 20f : 0f;
            float nameW  = (headerRect.width - warnW) * 0.55f;

            if (isDuplicate)
            {
                var warnRect = new Rect(headerRect.x, headerRect.y, warnW, headerRect.height);
                EditorGUI.LabelField(warnRect, new GUIContent(
                    EditorGUIUtility.IconContent("console.warnicon.sml")) { tooltip = "Duplicate name." });
            }

            var nameRect  = new Rect(headerRect.x + warnW, headerRect.y, nameW, headerRect.height);
            var guidLabel = new Rect(nameRect.xMax + 8f, headerRect.y, 70f, headerRect.height);
            var guidRect  = new Rect(guidLabel.xMax + 4f, headerRect.y,
                headerRect.xMax - (guidLabel.xMax + 4f), headerRect.height);

            EditorGUI.BeginProperty(nameRect, GUIContent.none, nameProp);
            if (isDuplicate)
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.8f, 0.6f);
                nameProp.stringValue = EditorGUI.TextField(nameRect, nameProp.displayName, nameProp.stringValue);
                GUI.backgroundColor = old;
            }
            else
            {
                nameProp.stringValue = EditorGUI.TextField(nameRect, nameProp.displayName, nameProp.stringValue);
            }
            EditorGUI.EndProperty();

            EditorGUI.LabelField(guidLabel, "GUID");
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.SelectableLabel(guidRect, guidProp != null ? guidProp.stringValue : "(missing)");

            var previewRect = new Rect(rect.x, headerRect.yMax + 6f, rect.width, 68f);
            DrawPrefabPreview(previewRect, prefabPropOverride);

            if (actionsProp != null)
            {
                float actH    = EditorGUI.GetPropertyHeight(actionsProp, true);
                var actRect   = new Rect(rect.x, previewRect.yMax + 4f, rect.width, actH);
                EditorGUI.PropertyField(actRect, actionsProp, new GUIContent("Expression Actions"), true);
            }
        }

        private static int CountName(SerializedProperty listProp, int excludeIndex, string name)
        {
            int count = 0;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (i == excludeIndex) continue;
                var n = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("DisplayName")
                        ?? listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
                if (n != null && n.stringValue == name) count++;
            }
            return count;
        }

        private static void EnsureGuids(SerializedProperty listProp)
        {
            if (listProp == null || !listProp.isArray) return;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var guidProp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("expressionID");
                if (guidProp != null && string.IsNullOrEmpty(guidProp.stringValue))
                    guidProp.stringValue = System.Guid.NewGuid().ToString("N");
            }
        }

        private static HashSet<string> GetDuplicateDisplayNames(SerializedProperty listProp)
        {
            var duplicates = new HashSet<string>();
            if (listProp == null || !listProp.isArray) return duplicates;

            var seen = new Dictionary<string, int>();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var n = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("DisplayName")
                        ?? listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
                if (n == null || string.IsNullOrWhiteSpace(n.stringValue)) continue;
                if (seen.ContainsKey(n.stringValue)) duplicates.Add(n.stringValue);
                else seen[n.stringValue] = i;
            }
            return duplicates;
        }

        private static void DrawPrefabPreview(Rect rect, SerializedProperty prefabProp)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.05f));
            if (prefabProp == null || !(prefabProp.objectReferenceValue is GameObject obj))
            {
                GUI.Label(rect, "(No prefab)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            const float pad = 6f;
            var inner = new Rect(rect.x + pad, rect.y + pad, rect.height - pad * 2f, rect.height - pad * 2f);
            var tex   = AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj);
            if (tex != null)
                GUI.DrawTexture(inner, tex, ScaleMode.ScaleToFit, true);
        }
    }
}
#endif
