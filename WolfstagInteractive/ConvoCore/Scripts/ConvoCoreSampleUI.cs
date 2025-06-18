using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WolfstagInteractive.ConvoCore
{
    public class ConvoCoreSampleUI : ConvoCoreUIFoundation
    {
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI speakerName;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image SpeakerPotraitImage;
        [SerializeField] private Button continueButton;
        private bool continuePressed = false;

        private void Awake()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false); // Hide the panel initially
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonPressed);
            }
            DontDestroyOnLoad(this.gameObject);
        }
        /// <summary>
        /// Displays the dialogue line on the UI.
        /// </summary>
        /// <param name="text">The dialogue text to display.</param>
        private void DisplayDialogue(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true); // Show the panel
            }
        }
        /// <summary>
        /// Hides the dialogue UI.
        /// </summary>
        private void HideDialogue()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false); // Hide the panel
            }
        }
        private void SetDialogue(string lineDialogue,string lineSpeakerName, Sprite portrait)
        {
            DisplayDialogue(lineDialogue);
            if (speakerName != null)
            {
                speakerName.text = lineSpeakerName;
            }
            SpeakerPotraitImage.sprite = portrait;
        }

        public void RefreshUI(string language)
        {
            DisplayDialogue(language);
        }

        public override void UpdateForLanguageChange(string language)
        {
            RefreshUI(language);
        }

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
            continuePressed = false;

            // If a continue button is available, we rely on its OnClick callback.
            if (continueButton != null)
            {
                // ensure the button is visible/enabled.
                if (ConvoCoreInstance.CurrentDialogueState != ConversationState.Ended)
                {
                    continueButton.gameObject.SetActive(true);
                }

                // Wait until the user clicks the button.
                yield return new WaitUntil(() => continuePressed);

                // Optionally, hide or disable the button after input.
                continueButton.gameObject.SetActive(false);
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
            continuePressed = true;
        }


        public override void Dispose()
        {
            HideDialogue();
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueButtonPressed);
            }
        }
    }
}