using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Defines how a prefab-based character should be sourced and displayed by ConvoCore.
    ///
    /// Two source modes are supported:
    /// <list type="bullet">
    ///   <item><see cref="CharacterSourceMode.SpawnFromPrefab"/> -- ConvoCore spawns and pools the prefab. ConvoCore owns the lifecycle.</item>
    ///   <item><see cref="CharacterSourceMode.SceneResident"/> -- ConvoCore locates an already-existing scene instance via a <see cref="ConvoCoreSceneCharacterRegistry"/>. ConvoCore never spawns, pools, or destroys the instance.</item>
    /// </list>
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1PrefabCharacterRepresentationData.html")]
    [CreateAssetMenu(fileName = "PrefabCharacterRepresentation",
        menuName = "ConvoCore/Character/Representation/Prefab Character Representation")]
    public class PrefabCharacterRepresentationData : CharacterRepresentationBase
#if UNITY_EDITOR
        , IDialogueLineEditorCustomizable
#endif
    {
        [Header("Source Mode")]
        [Tooltip("SpawnFromPrefab: ConvoCore instantiates and pools the prefab. ConvoCore owns the lifecycle.\n\nSceneResident: ConvoCore locates a pre-existing scene instance via a registry. ConvoCore never spawns or destroys the instance.")]
        public CharacterSourceMode SourceMode = CharacterSourceMode.SpawnFromPrefab;

        [Tooltip("Required when Source Mode is SpawnFromPrefab. The prefab to instantiate. Must have an IConvoCoreCharacterDisplay component on it or a child.")]
        public GameObject CharacterPrefab;

        [Tooltip("Required when Source Mode is SceneResident. Must match the Character ID on the ConvoCoreSceneCharacterRegistrant component in the scene.")]
        public string SceneCharacterId;

        [Header("Expressions")]
        public List<PrefabExpressionMapping> ExpressionMappings = new();

        // GUID catalog for editor selectors
        public IReadOnlyList<(string id, string name)> GetExpressionCatalog() =>
            ExpressionMappings.Select(m => (ExpressionId: m.ExpressionID, m.DisplayName)).ToList();

        public bool TryResolveById(string id, out PrefabExpressionMapping mapping)
        {
            mapping = ExpressionMappings.FirstOrDefault(m => m.ExpressionID == id);
            return mapping != null;
        }

        public override void ApplyExpression(string expressionId, ConvoCore runtime,
            ConvoCoreConversationData conversation, int lineIndex, IConvoCoreCharacterDisplay display)
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
                Runtime        = runtime,
                Conversation   = conversation,
                LineIndex      = lineIndex,
                Representation = this,
                Display        = display,
                ExpressionId   = mapping.ExpressionID
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

        // Prefab flow does not use ProcessExpression directly; the spawner binds and applies by GUID.
        public override object ProcessExpression(string expressionId) => expressionId;

#if UNITY_EDITOR
        public override float GetPreviewHeight() => CharacterPrefab ? 80f : 0f;

        public override void DrawInlineEditorPreview(object mappingData, Rect position)
        {
            if (!CharacterPrefab)
            {
                UnityEditor.EditorGUI.LabelField(position, SourceMode == CharacterSourceMode.SceneResident
                    ? "Scene Resident (no prefab preview)"
                    : "No Prefab");
                return;
            }

            var tex = UnityEditor.AssetPreview.GetAssetPreview(CharacterPrefab)
                      ?? UnityEditor.AssetPreview.GetMiniThumbnail(CharacterPrefab);

            if (!tex)
            {
                if (Event.current.type == EventType.Repaint)
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                UnityEditor.EditorGUI.LabelField(position, "Generating preview...");
                return;
            }

            Rect fit = FitRectPreserveAspect(position, tex.width, tex.height, 2f);
            GUI.DrawTexture(fit, tex, ScaleMode.ScaleToFit, true);
        }

        private static Rect FitRectPreserveAspect(Rect container, float w, float h, float pad)
        {
            var inner = new Rect(container.x + pad, container.y + pad,
                container.width - 2 * pad, container.height - 2 * pad);

            if (w <= 0f || h <= 0f) return inner;

            float ar = w / h;
            float targetW = inner.width;
            float targetH = targetW / ar;
            if (targetH > inner.height)
            {
                targetH = inner.height;
                targetW = targetH * ar;
            }

            float x = inner.x + (inner.width - targetW) * 0.5f;
            float y = inner.y + (inner.height - targetH) * 0.5f;
            return new Rect(x, y, targetW, targetH);
        }

        public Rect DrawDialogueLineOptions(Rect rect, string expressionID,
            UnityEditor.SerializedProperty displayOptionsProperty, float spacing) => rect;

        public float GetDialogueLineOptionsHeight(string expressionID,
            UnityEditor.SerializedProperty displayOptionsProperty) => 0f;
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

#if UNITY_EDITOR
            switch (SourceMode)
            {
                case CharacterSourceMode.SpawnFromPrefab when CharacterPrefab == null:
                    Debug.LogWarning($"[PrefabCharacterRepresentationData] '{name}' is set to SpawnFromPrefab but has no CharacterPrefab assigned.");
                    break;
                case CharacterSourceMode.SceneResident when string.IsNullOrEmpty(SceneCharacterId):
                    Debug.LogWarning($"[PrefabCharacterRepresentationData] '{name}' is set to SceneResident but has no SceneCharacterId assigned.");
                    break;
            }
#endif
        }
    }

    /// <summary>
    /// Determines how ConvoCore resolves the live <see cref="IConvoCoreCharacterDisplay"/>
    /// instance for a <see cref="PrefabCharacterRepresentationData"/> asset.
    /// </summary>
    public enum CharacterSourceMode
    {
        /// <summary>
        /// ConvoCore instantiates the prefab via the pool and owns the full lifecycle.
        /// The character is spawned when needed and returned to the pool when released.
        /// </summary>
        SpawnFromPrefab,

        /// <summary>
        /// ConvoCore locates an already-existing scene instance via a <see cref="ConvoCoreSceneCharacterRegistry"/>.
        /// ConvoCore never spawns, pools, or destroys the instance.
        /// The developer is fully responsible for the character's lifecycle.
        /// </summary>
        SceneResident
    }

    [System.Serializable]
    public sealed class PrefabExpressionMapping
    {
        [SerializeField, Tooltip("Stable unique ID (GUID). Non-editable.")]
        private string expressionID = System.Guid.NewGuid().ToString("N");
        public string ExpressionID => expressionID;

        [Tooltip("Human-readable name shown in dropdowns and inspector list headers.")]
        public string DisplayName;

        [Tooltip("ScriptableObject actions executed when this expression is applied. Evaluated in list order.")]
        public List<BaseExpressionAction> ExpressionActions = new();

        public void EnsureValidId(HashSet<string> usedIds)
        {
            if (string.IsNullOrEmpty(expressionID) || !usedIds.Add(expressionID))
                expressionID = System.Guid.NewGuid().ToString("N");
        }

        public void EnsureValidBasics()
        {
            if (string.IsNullOrEmpty(DisplayName))
                DisplayName = "Unnamed Expression";
        }
    }
}