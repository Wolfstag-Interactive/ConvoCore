using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Prefab display component that maps expression display names to SkinnedMeshRenderer
    /// blend shape indices and target weights. Supports optional smooth blending over time.
    ///
    /// Expression display names are matched against <see cref="PrefabExpressionMapping.DisplayName"/>
    /// on the bound <see cref="PrefabCharacterRepresentationData"/> asset at bind time, building
    /// a runtime GUID-to-blend-shape lookup.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreBlendShapeDisplay.html")]
    public class ConvoCoreBlendShapeDisplay : ConvoCoreCharacterDisplayBase
    {
        [Header("Renderer")]
        [Tooltip("SkinnedMeshRenderer whose blend shapes are driven by expressions. Auto-resolved if left empty.")]
        [SerializeField] private SkinnedMeshRenderer _renderer;

        [Header("Blend Settings")]
        [Tooltip("Time in seconds to transition between blend shape weights. 0 = instant.")]
        [SerializeField] private float _transitionSpeed = 0.2f;

        [Header("Expression Mappings")]
        [Tooltip("Maps expression display names (matching the representation asset) to blend shape indices and weights.")]
        [SerializeField] private List<BlendShapeExpressionMapping> _expressionMappings = new();

        private readonly Dictionary<string, BlendShapeExpressionMapping> _runtimeLookup = new();
        private PrefabCharacterRepresentationData _lastBoundRep;
        private Coroutine _blendCoroutine;

        protected override void Awake()
        {
            base.Awake();
            if (_renderer == null)
                _renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        public override void BindRepresentation(CharacterRepresentationBase representationAsset)
        {
            if (representationAsset == _lastBoundRep) return;
            _lastBoundRep = representationAsset as PrefabCharacterRepresentationData;

            _runtimeLookup.Clear();

            if (_lastBoundRep == null)
            {
                Debug.LogWarning($"[ConvoCoreBlendShapeDisplay] Expected PrefabCharacterRepresentationData " +
                                 $"but received '{representationAsset?.GetType().Name}'.");
                return;
            }

            foreach (var exprMapping in _lastBoundRep.ExpressionMappings)
            {
                var bsMapping = _expressionMappings.Find(m => m.ExpressionDisplayName == exprMapping.DisplayName);
                if (bsMapping != null)
                    _runtimeLookup[exprMapping.ExpressionID] = bsMapping;
                else
                    Debug.LogWarning($"[ConvoCoreBlendShapeDisplay] No blend shape mapping found for expression " +
                                     $"'{exprMapping.DisplayName}' on '{gameObject.name}'.");
            }
        }

        public override void ApplyExpression(string expressionId)
        {
            if (_renderer == null)
            {
                Debug.LogWarning("[ConvoCoreBlendShapeDisplay] No SkinnedMeshRenderer found.");
                return;
            }

            if (!_runtimeLookup.TryGetValue(expressionId, out var mapping))
            {
                Debug.LogWarning($"[ConvoCoreBlendShapeDisplay] Expression GUID '{expressionId}' not found in runtime lookup.");
                return;
            }

            if (_blendCoroutine != null)
                StopCoroutine(_blendCoroutine);

            if (_transitionSpeed <= 0f)
            {
                _renderer.SetBlendShapeWeight(mapping.BlendShapeIndex, mapping.TargetWeight);
            }
            else
            {
                _blendCoroutine = StartCoroutine(BlendTo(mapping.BlendShapeIndex, mapping.TargetWeight));
            }
        }

        private IEnumerator BlendTo(int index, float targetWeight)
        {
            float startWeight = _renderer.GetBlendShapeWeight(index);
            float elapsed = 0f;

            while (elapsed < _transitionSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _transitionSpeed);
                _renderer.SetBlendShapeWeight(index, Mathf.Lerp(startWeight, targetWeight, t));
                yield return null;
            }

            _renderer.SetBlendShapeWeight(index, targetWeight);
            _blendCoroutine = null;
        }
    }

    [System.Serializable]
    public class BlendShapeExpressionMapping
    {
        [Tooltip("Must match the Display Name on the corresponding PrefabExpressionMapping in the representation asset.")]
        public string ExpressionDisplayName;

        [Tooltip("Index of the blend shape on the SkinnedMeshRenderer.")]
        public int BlendShapeIndex;

        [Tooltip("Target blend shape weight (0-100).")]
        [Range(0f, 100f)]
        public float TargetWeight;
    }
}
