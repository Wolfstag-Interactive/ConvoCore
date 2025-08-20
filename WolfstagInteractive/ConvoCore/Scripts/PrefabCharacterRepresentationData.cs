using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "PrefabCharacterRepresentation",
        menuName = "ConvoCore/Prefab Character Representation")]
    public class PrefabCharacterRepresentationData : CharacterRepresentationBase
    {
        [Header("Prefab Settings")] public GameObject CharacterPrefab;

        [Header("Prefab Emotion Mappings")]
        public List<EmotionPrefabMapping> EmotionMappings = new List<EmotionPrefabMapping>();

        private GameObject _instantiatedPrefab;

        // Override to provide the list of emotion IDs
        public override List<string> GetEmotionIDs()
        {
            // Collect and return IDs from the emotion mappings
            return EmotionMappings.Select(mapping => mapping.EmotionID).ToList();
        }

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

        public override object ProcessEmotion(string emotionID)
        {
            if (_instantiatedPrefab == null)
            {
                Initialize(); // Ensure prefab is instantiated
            }

            // Attempt to find the emotion mapping
            var mapping = EmotionMappings.FirstOrDefault(emotion => emotion.EmotionID == emotionID);

            if (mapping != null)
            {
                // Apply the animation override or other settings to the prefab
                mapping.Apply(_instantiatedPrefab);
                return mapping; // Return the found mapping for further use
            }

            Debug.LogWarning($"Prefab Emotion ID '{emotionID}' not found. Returning null.");
            return null;
        }


#if UNITY_EDITOR
        /// <summary>
        /// Calculates the height required to display an inline prefab preview.
        /// </summary>
        public override float GetPreviewHeight()
        {
            // Define a standard preview height (in pixels) for prefabs
            const float prefabPreviewHeight = 100f;

            // If no prefab is assigned, return 0 (no preview height)
            if (CharacterPrefab == null) return 0f;

            // Return the defined preview height
            return prefabPreviewHeight;
        }
        /// <summary>
        /// Draws an inline editor preview for the prefab.
        /// </summary>
        public override void DrawInlineEditorPreview(object mappingData, Rect position)
        {
            // Cast the generic mapping data to EmotionPrefabMapping
            var prefabMapping = mappingData as EmotionPrefabMapping;
            if (CharacterPrefab == null || prefabMapping == null)
            {
                EditorGUI.LabelField(position, "Invalid or null prefab mapping for preview.");
                return;
            }

            // Render the prefab preview
            Texture2D previewTexture = AssetPreview.GetAssetPreview(CharacterPrefab);
            if (previewTexture != null)
            {
                GUI.DrawTexture(position, previewTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.LabelField(position, "Prefab preview not available.");
            }
        }
    }


#endif
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