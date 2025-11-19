using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/interface_wolfstaginteractive_1_1convocore_idialoguelineeditorcustomizable.html")]

    public abstract class CharacterRepresentationBase : ScriptableObject,
        IEditorPreviewableRepresentation
    {

        /// <summary>
        /// Processes the given expression and returns UI-relevant data (e.g., a sprite or GameObject).
        /// Allows each character representation to define its own output.
        /// </summary>
        /// <param name="expressionID">The expression to process.</param>
        /// <returns>Object related to the current representation, e.g., Sprite, GameObject, etc.</returns>
        public abstract object ProcessExpression(string expressionID);
        
        // Abstract or virtual method to get the expression IDs for the representation
        public abstract List<string> GetExpressionIDs();
        /// <summary>
        /// Retrieves the expression mapping object by its GUID.
        /// Used by the editor to display the correct expression in previews.
        /// </summary>
        /// <param name="expressionGuid">The GUID of the expression to retrieve.</param>
        /// <returns>The expression mapping object, or null if not found.</returns>
        public abstract object GetExpressionMappingByGuid(string expressionGuid);
        
        public abstract void DrawInlineEditorPreview(object expressionMapping, Rect position);

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