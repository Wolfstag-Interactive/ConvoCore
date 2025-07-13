using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "PrefabCharacterRepresentation", menuName = "ConvoCore/Prefab Character Representation")]
    public class PrefabCharacterRepresentationData : CharacterRepresentationBase
    {
        [Header("Prefab Settings")]
        public GameObject CharacterPrefab;

        [Header("Prefab Emotion Mappings")]
        public List<EmotionPrefabMapping> EmotionMappings = new List<EmotionPrefabMapping>();

        private GameObject _instantiatedPrefab;

        public override void Initialize()
        {
            if (CharacterPrefab == null)
            {
                Debug.LogWarning("No prefab assigned for prefab representation.");
                return;
            }
            _instantiatedPrefab = Instantiate(CharacterPrefab);
            _instantiatedPrefab.name = CharacterPrefab.name;
        }

        public override void SetEmotion(string emotionID)
        {
            if (_instantiatedPrefab == null)
            {
                Debug.LogWarning("Prefab has not been instantiated. Call Initialize() first.");
                return;
            }
            var mapping = EmotionMappings.Find(m => m.EmotionID == emotionID);
            if (mapping != null)
            {
                mapping.Apply(_instantiatedPrefab);
            }
            else
            {
                Debug.LogWarning($"No emotion mapping found for emotion ID '{emotionID}'.");
            }
        }

        public override void Show()
        {
            if (_instantiatedPrefab != null)
            {
                _instantiatedPrefab.SetActive(true);
            }
        }

        public override void Hide()
        {
            if (_instantiatedPrefab != null)
            {
                _instantiatedPrefab.SetActive(false);
            }
        }

        public override void Dispose()
        {
            if (_instantiatedPrefab != null)
            {
                Destroy(_instantiatedPrefab);
                _instantiatedPrefab = null;
            }
        }
    }

    [System.Serializable]
    public class EmotionPrefabMapping
    {
        public string EmotionID;
        public AnimatorOverrideController AnimatorOverride;

        public void Apply(GameObject instance)
        {
            var animator = instance.GetComponent<Animator>();
            if (animator != null && AnimatorOverride != null)
            {
                animator.runtimeAnimatorController = AnimatorOverride;
            }
        }
    }

}