using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "PrefabCharacterRepresentation",
        menuName = "ConvoCore/Prefab Character Representation")]
    public class PrefabCharacterRepresentationData : CharacterRepresentationBase
#if UNITY_EDITOR
        , IDialogueLineEditorCustomizable
#endif
    {
        [Header("Prefab Settings")]
        public GameObject CharacterPrefab;

        [Header("Emotions (GUID-only)")]
        public List<EmotionPrefabMapping> EmotionMappings = new();

        // GUID catalog for editor selectors
        public IReadOnlyList<(string id, string name)> GetEmotionCatalog() =>
            EmotionMappings.Select(m => (EmotionId: m.EmotionID, m.DisplayName)).ToList();

        public bool TryResolveById(string id, out EmotionPrefabMapping mapping)
        {
            mapping = EmotionMappings.FirstOrDefault(m => m.EmotionID == id);
            return mapping != null;
        }

        // Legacy/UI convenience no longer used – return names only if some old drawer calls it
        public override List<string> GetEmotionIDs() => EmotionMappings.Select(m => m.DisplayName).ToList();

        // Prefab flow doesn’t use this directly; spawner binds + applies by GUID
        public override object ProcessEmotion(string emotionId) => emotionId;

#if UNITY_EDITOR
        public override float GetPreviewHeight() => CharacterPrefab ? 80f : 0f;
        public override void DrawInlineEditorPreview(object mappingData, Rect position)
        {
            if (!CharacterPrefab)
            {
                UnityEditor.EditorGUI.LabelField(position, "No Prefab");
                return;
            }

            // Ask AssetPreview; if not ready, request a repaint
            var tex = UnityEditor.AssetPreview.GetAssetPreview(CharacterPrefab) 
                      ?? UnityEditor.AssetPreview.GetMiniThumbnail(CharacterPrefab);

            if (!tex)
            {
                // queue a repaint so when the preview is ready we draw it
                if (Event.current.type == EventType.Repaint)
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                UnityEditor.EditorGUI.LabelField(position, "Generating preview…");
                return;
            }

            // Fit rect preserving aspect
            Rect fit = FitRectPreserveAspect(position, tex.width, tex.height, 2f);
            GUI.DrawTexture(fit, tex, ScaleMode.ScaleToFit, true);
        }

        // Small helper local to this class (editor-only)
        private static Rect FitRectPreserveAspect(Rect container, float w, float h, float pad)
        {
            var inner = new Rect(container.x + pad, container.y + pad, container.width - 2*pad, container.height - 2*pad);
            if (w <= 0f || h <= 0f) return inner;

            float ar = w / h;
            float targetW = inner.width;
            float targetH = targetW / ar;
            if (targetH > inner.height)
            {
                targetH = inner.height;
                targetW = targetH * ar;
            }
            float x = inner.x + (inner.width  - targetW) * 0.5f;
            float y = inner.y + (inner.height - targetH) * 0.5f;
            return new Rect(x, y, targetW, targetH);
        }

        public Rect DrawDialogueLineOptions(Rect rect, string emotionID, UnityEditor.SerializedProperty displayOptionsProperty, float spacing) => rect;
        public float GetDialogueLineOptionsHeight(string emotionID, UnityEditor.SerializedProperty displayOptionsProperty) => 0f;
        
#endif

        private void OnValidate()
        {
            var used = new HashSet<string>();
            foreach (var m in EmotionMappings)
            {
                if (m == null) continue;
                m.EnsureValidId(used);
                m.EnsureValidBasics();
            }
        }
    }

    [System.Serializable]
    public class EmotionPrefabMapping
    {
        [SerializeField, Tooltip("Stable unique ID (GUID). Non-editable.")]
        private string emotionID = System.Guid.NewGuid().ToString("N");
        public string EmotionID => emotionID;

        [Tooltip("Human-readable name shown in dropdowns and inspector list headers.")]
        public string DisplayName = "Neutral";
        public void EnsureValidId(HashSet<string> used)
        {
            if (string.IsNullOrWhiteSpace(emotionID) || !used.Add(emotionID))
                emotionID = System.Guid.NewGuid().ToString("N");
        }

        public void EnsureValidBasics()
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = "Unnamed";
        }
    }
}