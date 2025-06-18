using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WolfstagInteractive.ConvoCore
{
    public class ConvoCoreSampleUI : ConvoCoreUIFoundation
    {
        [SerializeField] private TextMeshProUGUI DialogueText;
        [SerializeField] private TextMeshProUGUI SpeakerName;
        [SerializeField] private GameObject DialoguePanel;
        [SerializeField] private Image SpeakerPortraitImage;
        [SerializeField] private Button ContinueButton;
        private bool _continuePressed = false;

        private void Awake()
        {
            if (DialoguePanel != null)
            {
                DialoguePanel.SetActive(false); // Hide the panel initially
            }

            if (ContinueButton != null)
            {
                ContinueButton.onClick.AddListener(OnContinueButtonPressed);
            }
            DontDestroyOnLoad(gameObject);
        }
        /// <summary>
        /// Displays the dialogue line on the UI.
        /// </summary>
        /// <param name="text">The dialogue text to display.</param>
        private void DisplayDialogue(string text)
        {
            if (DialogueText != null)
            {
                DialogueText.text = text;
            }

            if (DialoguePanel != null)
            {
                DialoguePanel.SetActive(true); // Show the panel
            }
        }
        /// <summary>
        /// Hides the dialogue UI.
        /// </summary>
        private void HideDialogue()
        {
            if (DialoguePanel != null)
            {
                DialoguePanel.SetActive(false); // Hide the panel
            }
        }
        /// <summary>
        /// Set the dialogue info for the provided line
        /// </summary>
        /// <param name="lineDialogue"></param>
        /// <param name="lineSpeakerName"></param>
        /// <param name="portrait"></param>
        private void SetDialogue(string lineDialogue,string lineSpeakerName, Sprite portrait)
        {
            DisplayDialogue(lineDialogue);
            if (SpeakerName != null)
            {
                SpeakerName.text = lineSpeakerName;
            }
            SpeakerPortraitImage.sprite = portrait;
        }
        /// <summary>
        /// Refreshes the UI text with the provided language
        /// </summary>
        /// <param name="language"></param>
        private void RefreshUI(string language)
        {
            DisplayDialogue(language);
        }
        /// <summary>
        /// Update the current UI for the language
        /// </summary>
        /// <param name="language"></param>
        public override void UpdateForLanguageChange(string language)
        {
            RefreshUI(language);
        }
        /// <summary>
        /// Updates the UI with the current dialogue line.
        /// </summary>
        /// <param name="dialogueLine">The current dialogue line.</param>
        /// <param name="localizedText">The current localized text to be displayed</param>
        /// <param name="speakingCharacterName">The name of the speaking character</param>
        /// <param name="portrait">The speaking characters portrait</param>
        public override void UpdateDialogueUI(ConvoCoreConversationData.DialogueLines dialogueLine,
            string localizedText, string speakingCharacterName, Sprite portrait)
        {
            SetDialogue(localizedText, speakingCharacterName, portrait);
        }

        /// <summary>
        /// Waits for user input via a UI button if available or falls back to any key press.
        /// </summary>
        public override IEnumerator WaitForUserInput()
        {
            // Reset the input flag.
            _continuePressed = false;

            // If a continue button is available, we rely on its OnClick callback.
            if (ContinueButton != null)
            {
                // ensure the button is visible/enabled.
                if (ConvoCoreInstance.CurrentDialogueState != ConversationState.Ended)
                {
                    ContinueButton.gameObject.SetActive(true);
                }

                // Wait until the user clicks the button.
                yield return new WaitUntil(() => _continuePressed);

                // Optionally, hide or disable the button after input.
                ContinueButton.gameObject.SetActive(false);
            }
            else
            {
                // Fallback: wait until any key is pressed.
                yield return new WaitUntil(() => Input.anyKeyDown);
            }
        }

        /// <summary>
        /// Callback for when the continue button is pressed.
        /// </summary>
        private void OnContinueButtonPressed()
        {
            _continuePressed = true;
        }
        /// <summary>
        ///Hide Dialogue UI and unsubscribe
        /// </summary>
        public override void Dispose()
        {
            HideDialogue();
            if (ContinueButton != null)
            {
                ContinueButton.onClick.RemoveListener(OnContinueButtonPressed);
            }
        }
    }
}