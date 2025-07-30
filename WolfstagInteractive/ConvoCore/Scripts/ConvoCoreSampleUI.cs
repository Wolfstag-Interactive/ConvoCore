using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace WolfstagInteractive.ConvoCore
{
    public class ConvoCoreSampleUI : ConvoCoreUIFoundation
    {
        [Header("Dialogue UI Elements")] [SerializeField]
        private TextMeshProUGUI DialogueText;

        [SerializeField] private TextMeshProUGUI SpeakerName;
        [SerializeField] private GameObject DialoguePanel;
        [SerializeField] private Image SpeakerPortraitImage;
        [SerializeField] private Image FullBodyImageLeft;
        [SerializeField] private Image FullBodyImageRight;
        [SerializeField] private Button ContinueButton;
        private bool _continuePressed = false;
        private bool isWaitingForInput = false;
        [Header("Settings")] 
        [SerializeField] private bool AllowLineAdvanceOutsideButton = false;
        [Header("Input Settings")]
        [SerializeField]
        private InputAction AdvanceDialogueAction;
        
        private void Awake()
        {
            if (DialoguePanel != null)
            {
                HideDialogue();
            }

            if (ContinueButton != null)
            {
                ContinueButton.onClick.AddListener(OnContinueButtonPressed);
            }
            if (AdvanceDialogueAction != null)
            {
                AdvanceDialogueAction.Enable();
            }

            DontDestroyOnLoad(gameObject);
        }
        private void OnEnable()
        {
            // Enable and bind AdvanceDialogueAction
            if (AdvanceDialogueAction != null)
            {
                AdvanceDialogueAction.Enable();
                AdvanceDialogueAction.performed += OnAdvanceDialoguePerformed;
            }
            else
            {
                Debug.LogError("AdvanceDialogueAction is not assigned!");
            }
        }
        private void OnDisable()
        {
            if (AdvanceDialogueAction != null)
            {
                AdvanceDialogueAction.Disable();
                AdvanceDialogueAction.performed -= OnAdvanceDialoguePerformed;
            }
        }

        /// <summary>
        /// Listener for the AdvanceDialogueAction input event (keyboard or click).
        /// </summary>
        /// <param name="context">Input action callback context.</param>
        private void OnAdvanceDialoguePerformed(InputAction.CallbackContext context)
        {
            if (isWaitingForInput && (AllowLineAdvanceOutsideButton || !IsPointerOverUIElement(ContinueButton)))
            {
                OnContinueButtonPressed();
            }
        }

        /// <summary>
        /// Updates the dialogue UI based on the provided dialogue line information.
        /// </summary>
        /// <param name="dialogueLineInfo">Dialogue line metadata.</param>
        /// <param name="localizedText">The localized dialogue text to display.</param>
        /// <param name="speakingCharacterName">The name of the speaking character.</param>
        /// <param name="emotionMappingData">The emotion mapping object output by ProcessEmotion().</param>
      /// <summary>
/// Updates the dialogue UI based on the provided dialogue line information.
/// </summary>
/// <param name="dialogueLineInfo">Dialogue line metadata.</param>
/// <param name="localizedText">The localized dialogue text to display.</param>
/// <param name="speakingCharacterName">The name of the speaking character.</param>
/// <param name="emotionMappingData">The emotion mapping object output by ProcessEmotion().</param>
public override void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo dialogueLineInfo,
    string localizedText, string speakingCharacterName, CharacterRepresentationBase emotionMappingData)
{
    DisplayDialogue(localizedText);
    SpeakerName.text = speakingCharacterName;

    // Clear visuals by default
    if (SpeakerPortraitImage != null) SpeakerPortraitImage.sprite = null;
    if (FullBodyImageLeft != null) FullBodyImageLeft.sprite = null;
    if (FullBodyImageRight != null) FullBodyImageRight.sprite = null;

    // Hide all images initially
    if (SpeakerPortraitImage != null) SpeakerPortraitImage.gameObject.SetActive(false);
    if (FullBodyImageLeft != null) FullBodyImageLeft.gameObject.SetActive(false);
    if (FullBodyImageRight != null) FullBodyImageRight.gameObject.SetActive(false);

    // Get conversation data to resolve character profiles
    var conversationData = FindObjectOfType<ConvoCore>()?.ConversationData;
    if (conversationData == null)
    {
        Debug.LogError("Could not find ConversationData to resolve character representations");
        return;
    }

    // Render primary character representation (speaker)
    RenderCharacterRepresentation(
        conversationData, 
        dialogueLineInfo.PrimaryCharacterRepresentation, 
        SpeakerPortraitImage, 
        FullBodyImageLeft);

    // Render secondary character representation
    RenderCharacterRepresentation(
        conversationData,
        dialogueLineInfo.SecondaryCharacterRepresentation, 
        null, // No portrait for secondary
        FullBodyImageRight);

    // Render tertiary character representation (this might overlap with secondary - handle as needed)
    RenderCharacterRepresentation(
        conversationData,
        dialogueLineInfo.TertiaryCharacterRepresentation, 
        null, // No portrait for tertiary
        FullBodyImageRight); // You might want a separate image for tertiary

    // Show continue button
    ContinueButton.gameObject.SetActive(true);
}

void RenderCharacterRepresentation(
    ConvoCoreConversationData conversationData,
    ConvoCoreConversationData.CharacterRepresentationData representationData,
    Image portraitImage, 
    Image fullBodyImage)
{
    CharacterRepresentationBase representation = GetCharacterRepresentationFromData(conversationData, representationData);
    
    if (representation != null)
    {
        string emotionID = representationData.SelectedRepresentationEmotion;
        object emotionMappingObject = representation.ProcessEmotion(emotionID);

        if (emotionMappingObject is SpriteEmotionMapping spriteMapping)
        {
            // Extract the DisplayOptions for this emotion
            DialogueLineDisplayOptions options = spriteMapping.DisplayOptions;

            // Render portrait sprite
            if (spriteMapping.PortraitSprite != null && portraitImage != null)
            {
                portraitImage.sprite = spriteMapping.PortraitSprite;
                portraitImage.gameObject.SetActive(true);

                portraitImage.rectTransform.localScale = new Vector3(
                    options.FlipPortraitX ? -options.PortraitScale.x : options.PortraitScale.x,
                    options.FlipPortraitY ? -options.PortraitScale.y : options.PortraitScale.y,
                    options.PortraitScale.z
                );
            }

            // Render full-body sprite
            if (spriteMapping.FullBodySprite != null && fullBodyImage != null)
            {
                fullBodyImage.sprite = spriteMapping.FullBodySprite;
                fullBodyImage.gameObject.SetActive(true);

                fullBodyImage.rectTransform.localScale = new Vector3(
                    options.FlipFullBodyX ? -options.FullBodyScale.x : options.FullBodyScale.x,
                    options.FlipFullBodyY ? -options.FullBodyScale.y : options.FullBodyScale.y,
                    options.FullBodyScale.z
                );
            }
        }
    }
}

/// <summary>
/// Helper method to resolve character representation from representation data
/// </summary>
private CharacterRepresentationBase GetCharacterRepresentationFromData(
    ConvoCoreConversationData conversationData, 
    ConvoCoreConversationData.CharacterRepresentationData representationData)
{
    // Check if this is using the new secondary/tertiary system (has SelectedCharacterID)
    if (!string.IsNullOrEmpty(representationData.SelectedCharacterID))
    {
        // Find the profile by the selected character ID
        var selectedProfile = conversationData.ConversationParticipantProfiles
            .FirstOrDefault(p => p != null && p.CharacterID == representationData.SelectedCharacterID);
        
        if (selectedProfile != null && !string.IsNullOrEmpty(representationData.SelectedRepresentationName))
        {
            return selectedProfile.GetRepresentation(representationData.SelectedRepresentationName);
        }
    }
    else
    {
        // Use the old system - SelectedRepresentation directly
        return representationData.SelectedRepresentation;
    }
    
    return null;
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
        public override void HideDialogue()
        {
            DialogueText.gameObject.SetActive(false);
            SpeakerName.gameObject.SetActive(false);
            SpeakerPortraitImage.gameObject.SetActive(false);

            if (FullBodyImageLeft != null)
            {
                FullBodyImageLeft.gameObject.SetActive(false);
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
            ContinueButton.gameObject.SetActive(true);

            // Wait for user input (via key press or click)
            while (isWaitingForInput)
            {
                yield return null;
            }

            ContinueButton.gameObject.SetActive(false); // Hide the button when input is received
        }



        /// <summary>
        /// Called when the "Continue" button is pressed by the user.
        /// </summary>
        public void OnContinueButtonPressed()
        {
            if (!isWaitingForInput) return; // Ensure this is only executed while waiting for input

            isWaitingForInput = false; 
        }
        /// <summary>
        /// Determines if the pointer is currently over the specified UI element.
        /// </summary>
        /// <param name="uiElement">The UI element to check.</param>
        /// <returns>True if the pointer is over the element; false otherwise.</returns>
        private bool IsPointerOverUIElement(Button uiElement)
        {
            if (uiElement == null) return false;

            RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 localMousePosition = rectTransform.InverseTransformPoint(Mouse.current.position.ReadValue());
                return rectTransform.rect.Contains(localMousePosition);
            }
            return false; // Pointer is not over the UI element
        }


        private void OnDestroy()
        {
        #if ENABLE_INPUT_SYSTEM
        if (AdvanceDialogueAction != null)
        {
            AdvanceDialogueAction.Disable();
        }
        #endif
        }

    }

}