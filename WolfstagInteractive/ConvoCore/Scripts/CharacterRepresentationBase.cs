using System.Collections.Generic;
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
        // Abstract or virtual method to get the emotion IDs for the representation
        public abstract List<string> GetEmotionIDs();



    }
    /// <summary>
    /// Interface for character representations that want to provide custom editor UI for per-line display options
    /// </summary>
    public interface IDialogueLineEditorCustomizable
    {
        /// <summary>
        /// Draws custom editor fields for per-line display options
        /// </summary>
        /// <param name="rect">The rect to draw in</param>
        /// <param name="emotionID">The selected emotion ID</param>
        /// <param name="displayOptionsProperty">SerializedProperty for the LineSpecificDisplayOptions</param>
        /// <param name="spacing">Spacing between elements</param>
        /// <returns>The updated rect after drawing</returns>
        Rect DrawDialogueLineOptions(Rect rect, string emotionID, UnityEditor.SerializedProperty displayOptionsProperty, float spacing);
        
        /// <summary>
        /// Gets the height needed for the custom dialogue line options
        /// </summary>
        /// <param name="emotionID">The selected emotion ID</param>
        /// <param name="displayOptionsProperty">SerializedProperty for the LineSpecificDisplayOptions</param>
        /// <returns>Height in pixels</returns>
        float GetDialogueLineOptionsHeight(string emotionID, UnityEditor.SerializedProperty displayOptionsProperty);
    }

    public abstract class CharacterRepresentationBase : ScriptableObject, ICharacterRepresentation,
        IEditorPreviewableRepresentation
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
        
        // Abstract or virtual method to get the emotion IDs for the representation
        public abstract List<string> GetEmotionIDs();


        public abstract void DrawInlineEditorPreview(object emotionMapping, Rect position);

        public abstract float GetPreviewHeight();
    }

}