using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1DialogueLineDisplayOptions.html")]
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

        [Tooltip("Position of the character (Left, Center, or Right). Used by canvas UI as slot fallback when no SlotId is set.")]
        public CharacterPosition DisplayPosition = CharacterPosition.Left;

        [Tooltip("Named slot identifier for canvas UI. When set, takes precedence over DisplayPosition for slot selection.")]
        public string SlotId;

        [Tooltip("Additional scale applied to the portrait sprite.")]
        public Vector3 PortraitScale = Vector3.one;

        [Tooltip("Additional scale applied to the full-body sprite.")]
        public Vector3 FullBodyScale = Vector3.one;

        public enum CharacterPosition
        {
            Left,
            Center,
            Right
        }
    }
}