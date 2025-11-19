using System;
using System.Collections;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public interface IUIFoundation 
    {
        /// <summary>
        /// Initializes the UI builder and sets up required bindings.
        /// </summary>
        /// <param name="convoCoreInstance">The ConvoCore instance.</param>
        void InitializeUI(ConvoCore convoCoreInstance);

        /// <summary>
        /// Updates the displayed UI elements based on the ConvoCore instances current state.
        /// </summary>
        /// <param name="dialogueLineInfo">Details of the current dialogue line.</param>
        /// <param name="localizedText">The current localized text to be displayed</param>
        /// <param name="speakingCharacterName">The name of the speaking character</param>
        /// <param name="expressionMappingData">The speaking characters portrait</param>
        /// <param name="primaryProfile">The primary profile data</param>
        public void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo dialogueLineInfo, string localizedText,
            string speakingCharacterName, CharacterRepresentationBase expressionMappingData, ConvoCoreCharacterProfileBaseData primaryProfile);

        /// <summary>
        /// Updates the UI when language changes, primarily to replace the current dialogue text
        /// </summary>
        /// <param name="localizedDialogueText">The new localized dialogue text to display</param>
        /// <param name="newLanguageCode">The new language code that was applied</param>
        public virtual void UpdateForLanguageChange(string localizedDialogueText, string newLanguageCode)
        {
        }


        /// <summary>
        /// Wait until the UI signals that the user has provided input.
        /// The actual waiting is delegated to the UI implementation.
        /// </summary>
        /// <returns></returns>
        public IEnumerator WaitForUserInput();

        /// <summary>
        /// Handles cleanup when the UI builder is no longer needed.
        /// </summary>
        void Dispose();

    }
}