using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public interface IEditorPreviewableRepresentation
    {
        /// <summary>
        /// Draws a custom representation-specific section in the inspector.
        /// </summary>
        /// <param name="emotionMapping">The emotion mapping being inspected.</param>
        /// <param name="position">The rect bounds for drawing.</param>
        void DrawInlineEditorPreview(object emotionMapping, Rect position);

        /// <summary>
        /// Provides the height required for rendering the inline preview in the editor.
        /// </summary>
        /// <returns>Height required for preview rendering.</returns>
        float GetPreviewHeight();
    }
}