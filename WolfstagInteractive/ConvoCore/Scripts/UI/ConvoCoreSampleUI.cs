using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreSampleUI.html")]
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

        [Header("Dialogue History UI Elements")] 
        [SerializeField] private RectTransform DialogueHistoryPanelRoot;
        [SerializeField] private ScrollRect DialogueHistoryScrollRect;
        [SerializeField] private RectTransform DialogueHistoryScrollRectContent;
        [SerializeField] private TMP_Text DialogueHistoryText;
        [SerializeField] private Button ToggleDialogueHistoryButton;
        
        [Header("Settings")]
        [SerializeField] private bool AllowLineAdvanceOutsideButton;
        [SerializeField] private bool EnableTypewriterEffect = true;
        [SerializeField] private float TypewriterSpeed = 0.05f; // Time in seconds per character
        [SerializeField] private bool CanSkipTypewriter = true;
        [Header("Input Settings")]
        [SerializeField] private InputAction AdvanceDialogueAction;
        private Coroutine _typewriterCoroutine;
        private bool _isTyping;
        private string fullText = "";
        private bool _continuePressed = false;
        private bool _isWaitingForInput;

        public override void InitializeUI(ConvoCore convoCoreInstance)
        {
            base.InitializeUI(convoCoreInstance);
            var historyOutput = new TMPDialogueHistoryOutput(DialogueHistoryText, DialogueHistoryScrollRect);
            var ctx = new DialogueHistoryRendererContext
            {
                OutputHandler = historyOutput,
                DefaultSpeakerColor = Color.white,
                MaxEntries = ConvoCoreDialogueHistoryUI.maxEntries
            };
            HideDialogue();
            ContinueButton?.onClick.AddListener(OnContinueButtonPressed);
            ToggleDialogueHistoryButton?.onClick.AddListener(ToggleDialogueHistoryUI);
            AdvanceDialogueAction?.Enable();
            DontDestroyOnLoad(gameObject);
            ConvoCoreDialogueHistoryUI.InitializeRenderer(ctx);
        }

        private void OnEnable()
        {
            AdvanceDialogueAction?.Enable();
            if (AdvanceDialogueAction != null) AdvanceDialogueAction.performed += OnAdvanceDialoguePerformed;
        }

        private void OnDisable()
        {
            AdvanceDialogueAction?.Disable();
            if (AdvanceDialogueAction != null) AdvanceDialogueAction.performed -= OnAdvanceDialoguePerformed;
        }

        /// <summary>
        /// Updates the dialogue UI by displaying text, speaker information, and character representations for the current dialogue line.
        /// </summary>
        /// <param name="lineInfo">Data about the current dialogue line, including character representation details.</param>
        /// <param name="localizedText">The localized text for the current dialogue line.</param>
        /// <param name="speakerName">The name of the character currently speaking.</param>
        /// <param name="primaryRepresentation">The primary visual representation of the speaking character.</param>
        /// <param name="primaryProfile">The profile data of the speaking character, including display settings such as name color.</param>
        public override void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo lineInfo, string localizedText,
            string speakerName, CharacterRepresentationBase primaryRepresentation,
            ConvoCoreCharacterProfileBaseData primaryProfile)
        {
            DisplayDialogue(localizedText);
            SpeakerName.text = speakerName;
            SpeakerName.color = primaryProfile.CharacterNameColor;
            ConvoCoreDialogueHistoryUI.AddLine(speakerName,localizedText,primaryProfile.CharacterNameColor);

            // Hide all sprite elements
            HideAllSpriteImages();

            // Render character representations (sprites or prefabs)
            RenderRepresentation(lineInfo.PrimaryCharacterRepresentation, DisplaySlot.Center);
            RenderRepresentation(lineInfo.SecondaryCharacterRepresentation, DisplaySlot.Left);
            RenderRepresentation(lineInfo.TertiaryCharacterRepresentation, DisplaySlot.Right);

            ContinueButton?.gameObject.SetActive(true);
        }

        /// <summary>
        /// Renders a character's representation (e.g., sprite-based or prefab-based) in a specified display slot.
        /// </summary>
        /// <param name="data">The character representation data containing details about the character and its display options.</param>
        /// <param name="slot">The display slot where the character representation should be rendered (left, right, or center).</param>
        private void RenderRepresentation(ConvoCoreConversationData.CharacterRepresentationData data, DisplaySlot slot)
        {
            ConvoCoreConversationData conversationData = ConvoCoreInstance?.GetCurrentConversationData();
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
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            
            if (EnableTypewriterEffect)
            {
                DialogueText.text = "";
                _isTyping = true;
                _typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
            }
            else
            {
                DialogueText.text = text;
                _isTyping = false;
            }
        }

        public override void HideDialogue()
        {
            DialogueText.gameObject.SetActive(false);
            SpeakerName.gameObject.SetActive(false);
            DialoguePanel.gameObject.SetActive(false);
            HideAllSpriteImages();
            ContinueButton.gameObject.SetActive(false);
            ToggleDialogueHistoryUI();
        }

        public override IEnumerator WaitForUserInput()
        {
            _isWaitingForInput = true;
            ContinueButton.gameObject.SetActive(true);

            while (_isWaitingForInput)
            {
                yield return null;
            }

            ContinueButton.gameObject.SetActive(false);
        }

        public void OnContinueButtonPressed()
        {
            if (!_isWaitingForInput) return;
            // If the typewriter is active and can be skipped, complete the text immediately
            if (_isTyping && CanSkipTypewriter)
            {
                CompleteTypewriter();
                return;
            }
            _isWaitingForInput = false;
        }

        private void OnAdvanceDialoguePerformed(InputAction.CallbackContext context)
        {
            // --- 1️⃣ Properly detect if the click is over UI ---
            // Only ignore if you're actually allowing click-to-advance
            if (AllowLineAdvanceOutsideButton)
            {
                if (EventSystem.current != null)
                {
                    // For new Input System UI, check using the pointerId
                    var inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
                    if (inputModule != null)
                    {
                        // Mouse pointer id is -1 by default in InputSystemUIInputModule
                        if (EventSystem.current.IsPointerOverGameObject(-1))
                            return; // Ignore UI clicks (so they go to buttons)
                    }
                    else
                    {
                        // Fallback for old input module
                        if (EventSystem.current.IsPointerOverGameObject())
                            return;
                    }
                }
            }

            // --- 2️⃣ Now handle normal advancing logic ---
            if (_isTyping && !CanSkipTypewriter)
                return;

            if (_isWaitingForInput && (AllowLineAdvanceOutsideButton || !IsPointerOverUIElement(ContinueButton)))
            {
                OnContinueButtonPressed();
            }
            else if (_isTyping && CanSkipTypewriter)
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
            
            _isTyping = false;
            _typewriterCoroutine = null;
        }
        
        private void CompleteTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            
            DialogueText.text = fullText;
            _isTyping = false;
        }

        private void ToggleDialogueHistoryUI()
        {
            
            DialogueHistoryPanelRoot.gameObject.SetActive(!DialogueHistoryPanelRoot.gameObject.activeSelf);
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