using System;
using System.Collections;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Used as a basis of any UI that can be assigned in the inspector that includes all the base functions needed
    /// to interoperate with the dialogue state machine
    /// </summary>
[HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreUIFoundation.html")]
    public class ConvoCoreUIFoundation : MonoBehaviour, IUIFoundation
    {
        protected ConvoCore ConvoCoreInstance;
        protected ConvoCorePrefabRepresentationSpawner PrefabRepresentationSpawner;
        protected ConvoCoreDialogueHistoryUI ConvoCoreDialogueHistoryUI;
        [SerializeField, Min(1)]
        private int maxVisibleCharacterSlots = 3;

        // Includes the speaker. 3 means speaker plus 2 companions.
        public virtual int MaxVisibleCharacterSlots => maxVisibleCharacterSlots;

        public event Action RequestAdvance;
        public event Action RequestReverse;

        protected void RaiseAdvance() => RequestAdvance?.Invoke();
        protected void RaiseReverse() => RequestReverse?.Invoke();

        public virtual void InitializeUI(ConvoCore convoCoreInstance)
        {
            ConvoCoreInstance = convoCoreInstance;
            PrefabRepresentationSpawner = !TryGetComponent(out ConvoCorePrefabRepresentationSpawner spawner) ? 
                gameObject.AddComponent<ConvoCorePrefabRepresentationSpawner>() : spawner;
            ConvoCoreDialogueHistoryUI = !TryGetComponent(out  ConvoCoreDialogueHistoryUI convoCoreDialogueHistoryUI) ? 
                gameObject.AddComponent<ConvoCoreDialogueHistoryUI>() : convoCoreDialogueHistoryUI;
        }

        public virtual void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo dialogueLineInfo,
            string localizedText, string speakingCharacterName,
            CharacterRepresentationBase expressionMappingData, ConvoCoreCharacterProfileBaseData primaryProfile)
        {
        }
        /// <summary>
        /// Updates the UI when the language changes, primarily to replace the current dialogue text
        /// </summary>
        /// <param name="localizedDialogueText">The new localized dialogue text to display</param>
        /// <param name="newLanguageCode">The new language code that was applied</param>
        public virtual void UpdateForLanguageChange(string localizedDialogueText, string newLanguageCode)
        {
        }


        public virtual IEnumerator WaitForUserInput()
        {
            yield return null;
        }

        public virtual void Dispose()
        {
        }

        public virtual void HideDialogue()
        {
        }
        
        public virtual void DisplayDialogue(string text)
        {
        }
    }
}