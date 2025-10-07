using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1SimplePrefabRepresentationDisplay.html")]
    public class SimplePrefabRepresentationDisplay : ConvoCoreCharacterDisplayBase
    {
        private PrefabCharacterRepresentationData _catalog;

        public override void BindRepresentation(PrefabCharacterRepresentationData representationAsset)
        {
            _catalog = representationAsset;
        }

        public override void ApplyEmotion(string emotionId)
        {
            if (_catalog == null)
            {
                Debug.LogWarning("[SimplePrefabDisplay] Catalog not bound. Call BindRepresentation first.");
                return;
            }

            if (!_catalog.TryResolveById(emotionId, out var mapping))
            {
                Debug.LogWarning($"[SimplePrefabDisplay] EmotionId '{emotionId}' not found in '{_catalog.name}'.");
                return;
            }

            /*if (_animator && mapping.AnimatorController)
                _animator.runtimeAnimatorController = mapping.AnimatorController;

            if (_renderer && mapping.OverrideMaterial)
                _renderer.sharedMaterial = mapping.OverrideMaterial;*/

            // Add more payload effects as needed (SFX, particle, blend shapes...)
        }
        public override void ApplyDisplayOptions(DialogueLineDisplayOptions options)
        {
            // Example: scale/flip local transform
            var scale = Vector3.one;
            scale = Vector3.Scale(scale, options.FullBodyScale);
            transform.localScale = new Vector3(
                options.FlipFullBodyX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x),
                options.FlipFullBodyY ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y),
                scale.z);
        }
    }
}