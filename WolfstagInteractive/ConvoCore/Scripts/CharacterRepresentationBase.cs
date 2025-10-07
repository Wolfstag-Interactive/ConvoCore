using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    
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
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/interface_wolfstaginteractive_1_1convocore_idialoguelineeditorcustomizable.html")]

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