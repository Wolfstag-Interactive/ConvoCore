using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1SpriteCharacterRepresentationData.html")]
[CreateAssetMenu(fileName = "SpriteRepresentation", menuName = "ConvoCore/Sprite Representation")]
    public class SpriteCharacterRepresentationData : CharacterRepresentationBase
#if UNITY_EDITOR
        , IDialogueLineEditorCustomizable
#endif
    {
        [Header("Emotions (GUID-only)")] public List<SpriteEmotionMapping> EmotionMappings = new();

        public IReadOnlyList<(string id, string name)> GetEmotionCatalog() =>
            EmotionMappings.Select(m => (EmotionId: m.EmotionID, m.DisplayName)).ToList();

        private bool TryResolveById(string id, out SpriteEmotionMapping mapping)
        {
            mapping = EmotionMappings.FirstOrDefault(m => m.EmotionID == id);
            return mapping != null;
        }

        public override List<string> GetEmotionIDs() => EmotionMappings.Select(m => m.DisplayName).ToList();
        public override object GetEmotionMappingByGuid(string emotionGuid)
        {
            if (string.IsNullOrEmpty(emotionGuid))
                return null;
            
            return EmotionMappings.FirstOrDefault(m => m.EmotionID == emotionGuid);
        }

        public override object ProcessEmotion(string emotionId)
        {
            if (string.IsNullOrEmpty(emotionId))
            {
                return EmotionMappings.Count > 0 ? EmotionMappings[0] : null;
            }

            if (TryResolveById(emotionId, out var byGuid))
                return byGuid;

            Debug.LogWarning($"Sprite emotion '{emotionId}' not found; using first mapping as fallback.");
            return EmotionMappings.Count > 0 ? EmotionMappings[0] : null;
        }

#if UNITY_EDITOR
        public Rect DrawDialogueLineOptions(Rect rect, string emotionID, SerializedProperty displayOptionsProperty,
            float spacing) => rect;

        public float GetDialogueLineOptionsHeight(string emotionID, SerializedProperty displayOptionsProperty) => 0f;

        public override float GetPreviewHeight() => 84f;

        public override void DrawInlineEditorPreview(object mappingData, Rect position)
        {
            // FIX: parenthesize the ternary before using ??
            var mapping = (mappingData as SpriteEmotionMapping) ??
                          (EmotionMappings.Count > 0 ? EmotionMappings[0] : null);
            if (mapping == null)
            {
                EditorGUI.LabelField(position, "No sprite mapping to preview.");
                return;
            }

            Texture2D portraitTex = mapping.PortraitSprite != null ? mapping.PortraitSprite.texture : null;
            Texture2D fullBodyTex = mapping.FullBodySprite != null ? mapping.FullBodySprite.texture : null;

            // FIX: explicit null checks—can't treat Texture2D as bool
            int count = (portraitTex != null ? 1 : 0) + (fullBodyTex != null ? 1 : 0);
            if (count == 0)
            {
                EditorGUI.LabelField(position, "(No sprites)");
                return;
            }

            const float pad = 4f;
            var inner = new Rect(position.x + pad, position.y + pad,
                position.width - pad * 2f, position.height - pad * 2f);

            if (count == 1)
            {
                var tex = portraitTex != null ? portraitTex : fullBodyTex;
                if (tex)
                {
                    GUI.DrawTexture(FitRectPreserveAspect(inner, tex.width, tex.height), tex, ScaleMode.ScaleToFit, true);
                }
            }
            else
            {
                float slotW = (inner.width - pad) * 0.5f;
                var left = new Rect(inner.x, inner.y, slotW, inner.height);
                var right = new Rect(inner.x + slotW + pad, inner.y, slotW, inner.height);

                if (portraitTex != null)
                    GUI.DrawTexture(FitRectPreserveAspect(left, portraitTex.width, portraitTex.height), portraitTex,
                        ScaleMode.ScaleToFit, true);
                if (fullBodyTex != null)
                    GUI.DrawTexture(FitRectPreserveAspect(right, fullBodyTex.width, fullBodyTex.height), fullBodyTex,
                        ScaleMode.ScaleToFit, true);
            }
        }

        private static Rect FitRectPreserveAspect(Rect container, float texW, float texH)
        {
            if (texW <= 0f || texH <= 0f) return container;

            float ar = texW / texH;
            float targetW = container.width;
            float targetH = targetW / ar;

            if (targetH > container.height)
            {
                targetH = container.height;
                targetW = targetH * ar;
            }

            float x = container.x + (container.width - targetW) * 0.5f;
            float y = container.y + (container.height - targetH) * 0.5f;
            return new Rect(x, y, targetW, targetH);
        }

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

#endif
    }

    [System.Serializable]
    public class SpriteEmotionMapping
    {
        [SerializeField, Tooltip("Stable unique ID (GUID). Non-editable.")]
        private string emotionID = System.Guid.NewGuid().ToString("N");

        public string EmotionID => emotionID;

        [Tooltip("Human-readable name shown in dropdowns and inspector list headers.")]
        public string DisplayName = "Neutral";

        [Tooltip("Portrait sprite for the emotion.")]
        public Sprite PortraitSprite;

        [Tooltip("Full body sprite for the emotion.")]
        public Sprite FullBodySprite;

        [Header("Default Display Options")]
        public DialogueLineDisplayOptions DisplayOptions = new DialogueLineDisplayOptions();

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