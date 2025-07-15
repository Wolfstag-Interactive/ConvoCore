using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public interface ICharacterRepresentation
    {
        // Called once to initialize the representation.
        void Initialize();

        // Called to update the character's appearance (for example, changing an emotion).
        void SetEmotion(string emotionID);
    
        void Show();
        void Hide();
        void Dispose();
        /// <summary>
        /// Processes the given emotion and returns UI-relevant data (e.g., a sprite or GameObject).
        /// Allows each character representation to define its own output.
        /// </summary>
        /// <param name="emotionID">The emotion to process.</param>
        /// <returns>Object related to the current representation, e.g., Sprite, GameObject, etc.</returns>
        public abstract object ProcessEmotion(string emotionID);

    }

    public abstract class CharacterRepresentationBase : ScriptableObject, ICharacterRepresentation,IEditorPreviewableRepresentation
    {
        public abstract void Initialize();
        public abstract void SetEmotion(string emotionID);
        public abstract void Show();
        public abstract void Hide();
        public abstract void Dispose();
        /// <summary>
        /// Processes the given emotion and returns UI-relevant data (e.g., a sprite or GameObject).
        /// Allows each character representation to define its own output.
        /// </summary>
        /// <param name="emotionID">The emotion to process.</param>
        /// <returns>Object related to the current representation, e.g., Sprite, GameObject, etc.</returns>
        public abstract object ProcessEmotion(string emotionID);

        public abstract void DrawInlineEditorPreview(object emotionMapping, Rect position);

        public abstract float GetPreviewHeight();
    }

}