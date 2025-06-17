using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "NewCharacterEmotion", menuName = "ConvoCore/Character/Emotion")]
    public class ConvoCoreCharacterEmotion : ScriptableObject
    {
        public string emotionName;
        public Sprite CharacterEmotionSprite;

        [Header("Override Emotion Sprites per Representation")]
        public List<RepresentationEmotionOverride> RepresentationOverrides;

        // Method to fetch emotion sprite by representation ID (or default if none exists)
        public Sprite GetEmotionSpriteForRepresentation(string representationId)
        {
            if (string.IsNullOrEmpty(representationId))
                return CharacterEmotionSprite;

            // Check if there is a specific override for this representation
            var overrideSprite = RepresentationOverrides.FirstOrDefault(r => 
                r.RepresentationID == representationId);
            return overrideSprite?.EmotionSprite ?? CharacterEmotionSprite;
        }

    }

    [System.Serializable]
    public class RepresentationEmotionOverride
    {
        public string RepresentationID; // The ID of the alternative representation
        public Sprite EmotionSprite; // Custom sprite for the emotion in this representation
    }
}