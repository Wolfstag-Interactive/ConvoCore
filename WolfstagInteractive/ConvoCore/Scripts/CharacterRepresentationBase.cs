using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/interface_wolfstaginteractive_1_1convocore_idialoguelineeditorcustomizable.html")]

    public abstract class CharacterRepresentationBase : ScriptableObject,
        IEditorPreviewableRepresentation
    {

        /// <summary>
        /// Processes the given emotion and returns UI-relevant data (e.g., a sprite or GameObject).
        /// Allows each character representation to define its own output.
        /// </summary>
        /// <param name="emotionID">The emotion to process.</param>
        /// <returns>Object related to the current representation, e.g., Sprite, GameObject, etc.</returns>
        public abstract object ProcessEmotion(string emotionID);
        
        // Abstract or virtual method to get the emotion IDs for the representation
        public abstract List<string> GetEmotionIDs();
        /// <summary>
        /// Retrieves the emotion mapping object by its GUID.
        /// Used by the editor to display the correct emotion in previews.
        /// </summary>
        /// <param name="emotionGuid">The GUID of the emotion to retrieve.</param>
        /// <returns>The emotion mapping object, or null if not found.</returns>
        public abstract object GetEmotionMappingByGuid(string emotionGuid);
        
        public abstract void DrawInlineEditorPreview(object emotionMapping, Rect position);

        public abstract float GetPreviewHeight();
    }
    
    /// <summary>
    /// Optional interface for character representations that require manual initialization.
    /// </summary>
    public interface IConvoCoreRepresentationInitializable
    {
        void Initialize();
    }
}