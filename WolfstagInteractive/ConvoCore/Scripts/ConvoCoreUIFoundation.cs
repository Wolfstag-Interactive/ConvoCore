using System.Collections;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Used as a basis of any UI that can be assigned in the inspector that includes all the base functions needed
    /// to interoperate with the dialogue state machine
    /// </summary>
    public class ConvoCoreUIFoundation : MonoBehaviour, IUIFoundation
    {
        protected ConvoCore ConvoCoreInstance;
        public virtual void InitializeUI(ConvoCore convoCoreInstance)
        {
            ConvoCoreInstance = convoCoreInstance;
        }
        public virtual void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo dialogueLineInfo, 
            string localizedText, string speakingCharacterName, Sprite portrait)
        {
        }
        public virtual void UpdateForLanguageChange(string newLanguage)
        {
        }

        public virtual IEnumerator WaitForUserInput()
        {
            yield return null;
        }

        public virtual void Dispose()
        {
        }
    }
}