using System;
using UnityEngine;

namespace CyberRift
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
        /// <param name="dialogueLine">Details of the current dialogue line.</param>
        /// <param name="localizedText">The current localized text to be displayed</param>
        /// <param name="speakingCharacterName">The name of the speaking character</param>
        /// <param name="portrait">The speaking characters portrait</param>
        public void UpdateDialogueUI(ConvoCoreCharacterConversationObject.DialogueLines dialogueLine, string localizedText, 
            string speakingCharacterName, Sprite portrait);

        /// <summary>
        /// Updates the UI for the selected language.
        /// </summary>
        /// <param name="newLanguage">The new language code.</param>
        public void UpdateForLanguageChange(string newLanguage);

        /// <summary>
        /// Handles cleanup when the UI builder is no longer needed.
        /// </summary>
        void Dispose();

    }
}