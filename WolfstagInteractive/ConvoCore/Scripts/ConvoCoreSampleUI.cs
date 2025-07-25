using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; 
#endif


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
        [Header("Settings")] [SerializeField] private bool AllowClickAnywhereToAdvanceLine = false;
        [Header("Input Settings")]
        [SerializeField] 
        private KeyCode AdvanceDialogueKey = KeyCode.Space;
        #if ENABLE_INPUT_SYSTEM
        [SerializeField]
        private InputAction AdvanceDialogueAction;
        #endif

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
            #if ENABLE_INPUT_SYSTEM
                // Enable the new input action for dialogue progression
                if (AdvanceDialogueAction != null)
                {
                    AdvanceDialogueAction.Enable();
                }
            #endif

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Updates the dialogue UI based on the provided dialogue line information.
        /// </summary>
        /// <param name="dialogueLineInfo">Dialogue line metadata.</param>
        /// <param name="localizedText">The localized dialogue text to display.</param>
        /// <param name="speakingCharacterName">The name of the speaking character.</param>
        /// <param name="representationObject">The emotion mapping object output by ProcessEmotion().</param>
        public override void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo dialogueLineInfo,
            string localizedText, string speakingCharacterName, object representationObject)
        {
            DisplayDialogue(localizedText);
            SpeakerName.text = speakingCharacterName;

            // Clear visuals by default
            SpeakerPortraitImage.sprite = null;
            FullBodyImageLeft.sprite = null;
            FullBodyImageRight.sprite = null;

            if (representationObject is CharacterRepresentationBase representation)
            {
                object emotionMappingObject =
                    representation.ProcessEmotion(dialogueLineInfo.SelectedRepresentationEmotion);

                if (emotionMappingObject is EmotionMapping spriteMapping)
                {
                    // Extract the DisplayOptions specific to this emotion
                    var options = spriteMapping.DisplayOptions;

                    // Render portrait sprite with emotion-specific display options
                    if (spriteMapping.PortraitSprite != null)
                    {
                        SpeakerPortraitImage.sprite = spriteMapping.PortraitSprite;
                        SpeakerPortraitImage.gameObject.SetActive(true);

                        SpeakerPortraitImage.rectTransform.localScale = new Vector3(
                            options.FlipPortraitX ? -options.PortraitScale.x : options.PortraitScale.x,
                            options.FlipPortraitY ? -options.PortraitScale.y : options.PortraitScale.y,
                            options.PortraitScale.z
                        );
                    }

                    // Render full-body sprite with emotion-specific display options
                    if (spriteMapping.FullBodySprite != null)
                    {
                        FullBodyImageLeft.sprite = spriteMapping.FullBodySprite;
                        FullBodyImageLeft.gameObject.SetActive(true);

                        if (spriteMapping.DisplayOptions.DisplayPosition ==
                            DialogueLineDisplayOptions.CharacterPosition.Left)
                        {
                            FullBodyImageRight.sprite = spriteMapping.FullBodySprite;
                            FullBodyImageRight.gameObject.SetActive(true);
                        }
                        else if(spriteMapping.DisplayOptions.DisplayPosition == 
                                DialogueLineDisplayOptions.CharacterPosition.Right)
                        {
                            FullBodyImageRight.sprite = spriteMapping.FullBodySprite;
                            FullBodyImageRight.gameObject.SetActive(true);
                        }
                        
                        FullBodyImageLeft.rectTransform.localScale = new Vector3(
                            options.FlipFullBodyX ? -options.FullBodyScale.x : options.FullBodyScale.x,
                            options.FlipFullBodyY ? -options.FullBodyScale.y : options.FullBodyScale.y,
                            options.FullBodyScale.z
                        );
                    }
                }
            }


            // Show continue button
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
            bool inputProcessed = false; // Flag to ensure input is processed only once

            ContinueButton.gameObject.SetActive(true);

            while (isWaitingForInput)
            {
                #if ENABLE_INPUT_SYSTEM
                // New Input System: Check for input actions
                if (AdvanceDialogueAction != null && AdvanceDialogueAction.triggered && !inputProcessed)
                {
                    inputProcessed = true;
                    OnContinueButtonPressed();
                }
                #else
                // Legacy Input System: Check KeyCode
                if (Input.GetKeyDown(AdvanceDialogueKey) && !inputProcessed)
                {
                    inputProcessed = true;
                    OnContinueButtonPressed();
                }
                #endif


                // Check if "click anywhere" feature is enabled
                if (AllowClickAnywhereToAdvanceLine && Input.GetMouseButtonDown(0) && !inputProcessed)
                {
                    // Check if the click is outside the ContinueButton
                    if (!IsPointerOverUIElement(ContinueButton))
                    {
                        inputProcessed = true; // Mark input as processed
                        OnContinueButtonPressed();
                    }
                }

                yield return null; // Wait until user presses a button or clicks
            }

            ContinueButton.gameObject.SetActive(false); // Hide the button when input is received
        }


        /// <summary>
        /// Called when the "Continue" button is pressed by the user.
        /// </summary>
        public void OnContinueButtonPressed()
        {
            if (!isWaitingForInput) return; // Ensure this is only executed while waiting for input

            isWaitingForInput = false; // Signal that input was received
        }
        /// <summary>
        /// Determines if the pointer is currently over the specified UI element.
        /// </summary>
        /// <param name="uiElement">The UI element to check.</param>
        /// <returns>True if the pointer is over the element; false otherwise.</returns>
        private bool IsPointerOverUIElement(Button uiElement)
        {
            // Perform a raycast to check if the pointer is over the UI element
            RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 localMousePosition = rectTransform.InverseTransformPoint(Input.mousePosition);
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