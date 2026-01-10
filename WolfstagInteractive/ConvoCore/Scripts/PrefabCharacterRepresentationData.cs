using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1PrefabCharacterRepresentationData.html")]
[CreateAssetMenu(fileName = "PrefabCharacterRepresentation",
        menuName = "ConvoCore/Character/Representation/Prefab Character Representation")]
    //This class dictates how a prefab should be represented by telling ConvoCore how to get and apply expressions and expression actions 
    public class PrefabCharacterRepresentationData : CharacterRepresentationBase
#if UNITY_EDITOR
        , IDialogueLineEditorCustomizable
#endif
    {
        [Header("Prefab Settings")]
        public GameObject CharacterPrefab;

        public List<PrefabExpressionMapping> ExpressionMappings = new();

        // GUID catalog for editor selectors
        public IReadOnlyList<(string id, string name)> GetExpressionCatalog() =>
            ExpressionMappings.Select(m => (ExpressionId: m.ExpressionID, m.DisplayName)).ToList();

        public bool TryResolveById(string id, out PrefabExpressionMapping mapping)
        {
            mapping = ExpressionMappings.FirstOrDefault(m => m.ExpressionID == id);
            return mapping != null;
        }
        public override void ApplyExpression(string expressionId, ConvoCore runtime, ConvoCoreConversationData conversation, int lineIndex,
            IConvoCoreCharacterDisplay display)
        {
            if (!TryResolveById(expressionId, out var mapping))
            {
                Debug.LogWarning($"[PrefabCharacterRepresentationData] Expression '{expressionId}' not found on '{name}'.");
                return;
            }

            var actions = mapping.ExpressionActions;
            if (actions == null || actions.Count == 0)
                return;

            var ctx = new ExpressionActionContext
            {
                Runtime      = runtime,
                Conversation = conversation,
                LineIndex    = lineIndex,
                Representation = this,
                Display      = display,
                ExpressionId = mapping.ExpressionID
            };

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action != null)
                    action.ExecuteAction(ctx);
            }
            
        }

        public override object GetExpressionMappingByGuid(string expressionGuid)
        {
            if (string.IsNullOrEmpty(expressionGuid))
                return null;
            
            return ExpressionMappings.FirstOrDefault(m => m.ExpressionID == expressionGuid);
            
        }

        // Prefab flow does not use this directly; spawner binds and applies by GUID
        public override object ProcessExpression(string expressionId) => expressionId;

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
                // queue a repaint so when the preview is ready, we draw it
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

        public Rect DrawDialogueLineOptions(Rect rect, string expressionID, UnityEditor.SerializedProperty displayOptionsProperty, float spacing) => rect;
        public float GetDialogueLineOptionsHeight(string expressionID, UnityEditor.SerializedProperty displayOptionsProperty) => 0f;
        
#endif

        private void OnValidate()
        {
            var used = new HashSet<string>();
            foreach (var m in ExpressionMappings)
            {
                if (m == null) continue;
                m.EnsureValidId(used);
                m.EnsureValidBasics();
            }
        }
    }

    [System.Serializable]
    // This class defines what a prefab expression is
    public sealed class PrefabExpressionMapping
    {
        [SerializeField, Tooltip("Stable unique ID (GUID). Non-editable.")]
        private string expressionID = System.Guid.NewGuid().ToString("N");
        public string ExpressionID => expressionID;

        [Tooltip("Human-readable name shown in dropdowns and inspector list headers.")]
        public string DisplayName = "Neutral";
        [Tooltip("Actions that run when this expression is applied on this representation")]
        public List<BaseExpressionAction> ExpressionActions = new();
        public void EnsureValidId(HashSet<string> used)
        {
            if (string.IsNullOrWhiteSpace(expressionID) || !used.Add(expressionID))
                expressionID = System.Guid.NewGuid().ToString("N");
        }

        public void EnsureValidBasics()
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = "Unnamed";
        }
    }
}