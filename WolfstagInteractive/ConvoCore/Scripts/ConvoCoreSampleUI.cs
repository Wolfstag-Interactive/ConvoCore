using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WolfstagInteractive.ConvoCore
{
   public class ConvoCoreSampleUI : ConvoCoreUIFoundation
{
    [Header("Dialogue UI Elements")]
    [SerializeField] private TextMeshProUGUI DialogueText;
    [SerializeField] private TextMeshProUGUI SpeakerName;
    [SerializeField] private GameObject DialoguePanel;
    [SerializeField] private Image SpeakerPortraitImage;
    [SerializeField] private Button ContinueButton;
    [SerializeField] private Image FullBodyImage;
    private bool _continuePressed = false;
    private bool isWaitingForInput = false;

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
    /// Updates the dialogue UI based on the provided dialogue line information.
    /// </summary>
    /// <param name="dialogueLineInfo">Dialogue line metadata.</param>
    /// <param name="localizedText">The localized dialogue text to display.</param>
    /// <param name="speakingCharacterName">The name of the speaking character.</param>
    /// <param name="emotionMappingObject">The emotion mapping object output by ProcessEmotion().</param>
    public override void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo dialogueLineInfo, string localizedText, string speakingCharacterName, object emotionMappingObject)
    {
        // Update dialogue text
        DialogueText.text = localizedText;
        
        // Update speaker name
        SpeakerName.text = speakingCharacterName;

        // Clear visuals by default
        SpeakerPortraitImage.sprite = null;
        FullBodyImage.sprite = null;

        // Handle emotion mapping output
        if (emotionMappingObject is EmotionMapping emotionMapping)
        {
            // Update the portrait sprite (if available)
            if (emotionMapping.PortraitSprite != null)
            {
                SpeakerPortraitImage.sprite = emotionMapping.PortraitSprite;
                SpeakerPortraitImage.gameObject.SetActive(true);
            }
            else
            {
                SpeakerPortraitImage.gameObject.SetActive(false);
            }

            // Optionally update the full-body sprite (if available)
            if (FullBodyImage != null && emotionMapping.FullBodySprite != null)
            {
                FullBodyImage.sprite = emotionMapping.FullBodySprite;
                FullBodyImage.gameObject.SetActive(true);
            }
            else if (FullBodyImage != null)
            {
                FullBodyImage.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("Emotion mapping object is null or invalid for UpdateDialogueUI.");
        }

        // Show continue button (and ensure it's active only when user input is needed)
        ContinueButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Displays a piece of dialogue.
    /// </summary>
    public void DisplayDialogue(string text)
    {
        DialogueText.text = text;
        DialogueText.gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the dialogue display.
    /// </summary>
    public void HideDialogue()
    {
        DialogueText.gameObject.SetActive(false);
        SpeakerName.gameObject.SetActive(false);
        SpeakerPortraitImage.gameObject.SetActive(false);

        if (FullBodyImage != null)
        {
            FullBodyImage.gameObject.SetActive(false);
        }

        ContinueButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Waits for user input (via a button press) to proceed with dialogue
    /// </summary>
    /// <returns>Coroutine to wait for user input</returns>
    public override IEnumerator WaitForUserInput()
    {
        isWaitingForInput = true;
        ContinueButton.gameObject.SetActive(true); // Show continue button to prompt user

        while (isWaitingForInput)
        {
            yield return null; // Wait until user presses the continue button
        }

        ContinueButton.gameObject.SetActive(false); // Hide the button when input is received
    }

    /// <summary>
    /// Called when the "Continue" button is pressed by the user.
    /// </summary>
    public void OnContinueButtonPressed()
    {
        isWaitingForInput = false; // Signal that input was received
    }
}

}