using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "ConvoCore/CharacterProfile")]
    public class ConvoCoreCharacterProfileBaseData : ScriptableObject
    {
        /// <summary>
        /// Is this character profile the player character
        /// </summary>
        public bool IsPlayerCharacter;
        /// <summary>
        /// The Characters Name used if they are not the player
        /// </summary>
        public string CharacterName;
        /// <summary>
        /// Allows setting the placeholder player name. This is used as an indentifier within the dialogue to replace with the name set in the editor before final presentation of the dialogue line. 
        /// </summary>
        public string PlayerPlaceholder;

        public Sprite DefaultPortrait;
        public string CharacterID;

        [Header("Portraits Per Emotion")]
        public List<ConvoCoreCharacterEmotion> CharacterEmotions = new List<ConvoCoreCharacterEmotion>();
        [Header("Alternate Representations")]
        public List<CharacterRepresentation> AlternateRepresentations = new List<CharacterRepresentation>();

        // Fetch correct portrait for specific emotion and representation context
        public Sprite GetEmotionForRepresentation(string emotionName, string representationId)
        {
            // Handle cases where no specific representation is provided (use the default)
            if (string.IsNullOrEmpty(representationId))
            {
                // Default representation: find and return the emotion sprite
                var defaultEmotion = CharacterEmotions.FirstOrDefault(e => e.emotionName == emotionName);
                if (defaultEmotion == null)
                {
                    Debug.LogWarning(
                        $"Emotion '{emotionName}' not found for character '{CharacterName}'. Using default portrait.");
                    return DefaultPortrait;
                }
                return defaultEmotion.CharacterEmotionSprite; // Return default emotion sprite
            }
            // Alternate representation: check for representation-specific override
            var representation =
                AlternateRepresentations.FirstOrDefault(rep => rep.RepresentationID == representationId);
            if (representation != null)
            {
                // If no specific emotion sprite found, fallback to the representation-level portrait
                if (representation.AliasPortrait != null)
                {
                    return representation.AliasPortrait;
                }
                // Try to find an alternate emotion tied to this representation
                foreach (var emotion in CharacterEmotions)
                {
                    if (emotion.emotionName == emotionName)
                    {
                        var specificEmotionPortrait = emotion.GetEmotionSpriteForRepresentation(representationId);
                        Debug.Log(specificEmotionPortrait != null
                            ? $"Found specific emotion sprite for representation '{representationId}'."
                            : $"No specific sprite found for representation '{representationId}', falling back.");
                        if (specificEmotionPortrait != null) return specificEmotionPortrait;
                    }
                }

               
            }
            // Fallback: No representation-specific emotion or alternate found, return default emotion
            var fallbackEmotion = CharacterEmotions.FirstOrDefault(e => e.emotionName == emotionName);
            if (fallbackEmotion == null)
            {
                Debug.LogWarning(
                    $"Emotion '{emotionName}' not found for character '{CharacterName}'. Using default portrait.");
                return DefaultPortrait;
            }
            return fallbackEmotion.CharacterEmotionSprite;
        }
        // Fetch the name, considering which representation is active
        public string GetNameForRepresentation(string representationId)
        {
            var representation =
                AlternateRepresentations.FirstOrDefault(rep => rep.RepresentationID == representationId);
            return representation?.AliasName ?? CharacterName;
        }
    }
    [System.Serializable]
    public class CharacterRepresentation
    {
        public string AliasName; // Alternate name for the character (e.g., "???", "Stranger")
        public Sprite AliasPortrait; // Alternate sprite/portrait for the character
        public string RepresentationID; // Optional identifier for this representation (e.g., "hidden", "discovered")
    }
}