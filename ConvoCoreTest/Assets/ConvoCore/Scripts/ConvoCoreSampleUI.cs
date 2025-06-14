using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CyberRift
{
    public class ConvoCoreSampleUI : ConvoCoreUIFoundation
    {
        [SerializeField] private SuperTextMesh dialogueText;
        [SerializeField] private SuperTextMesh speakerName;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image SpeakerPotraitImage;
        private void Awake()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false); // Hide the panel initially
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

        public override void UpdateDialogueUI(ConvoCoreCharacterConversationObject.DialogueLines dialogueLine,
            string localizedText, string speakingCharacterName, Sprite portrait)
        {
            SetDialogue(localizedText, speakingCharacterName, portrait);
        }

        public override void Dispose()
        {
            HideDialogue();
        }
    }
}