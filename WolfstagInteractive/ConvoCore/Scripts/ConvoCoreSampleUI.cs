using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace WolfstagInteractive.ConvoCore
{
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreSampleUI.html")]
    public class ConvoCoreSampleUI : ConvoCoreUIFoundation
    {
        [Header("Dialogue UI Elements")]
        [SerializeField] private TextMeshProUGUI DialogueText;
        [SerializeField] private TextMeshProUGUI SpeakerName;
        [SerializeField] private GameObject DialoguePanel;
        [SerializeField] private Image SpeakerPortraitImage;
        [SerializeField] private Image FullBodyImageLeft;
        [SerializeField] private Image FullBodyImageRight;
        [SerializeField] private Image FullBodyImageCenter;
        [SerializeField] private Button ContinueButton;

        [Header("Settings")]
        [SerializeField] private bool AllowLineAdvanceOutsideButton = false;
        [SerializeField] private bool EnableTypewriterEffect = true;
        [SerializeField] private float TypewriterSpeed = 0.05f; // Time in seconds per character
        [SerializeField] private bool CanSkipTypewriter = true;
        [Header("Input Settings")]
        [SerializeField] private InputAction AdvanceDialogueAction;
        private Coroutine typewriterCoroutine;
        private bool isTyping = false;
        private string fullText = "";
        private bool _continuePressed = false;
        private bool isWaitingForInput = false;

        private void Awake()
        {
            HideDialogue();
            ContinueButton?.onClick.AddListener(OnContinueButtonPressed);
            AdvanceDialogueAction?.Enable();
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            AdvanceDialogueAction?.Enable();
            AdvanceDialogueAction.performed += OnAdvanceDialoguePerformed;
        }

        private void OnDisable()
        {
            AdvanceDialogueAction?.Disable();
            AdvanceDialogueAction.performed -= OnAdvanceDialoguePerformed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lineInfo"></param>
        /// <param name="localizedText"></param>
        /// <param name="speakerName"></param>
        /// <param name="primaryRepresentation"></param>
        public override void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo lineInfo, string localizedText, string speakerName, CharacterRepresentationBase primaryRepresentation)
        {
            DisplayDialogue(localizedText);
            SpeakerName.text = speakerName;

            // Hide all sprite elements
            HideAllSpriteImages();

            // Render character representations (sprites or prefabs)
            RenderRepresentation(lineInfo.PrimaryCharacterRepresentation, DisplaySlot.Center);
            RenderRepresentation(lineInfo.SecondaryCharacterRepresentation, DisplaySlot.Left);
            RenderRepresentation(lineInfo.TertiaryCharacterRepresentation, DisplaySlot.Right);

            ContinueButton?.gameObject.SetActive(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="slot"></param>
        private void RenderRepresentation(ConvoCoreConversationData.CharacterRepresentationData data, DisplaySlot slot)
        {
            var convoCore = FindObjectOfType<ConvoCore>();
            var conversationData = convoCore?.ConversationData;
            if (conversationData == null) return;

            var representation = GetCharacterRepresentationFromData(conversationData, data);
            if (representation == null) return;

            var emotionID = data.SelectedEmotionId;
            var processed = representation.ProcessEmotion(emotionID);

            // Handle prefab-based representation
            if (representation is PrefabCharacterRepresentationData prefabRep)
            {
                PrefabRepresentationSpawner?.SpawnCharacter(
                    prefabRep,
                    emotionID,
                    data.LineSpecificDisplayOptions,
                    slot);
            }
            // Handle sprite-based representation
            else if (processed is SpriteEmotionMapping spriteMapping)
            {
                var displayOptions = data.LineSpecificDisplayOptions ?? spriteMapping.DisplayOptions;

                Image portraitImage = SpeakerPortraitImage;
                Image fullBodyImage = GetFullBodyImage(slot);

                if (portraitImage && spriteMapping.PortraitSprite)
                {
                    portraitImage.sprite = spriteMapping.PortraitSprite;
                    portraitImage.rectTransform.localScale = new Vector3(
                        displayOptions.FlipPortraitX ? -displayOptions.PortraitScale.x : displayOptions.PortraitScale.x,
                        displayOptions.FlipPortraitY ? -displayOptions.PortraitScale.y : displayOptions.PortraitScale.y,
                        displayOptions.PortraitScale.z);
                    portraitImage.gameObject.SetActive(true);
                    TryFadeIn(portraitImage);
                }

                if (fullBodyImage && spriteMapping.FullBodySprite)
                {
                    fullBodyImage.sprite = spriteMapping.FullBodySprite;
                    fullBodyImage.rectTransform.localScale = new Vector3(
                        displayOptions.FlipFullBodyX ? -displayOptions.FullBodyScale.x : displayOptions.FullBodyScale.x,
                        displayOptions.FlipFullBodyY ? -displayOptions.FullBodyScale.y : displayOptions.FullBodyScale.y,
                        displayOptions.FullBodyScale.z);
                    fullBodyImage.gameObject.SetActive(true);
                    TryFadeIn(fullBodyImage);
                }
            }
        }

        private void TryFadeIn(Graphic graphic)
        {
            var fade = graphic.GetComponent<IConvoCoreFadeIn>();
            fade?.FadeIn();
        }

        private Image GetFullBodyImage(DisplaySlot slot) => slot switch
        {
            DisplaySlot.Left => FullBodyImageLeft,
            DisplaySlot.Center => FullBodyImageCenter,
            DisplaySlot.Right => FullBodyImageRight,
            _ => null
        };

        private void HideAllSpriteImages()
        {
            SpeakerPortraitImage?.gameObject.SetActive(false);
            FullBodyImageLeft?.gameObject.SetActive(false);
            FullBodyImageCenter?.gameObject.SetActive(false);
            FullBodyImageRight?.gameObject.SetActive(false);
        }

        private CharacterRepresentationBase GetCharacterRepresentationFromData(ConvoCoreConversationData convoData, ConvoCoreConversationData.CharacterRepresentationData data)
        {
            if (!string.IsNullOrEmpty(data.SelectedCharacterID))
            {
                var profile = convoData.ConversationParticipantProfiles.FirstOrDefault(p => p.CharacterID == data.SelectedCharacterID);
                return profile?.GetRepresentation(data.SelectedRepresentationName);
            }
            return data.SelectedRepresentation;
        }

        public override void DisplayDialogue(string text)
        {
            DialogueText.text = text;
            DialogueText.gameObject.SetActive(true);
            SpeakerName.gameObject.SetActive(true);
            DialoguePanel.gameObject.SetActive(true);
            // Stop any existing typewriter effect
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            
            if (EnableTypewriterEffect)
            {
                DialogueText.text = "";
                isTyping = true;
                typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
            }
            else
            {
                DialogueText.text = text;
                isTyping = false;
            }
        }

        public override void HideDialogue()
        {
            DialogueText.gameObject.SetActive(false);
            SpeakerName.gameObject.SetActive(false);
            DialoguePanel.gameObject.SetActive(false);
            HideAllSpriteImages();
            ContinueButton.gameObject.SetActive(false);
        }

        public override IEnumerator WaitForUserInput()
        {
            isWaitingForInput = true;
            ContinueButton.gameObject.SetActive(true);

            while (isWaitingForInput)
            {
                yield return null;
            }

            ContinueButton.gameObject.SetActive(false);
        }

        public void OnContinueButtonPressed()
        {
            if (!isWaitingForInput) return;
            // If typewriter is active and can be skipped, complete the text immediately
            if (isTyping && CanSkipTypewriter)
            {
                CompleteTypewriter();
                return;
            }
            isWaitingForInput = false;
        }

        private void OnAdvanceDialoguePerformed(InputAction.CallbackContext context)
        {
            // If we're currently typing and can't skip, don't do anything
            if (isTyping && !CanSkipTypewriter)
            {
                return;
            }
            
            if (isWaitingForInput && (AllowLineAdvanceOutsideButton || !IsPointerOverUIElement(ContinueButton)))
            {
                OnContinueButtonPressed();
            }
            // Allow skipping typewriter effect only if CanSkipTypewriter is true
            else if (isTyping && CanSkipTypewriter)
            {
                CompleteTypewriter();
            }
        }
        private IEnumerator TypewriterEffect(string text)
        {
            DialogueText.text = "";
            
            for (int i = 0; i < text.Length; i++)
            {
                DialogueText.text = text.Substring(0, i + 1);
                yield return new WaitForSeconds(TypewriterSpeed);
            }
            
            isTyping = false;
            typewriterCoroutine = null;
        }
        
        private void CompleteTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            
            DialogueText.text = fullText;
            isTyping = false;
        }
        protected bool IsPointerOverUIElement(Component uiElement)
        {
            if (uiElement == null) return false;


            RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    mousePosition = Mouse.current.position.ReadValue();
                }
                else
                {
                    mousePosition = Input.mousePosition;
                }
#else
mousePosition = Input.mousePosition;
#endif
                Vector2 localMousePosition = rectTransform.InverseTransformPoint(mousePosition);
                return rectTransform.rect.Contains(localMousePosition);
            }


            return false;
        }

        private void OnDestroy()
        {
            AdvanceDialogueAction?.Disable();
        }
    }
}