using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1CharacterRepresentationBase.html")]

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
        /// <summary>
        /// Apply an expression for this representation.
        /// Implementations are expected to run any attached BaseExpressionAction.
        /// </summary>
        public abstract void ApplyExpression(
            string expressionId,
            ConvoCore runtime,
            ConvoCoreConversationData conversation,
            int lineIndex,
            IConvoCoreCharacterDisplay display);
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