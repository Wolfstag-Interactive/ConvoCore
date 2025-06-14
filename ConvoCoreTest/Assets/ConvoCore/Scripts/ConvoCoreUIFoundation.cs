using UnityEngine;

namespace CyberRift
{
    /// <summary>
    /// Used as a basis of any UI that can be assigned in the inspector that includes all the base functions needed
    /// to interoperate with the dialogue state machine
    /// </summary>
    public class ConvoCoreUIFoundation : MonoBehaviour, IUIFoundation
    {
        public virtual void InitializeUI(ConvoCore ConvoCoreInstance)
        {
        }
        public virtual void UpdateDialogueUI(ConvoCoreCharacterConversationObject.DialogueLines dialogueLine, 
            string localizedText, string speakingCharacterName, Sprite portrait)
        {
        }
        public virtual void UpdateForLanguageChange(string newLanguage)
        {
        }
        public virtual void Dispose()
        {
        }
    }
}