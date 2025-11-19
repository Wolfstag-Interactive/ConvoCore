#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1PrefabCharacterRepresentationDataEditor.html")]
[CustomEditor(typeof(PrefabCharacterRepresentationData))]
    public class PrefabCharacterRepresentationDataEditor : UnityEditor.Editor
    {
        private ReorderableList _list;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Character Prefab first
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CharacterPrefab"));

            if (_list == null) BuildList(); // safety in case domain reload order changes

            // Ensure each element has a GUID before drawing
            EnsureGuids(serializedObject.FindProperty("ExpressionMappings"));
            
            // Check for duplicate names and show warning at the top
            var duplicateNames = GetDuplicateDisplayNames(serializedObject.FindProperty("ExpressionMappings"));
            if (duplicateNames.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Warning: Duplicate expression names detected: {string.Join(", ", duplicateNames)}. Each expression should have a unique Display Name.",
                    MessageType.Warning);
            }

            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private void OnEnable() => BuildList();

        private static System.Collections.Generic.HashSet<string> GetDuplicateDisplayNames(SerializedProperty listProp)
        {
            var duplicates = new System.Collections.Generic.HashSet<string>();
            if (listProp == null || !listProp.isArray) return duplicates;

            var seen = new System.Collections.Generic.Dictionary<string, int>();
            
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var el = listProp.GetArrayElementAtIndex(i);
                var nameProp = el.FindPropertyRelative("DisplayName") ?? el.FindPropertyRelative("Name");
                if (nameProp == null) continue;

                string name = nameProp.stringValue;
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (seen.ContainsKey(name))
                {
                    duplicates.Add(name);
                }
                else
                {
                    seen[name] = i;
                }
            }

            return duplicates;
        }
private void BuildList()
{
    var listProp = serializedObject.FindProperty("ExpressionMappings");
    if (listProp == null) return;

    _list = new ReorderableList(serializedObject, listProp, true, true, true, true);

    _list.drawHeaderCallback = rect =>
    {
        EditorGUI.LabelField(rect, "Expression Mappings");
    };

    _list.elementHeightCallback = index =>
    {
        const float pad = 6f;
        const float preview = 68f;
        float header = EditorGUIUtility.singleLineHeight;

        // Base height: padding + header + spacing + preview + padding
        float height = pad + header + 6f + preview + pad;

        // Add dynamic height for the ExpressionActions list
        if (listProp != null && listProp.isArray && index >= 0 && index < listProp.arraySize)
        {
            var el = listProp.GetArrayElementAtIndex(index);
            var actionsProp = el.FindPropertyRelative("ExpressionActions");
            if (actionsProp != null)
            {
                // Add a bit of spacing before actions + the property height
                height += 4f + EditorGUI.GetPropertyHeight(actionsProp, true);
            }
        }

        return height;
    };

    _list.drawElementCallback = (rect, index, active, focused) =>
    {
        var el = listProp.GetArrayElementAtIndex(index);

        var nameProp = el.FindPropertyRelative("DisplayName") ??
                       el.FindPropertyRelative("Name");

        var guidProp = el.FindPropertyRelative("expressionID");
        var actionsProp = el.FindPropertyRelative("ExpressionActions");

        // Duplicate name detection
        bool isDuplicate = false;
        string currentName = nameProp.stringValue;
        if (!string.IsNullOrWhiteSpace(currentName))
        {
            int duplicateCount = 0;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (i == index) continue;
                var otherEl = listProp.GetArrayElementAtIndex(i);
                var otherName = otherEl.FindPropertyRelative("DisplayName") ??
                                otherEl.FindPropertyRelative("Name");
                if (otherName != null && otherName.stringValue == currentName)
                {
                    duplicateCount++;
                }
            }
            isDuplicate = duplicateCount > 0;
        }

        const float pad = 6f;
        rect = new Rect(rect.x + pad, rect.y + pad,
                        rect.width - pad * 2f, rect.height - pad * 2f);

        // Header row
        var headerRect = new Rect(rect.x, rect.y,
                                  rect.width, EditorGUIUtility.singleLineHeight);

        float warningIconWidth = isDuplicate ? 20f : 0f;
        float nameWidth = (headerRect.width - warningIconWidth) * 0.55f;

        // Warning icon for duplicates
        if (isDuplicate)
        {
            var warningRect = new Rect(headerRect.x, headerRect.y,
                                       warningIconWidth, headerRect.height);
            var warningContent = new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml"))
            {
                tooltip = "Duplicate name detected! Each expression should have a unique Display Name."
            };
            EditorGUI.LabelField(warningRect, warningContent);
        }

        var nameRect = new Rect(headerRect.x + warningIconWidth, headerRect.y,
                                nameWidth, headerRect.height);
        var guidLabelRect = new Rect(nameRect.xMax + 8f, headerRect.y,
                                     70f, headerRect.height);
        var guidRect = new Rect(guidLabelRect.xMax + 4f, headerRect.y,
                                headerRect.xMax - (guidLabelRect.xMax + 4f), headerRect.height);

        // DisplayName field (highlight if duplicate)
        EditorGUI.BeginProperty(nameRect, GUIContent.none, nameProp);
        if (isDuplicate)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.6f);
            nameProp.stringValue = EditorGUI.TextField(nameRect, nameProp.displayName, nameProp.stringValue);
            GUI.backgroundColor = oldColor;
        }
        else
        {
            nameProp.stringValue = EditorGUI.TextField(nameRect, nameProp.displayName, nameProp.stringValue);
        }
        EditorGUI.EndProperty();

        // GUID label
        EditorGUI.LabelField(guidLabelRect, "GUID");
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.SelectableLabel(guidRect,
                guidProp != null ? guidProp.stringValue : "(missing)");
        }

        // Prefab preview row
        var previewRect = new Rect(
            rect.x,
            headerRect.yMax + 6f,
            rect.width,
            68f);

        DrawPrefabPreview(previewRect, serializedObject.FindProperty("CharacterPrefab"));

        // Actions list row (under the preview)
        if (actionsProp != null)
        {
            float actionsY = previewRect.yMax + 4f;
            float actionsHeight = EditorGUI.GetPropertyHeight(actionsProp, true);
            var actionsRect = new Rect(rect.x, actionsY, rect.width, actionsHeight);

            EditorGUI.PropertyField(actionsRect, actionsProp,
                new GUIContent("Expression Actions"), true);
        }
    };
}

        private static void EnsureGuids(SerializedProperty listProp)
        {
            if (listProp == null || !listProp.isArray) return;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var el = listProp.GetArrayElementAtIndex(i);
                var guidProp = el.FindPropertyRelative("expressionID");

                if (guidProp != null && string.IsNullOrEmpty(guidProp.stringValue))
                {
                    guidProp.stringValue = System.Guid.NewGuid().ToString("N");
                }
            }
        }

        private static void DrawPrefabPreview(Rect rect, SerializedProperty prefabProp)
        {
            if (prefabProp == null) return;
            var obj = prefabProp.objectReferenceValue as GameObject;
            if (obj == null)
            {
                // subtle empty box
                EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.05f));
                GUI.Label(rect, "(Assign Character Prefab to see preview)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // frame with padding
            const float pad = 6f;
            var inner = new Rect(rect.x + pad, rect.y + pad, rect.height - pad * 2f, rect.height - pad * 2f);
            var tex = AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj);
            if (tex != null) GUI.DrawTexture(inner, tex, ScaleMode.ScaleToFit, true);
        }
    }
}
#endif