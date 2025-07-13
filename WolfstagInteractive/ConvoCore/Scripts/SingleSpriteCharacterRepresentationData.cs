using System.Collections.Generic;
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
        public override void Initialize() { }
        public override void SetEmotion(string emotionID) { }
        public override void Show() { }
        public override void Hide() { }
        public override void Dispose(){ }
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