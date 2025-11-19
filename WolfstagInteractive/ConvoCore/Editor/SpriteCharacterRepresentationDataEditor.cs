#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1SpriteCharacterRepresentationDataEditor.html")]
[CustomEditor(typeof(SpriteCharacterRepresentationData))]
    public class SpriteCharacterRepresentationDataEditor : UnityEditor.Editor
    {
        private ReorderableList _list;

        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EnsureGuidsIfMissing(serializedObject);
            
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

        private static void EnsureGuidsIfMissing(SerializedObject so)
        {
            var listProp = so.FindProperty("ExpressionMappings");
            if (listProp == null || !listProp.isArray) return;

            bool changed = false;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var el = listProp.GetArrayElementAtIndex(i);
                var guidProp = el.FindPropertyRelative("expressionID");

                if (guidProp != null && string.IsNullOrEmpty(guidProp.stringValue))
                {
                    guidProp.stringValue = System.Guid.NewGuid().ToString("N");
                    changed = true;
                }
            }

            if (changed) so.ApplyModifiedProperties();
        }
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

            _list.drawHeaderCallback = r => EditorGUI.LabelField(r, "Expression Mappings");

            _list.elementHeightCallback = i =>
            {
                const float pad = 6f;
                float headerH = EditorGUIUtility.singleLineHeight + 4f;
                float objFieldH = EditorGUIUtility.singleLineHeight;
                float previewH = 96f; // a bit taller for better visibility

                var el = listProp.GetArrayElementAtIndex(i);
                var optsProp = el.FindPropertyRelative("DisplayOptions");
                float optsH = optsProp != null ? EditorGUI.GetPropertyHeight(optsProp, true) : 0f;

                return headerH + (objFieldH + previewH) + 4f + optsH + pad * 2f + 6f;
            };

            _list.drawElementCallback = (rect, index, active, focused) =>
            {
                var el = listProp.GetArrayElementAtIndex(index);

                var nameProp = el.FindPropertyRelative("DisplayName") ?? el.FindPropertyRelative("Name");
                var guidProp = el.FindPropertyRelative("expressionID");
                var portProp = el.FindPropertyRelative("PortraitSprite");
                var fullProp = el.FindPropertyRelative("FullBodySprite");
                var optsProp = el.FindPropertyRelative("DisplayOptions");

                const float pad = 6f;
                rect = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f);
                // Check if this name is a duplicate
                bool isDuplicate = false;
                string currentName = nameProp.stringValue;
                if (!string.IsNullOrWhiteSpace(currentName))
                {
                    int duplicateCount = 0;
                    for (int i = 0; i < listProp.arraySize; i++)
                    {
                        if (i == index) continue;
                        var otherEl = listProp.GetArrayElementAtIndex(i);
                        var otherName = otherEl.FindPropertyRelative("DisplayName") ?? otherEl.FindPropertyRelative("Name");
                        if (otherName != null && otherName.stringValue == currentName)
                        {
                            duplicateCount++;
                        }
                    }
                    isDuplicate = duplicateCount > 0;
                }
                // Header row: Display Name (editable) + GUID (readonly, copyable)
                var headerRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                // Reserve space for warning icon if duplicate
                float warningIconWidth = isDuplicate ? 20f : 0f;
                float nameWidth = (headerRect.width - warningIconWidth) * 0.60f;
                
                // Draw warning icon first if duplicate
                if (isDuplicate)
                {
                    var warningRect = new Rect(headerRect.x, headerRect.y, warningIconWidth, headerRect.height);
                    var warningContent = new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml"));
                    warningContent.tooltip = "Duplicate name detected! Each expression should have a unique Display Name.";
                    EditorGUI.LabelField(warningRect, warningContent);
                }
                
                
                var nameRect = new Rect(headerRect.x+ warningIconWidth, headerRect.y, nameWidth, headerRect.height);
                var guidLabelRect = new Rect(nameRect.xMax + 8f, headerRect.y, 44f, headerRect.height);
                var guidRect = new Rect(guidLabelRect.xMax + 4f, headerRect.y,
                    headerRect.xMax - (guidLabelRect.xMax + 4f), headerRect.height);
                // Highlight the name field if duplicate
                if (isDuplicate)
                {
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.8f, 0.6f); // orange tint
                    nameProp.stringValue = EditorGUI.TextField(nameRect, "Display Name", nameProp.stringValue);
                    GUI.backgroundColor = oldColor;
                }
                else
                {
                    nameProp.stringValue = EditorGUI.TextField(nameRect, "Display Name", nameProp.stringValue);
                }

                EditorGUI.LabelField(guidLabelRect, "GUID");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.SelectableLabel(guidRect, guidProp != null ? guidProp.stringValue : "(missing)");
                }

                // Two columns (object field + preview)
                float colGap = 12f;
                float colWidth = (rect.width - colGap) * 0.5f;
                float topY = headerRect.yMax + 6f;
                float totalColH = EditorGUIUtility.singleLineHeight + 96f;

                var portRect = new Rect(rect.x, topY, colWidth, totalColH);
                var fullRect = new Rect(rect.x + colWidth + colGap, topY, colWidth, totalColH);

                // Make object fields wider (smaller label width while drawing these two)
                float oldLW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80f; // wider field, shorter label
                DrawSpriteFieldWithPreview(portRect, portProp, "Portrait");
                DrawSpriteFieldWithPreview(fullRect, fullProp, "Full Body");
                EditorGUIUtility.labelWidth = oldLW;

                // Display Options: draw WITH children so the foldout expands properly
                if (optsProp != null)
                {
                    float optsH = EditorGUI.GetPropertyHeight(optsProp, true);
                    var optsRect = new Rect(rect.x, topY + totalColH + 4f, rect.width, optsH);
                    EditorGUI.PropertyField(optsRect, optsProp, new GUIContent("Default Display Options"), true);
                }
            };
        }


        private static void DrawSpriteFieldWithPreview(Rect rect, SerializedProperty spriteProp, string label)
        {
            var fieldRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(fieldRect, spriteProp, new GUIContent(label));

            var previewRect = new Rect(rect.x, fieldRect.yMax + 2f, rect.width,
                Mathf.Max(0f, rect.height - (EditorGUIUtility.singleLineHeight + 2f)));

            var sprite = spriteProp != null ? spriteProp.objectReferenceValue as Sprite : null;

            // Subtle background so empty previews are still visually aligned
            EditorGUI.DrawRect(previewRect, new Color(0f, 0f, 0f, 0.04f));

            if (sprite == null || sprite.texture == null) return;

            var tex = sprite.texture;
            var r   = sprite.rect;
            var uv  = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);

            float aspect  = r.width / r.height;
            float targetW = previewRect.height * aspect;
            float targetH = previewRect.height;

            if (targetW > previewRect.width)
            {
                targetW = previewRect.width;
                targetH = previewRect.width / aspect;
            }

            var fit = new Rect(
                previewRect.x + (previewRect.width - targetW) * 0.5f,
                previewRect.y + (previewRect.height - targetH) * 0.5f,
                targetW, targetH
            );

            GUI.DrawTextureWithTexCoords(fit, tex, uv, true);
        }
    }
}
#endif