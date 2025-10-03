using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [System.Serializable]
    public class DialogueLineDisplayOptions
    {
        [Tooltip("Flip the display of the portrait sprite horizontally.")]
        public bool FlipPortraitX = false;

        [Tooltip("Flip the display of the portrait sprite vertically.")]
        public bool FlipPortraitY = false;

        [Tooltip("Flip the display of the full-body sprite horizontally.")]
        public bool FlipFullBodyX = false;

        [Tooltip("Flip the display of the full-body sprite vertically.")]
        public bool FlipFullBodyY = false;

        [Tooltip("Position of the character (Left or Right side of the dialogue box).")]
        public CharacterPosition DisplayPosition = CharacterPosition.Left;

        [Tooltip("Additional scale applied to the portrait sprite.")]
        public Vector3 PortraitScale = Vector3.one;

        [Tooltip("Additional scale applied to the full-body sprite.")]
        public Vector3 FullBodyScale = Vector3.one;

        public enum CharacterPosition
        {
            Left,Center,
            Right
        }
        
        public DisplaySide FullBodySide =>
            DisplayPosition == CharacterPosition.Right ? DisplaySide.Right : DisplaySide.Left;
        
    }

    /// <summary>
    /// Shared enum for prefab and UI side positioning
    /// </summary>
    public enum DisplaySide
    {
        Left,
        Right,
        Center
    }
}