using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Used as a basis of any UI that can be assigned in the inspector that includes all the base functions needed
    /// to interoperate with the dialogue state machine.
    /// Representation-agnostic: subclasses are responsible for obtaining and using any
    /// <see cref="ConvoCorePrefabRepresentationSpawner"/> they need.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreUIFoundation.html")]
    public class ConvoCoreUIFoundation : MonoBehaviour, IUIFoundation
    {
        /// <summary>
        /// A named slot entry that maps a display name to a scene object reference.
        /// Populated on the UI Foundation prefab to define all slots available for character placement.
        /// </summary>
        [Serializable]
        public class DisplaySlotDefinition
        {
            [Tooltip("The name shown in the inspector dropdown for this slot.")]
            public string SlotName;
            [Tooltip("The GameObject in the canvas hierarchy that represents this slot.")]
            public GameObject SlotObject;
        }

        [Tooltip("Named display slots available for character placement. " +
                 "These populate the Display Slot dropdown in the dialogue line inspector.")]
        [SerializeField] private List<DisplaySlotDefinition> _displaySlots = new();
        public IReadOnlyList<DisplaySlotDefinition> DisplaySlots => _displaySlots;

        protected ConvoCore ConvoCoreInstance;
        protected ConvoCoreDialogueHistoryUI ConvoCoreDialogueHistoryUI;

        public event Action RequestAdvance;
        public event Action RequestReverse;

        protected void RaiseAdvance() => RequestAdvance?.Invoke();
        protected void RaiseReverse() => RequestReverse?.Invoke();

        public virtual void InitializeUI(ConvoCore convoCoreInstance)
        {
            ConvoCoreInstance = convoCoreInstance;

            ConvoCoreDialogueHistoryUI = !TryGetComponent(out ConvoCoreDialogueHistoryUI historyUI)
                ? gameObject.AddComponent<ConvoCoreDialogueHistoryUI>()
                : historyUI;
        }

        public virtual void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo dialogueLineInfo,
            string localizedText, string speakingCharacterName,
            CharacterRepresentationBase expressionMappingData, ConvoCoreCharacterProfileBaseData primaryProfile)
        {
        }

        /// <summary>
        /// Updates the UI when the language changes, primarily to replace the current dialogue text.
        /// </summary>
        public virtual void UpdateForLanguageChange(string localizedDialogueText, string newLanguageCode)
        {
        }

        public virtual IEnumerator WaitForUserInput()
        {
            yield return null;
        }

        /// <summary>
        /// Present a set of player choices and wait for the player to select one.
        /// The base implementation auto-selects index 0 so conversations never hang
        /// if no choice UI has been implemented.
        /// </summary>
        public virtual IEnumerator PresentChoices(
            List<ConvoCoreConversationData.ChoiceOption> options,
            List<string> localizedLabels,
            ChoiceResult result)
        {
            result.SelectedIndex = 0;
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