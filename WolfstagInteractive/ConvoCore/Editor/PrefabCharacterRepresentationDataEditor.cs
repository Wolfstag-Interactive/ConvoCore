#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
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
            EnsureGuids(serializedObject.FindProperty("EmotionMappings"));

            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private void OnEnable() => BuildList();

        // ---------------- helpers ----------------

        private void BuildList()
        {
            var listProp = serializedObject.FindProperty("EmotionMappings");
            if (listProp == null) return;

            _list = new ReorderableList(serializedObject, listProp, true, true, true, true);

            _list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Emotion Mappings");
            };

            _list.elementHeightCallback = index =>
            {
                // header line + preview + padding
                const float pad = 6f;
                float header = EditorGUIUtility.singleLineHeight + 4f;
                const float preview = 68f; // stable, avoids jumping lists
                return header + preview + pad * 2f;
            };

            _list.drawElementCallback = (rect, index, active, focused) =>
            {
                var el = listProp.GetArrayElementAtIndex(index);

                var nameProp = el.FindPropertyRelative("DisplayName") ??
                               el.FindPropertyRelative("Name");

                var guidProp = el.FindPropertyRelative("emotionID");
                // header row: wider name + copyable, disabled GUID
                const float pad = 6f;
                rect = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f);

                var headerRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                float nameWidth = headerRect.width * 0.55f; // wider name
                var nameRect = new Rect(headerRect.x, headerRect.y, nameWidth, headerRect.height);
                var guidLabelRect = new Rect(nameRect.xMax + 8f, headerRect.y, 70f, headerRect.height);
                var guidRect = new Rect(guidLabelRect.xMax + 4f, headerRect.y,
                                        headerRect.xMax - (guidLabelRect.xMax + 4f), headerRect.height);

                EditorGUI.BeginProperty(nameRect, GUIContent.none, nameProp);
                nameProp.stringValue = EditorGUI.TextField(nameRect, nameProp.displayName, nameProp.stringValue);
                EditorGUI.EndProperty();

                EditorGUI.LabelField(guidLabelRect, "GUID");
                using (new EditorGUI.DisabledScope(true))
                {
                    // selectable label so designers can copy the ID
                    EditorGUI.SelectableLabel(guidRect, guidProp != null ? guidProp.stringValue : "(missing)");
                }

                // preview row
                var previewRect = new Rect(rect.x, headerRect.yMax + 6f, rect.width, rect.height - headerRect.height - 6f);
                DrawPrefabPreview(previewRect, serializedObject.FindProperty("CharacterPrefab"));
            };
        }

        private static void EnsureGuids(SerializedProperty listProp)
        {
            if (listProp == null || !listProp.isArray) return;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var el = listProp.GetArrayElementAtIndex(i);
                var guidProp = el.FindPropertyRelative("emotionID");

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