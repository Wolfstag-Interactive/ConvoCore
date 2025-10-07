using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Marks a string field to be rendered as a popup of emotion IDs coming from a sibling
    /// SerializedProperty that references a CharacterRepresentationBase asset.
    /// Stores the GUID, shows DisplayName.
    /// </summary>
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1EmotionIDSelectorAttribute.html")]
[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public class EmotionIDSelectorAttribute : PropertyAttribute
    {
        public string RepresentationPropertyName { get; }
        public EmotionIDSelectorAttribute(string representationPropertyName)
        {
            RepresentationPropertyName = representationPropertyName;
        }
    }
}