using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "SingleSpriteRepresentation", menuName = "ConvoCore/Sprite Representation")]
    public class SingleSpriteCharacterRepresentationData : CharacterRepresentationBase
    {
        [Header("Emotion Mappings")]
        [Tooltip("List of emotion mappings that pair an emotion ID with a portrait and full body sprite.")]
        public List<EmotionMapping> EmotionMappings = new List<EmotionMapping>();

        // Override runtime methods with empty implementations
        public override void Initialize()
        {
        }

        public override void SetEmotion(string emotionID)
        {
        }

        public override void Show()
        {
        }

        public override void Hide()
        {
        }

        public override void Dispose()
        {
        }

        public override object ProcessEmotion(string emotionID)
        {
            if (string.IsNullOrEmpty(emotionID))
            {
                Debug.LogWarning(
                    "No emotion ID provided for sprite-based representation. Defaulting to first available mapping.");
            }

            // Attempt to find the emotion mapping
            var mapping = EmotionMappings.FirstOrDefault(emotion => emotion.EmotionID == emotionID);

            if (mapping != null)
            {
                return mapping; // Return the found mapping directly
            }

            // Fallback to the first available mapping
            if (EmotionMappings.Count > 0)
            {
                var defaultMapping = EmotionMappings[0];
                /*Debug.LogWarning(
                    $"Emotion ID '{emotionID}' not found. Using first available emotion '{defaultMapping.EmotionID}'.");*/
                return defaultMapping;
            }

            // If no mappings exist, return null
            Debug.LogWarning("No emotion mappings available in sprite-based representation.");
            return null;
        }
#if UNITY_EDITOR
        /// <summary>
        /// Calculates the height required to display an inline preview.
        /// </summary>
        public override float GetPreviewHeight()
        {
            const float dropdownHeight = 18f; // Height of the foldout dropdown
            const float spriteHeight = 64f;   // Height of each sprite preview
            const float spacing = 5f;         // Spacing between elements

            // If no emotion mappings exist, return only the dropdown height
            if (EmotionMappings.Count == 0)
            {
                return dropdownHeight;
            }

            // Calculate height based on foldout state
            if (FoldoutStates.TryGetValue(this, out bool foldoutState) && foldoutState)
            {
                // Expanded: Include dropdown, both sprites, and spacing
                var mapping = EmotionMappings[0];
                float totalHeight = dropdownHeight; // Add dropdown height first

                if (mapping.PortraitSprite != null)
                {
                    totalHeight += spriteHeight + spacing;
                }

                if (mapping.FullBodySprite != null)
                {
                    totalHeight += spriteHeight + spacing;
                }

                // Subtract extra spacing after the last element
                return totalHeight - spacing;
            }

            // Collapsed: Only include the dropdown height
            return dropdownHeight;

        }

        /// <summary>
        /// Draws an inline editor preview of the representation.
        /// </summary>
        public override void DrawInlineEditorPreview(object mappingData, Rect position)
        {
            // Cast the generic mapping data to EmotionMapping
            var emotionMapping = mappingData as EmotionMapping;
            if (emotionMapping == null)
            {
                EditorGUI.LabelField(position, "Invalid or null mapping for preview.");
                return;
            }

            // Constants for sizes and spacing
            float buttonHeight = EditorGUIUtility.singleLineHeight;
            const float spriteWidth = 64f;    // Width of each sprite preview
            const float spriteHeight = 64f;   // Height of each sprite preview
            const float spacing = 5f;         // Spacing between sprites
            // Ensure foldout state is tracked with a stable object reference
            if (!FoldoutStates.TryGetValue(this, out _))
            {
                FoldoutStates[this] = false; // Default to collapsed
            }
            // Draw the toggle button
            Rect buttonRect = new Rect(position.x, position.y, position.width, buttonHeight);
            if (GUI.Button(buttonRect, FoldoutStates[this] ? "Hide Sprites" : "Show Sprites"))
            {
                // Toggle the visibility state
                FoldoutStates[this] = !FoldoutStates[this];
            }


            // If the foldout is collapsed, skip rendering further content
            if (!FoldoutStates[this])
            {
                return;
            }

            // Adjust the Y position after the foldout dropdown
            float currentY = position.y + buttonHeight + spacing;

            // Draw the portrait sprite (if available)
            if (emotionMapping.PortraitSprite != null)
            {
                Rect portraitRect = new Rect(position.x, currentY, spriteWidth, spriteHeight);
                EditorGUI.DrawPreviewTexture(
                    portraitRect,
                    emotionMapping.PortraitSprite.texture,
                    null,
                    ScaleMode.ScaleToFit
                );

                // Move current Y position down by the sprite's height and spacing
                currentY += spriteHeight + spacing;
            }

            // Draw the full body sprite (if available)
            if (emotionMapping.FullBodySprite != null)
            {
                Rect fullBodyRect = new Rect(position.x, currentY, spriteWidth, spriteHeight);
                EditorGUI.DrawPreviewTexture(
                    fullBodyRect,
                    emotionMapping.FullBodySprite.texture,
                    null,
                    ScaleMode.ScaleToFit
                );
            }

        }

        /// <summary>
        /// Cache for foldout states based on objects being inspected.
        /// Prevents foldout states from resetting across UI refreshes.
        /// </summary>
        private static readonly Dictionary<object, bool> FoldoutStates = new Dictionary<object, bool>();
#endif

    }
    [System.Serializable]
    public class EmotionMapping
    {
        [Tooltip("The unique identifier for the emotion (e.g., 'happy', 'sad').")]
        public string EmotionID;
    
        [Tooltip("Portrait sprite for the emotion.")]
        public Sprite PortraitSprite;
    
        [Tooltip("Full body sprite for the emotion.")]
        public Sprite FullBodySprite;
    }
}