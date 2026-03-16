using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// Legacy monolithic sample UI. Kept for backward compatibility with existing prefabs.
    /// For new projects use <see cref="ConvoCoreSampleUICanvas"/> (2D/canvas) or
    /// <see cref="ConvoCoreSampleUI3D"/> (world-space) instead.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreSampleUI.html")]
    public class ConvoCoreSampleUI : ConvoCoreUIFoundation
    {
        // Canvas-internal slot concept (replaces the retired DisplaySlot enum).
        private enum CanvasSlot { Left, Right, Center }

        [Header("Prefab Representation")]
        [SerializeField] private ConvoCorePrefabRepresentationSpawner prefabRepresentationSpawner;
        [SerializeField] private Transform prefabLeftAnchor;
        [SerializeField] private Transform prefabRightAnchor;
        [SerializeField] private Transform prefabCenterAnchor;

        [Header("Choice UI Elements")]
        [SerializeField] private GameObject ChoicePanel;
        [SerializeField] private Transform ChoiceButtonContainer;
        [SerializeField] private GameObject ChoiceButtonPrefab;

        [Header("Dialogue UI Elements")]
        [SerializeField] private TextMeshProUGUI DialogueText;
        [SerializeField] private TextMeshProUGUI SpeakerName;
        [SerializeField] private GameObject DialoguePanel;
        [SerializeField] private Image SpeakerPortraitImage;
        [SerializeField] private Image FullBodyImageLeft;
        [SerializeField] private Image FullBodyImageRight;
        [SerializeField] private Image FullBodyImageCenter;
        [SerializeField] private Button ContinueButton;
        [SerializeField] private Button PreviousLineButton;

        [Header("Dialogue History UI Elements")]
        [SerializeField] private RectTransform DialogueHistoryPanelRoot;
        private string _lastSpeakerName;
        private Color _lastSpeakerColor;
        private string _lastLineText;
        private int _lastLineIndex = -1;
        private readonly HashSet<int> _committedLineIndices = new();

        [SerializeField] private ScrollRect DialogueHistoryScrollRect;
        [SerializeField] private RectTransform DialogueHistoryScrollRectContent;
        [SerializeField] private Image DialogueHistoryContentBackground;
        [SerializeField] private TMP_Text DialogueHistoryText;
        [SerializeField] private Button ToggleDialogueHistoryButton;

        [Header("Settings")]
        [SerializeField] private bool AllowLineAdvanceOutsideButton;
        [SerializeField] private bool EnableTypewriterEffect = true;
        [SerializeField] private float TypewriterSpeed = 0.05f;
        [SerializeField] private bool CanSkipTypewriter = true;
        public bool AutoHideUIOnStart;

        [Header("Input Settings")]
        [SerializeField] private InputAction AdvanceDialogueAction;
        [SerializeField] private InputAction PreviousDialogueAction;

        private Coroutine _typewriterCoroutine;
        private bool _isTyping;
        private string fullText = "";
        private bool _continuePressed = false;
        private bool _isWaitingForInput;
        private bool _historyVisible;
        private CanvasGroup _historyGroup;
        private bool _togglingGuard;

        private CanvasSlot GetSlotForIndex(int index)
        {
            if (index == 0) return CanvasSlot.Left;
            if (index == 1) return CanvasSlot.Right;
            return CanvasSlot.Center;
        }

        private Transform GetSlotAnchor(CanvasSlot slot) => slot switch
        {
            CanvasSlot.Left   => prefabLeftAnchor,
            CanvasSlot.Right  => prefabRightAnchor,
            CanvasSlot.Center => prefabCenterAnchor,
            _                 => null
        };

        private void Start()
        {
            if (AutoHideUIOnStart)
                HideDialogue();
        }

        public override void InitializeUI(ConvoCore convoCoreInstance)
        {
            base.InitializeUI(convoCoreInstance);

            if (prefabRepresentationSpawner == null)
            {
                prefabRepresentationSpawner = GetComponent<ConvoCorePrefabRepresentationSpawner>();
                if (prefabRepresentationSpawner == null)
                    Debug.LogWarning($"[ConvoCoreSampleUI] No ConvoCorePrefabRepresentationSpawner assigned or found on '{gameObject.name}'. Prefab characters will not resolve.");
            }

            var historyOutput = new TMPDialogueHistoryOutput(DialogueHistoryText, DialogueHistoryScrollRect);
            var ctx = new DialogueHistoryRendererContext
            {
                OutputHandler = historyOutput,
                DefaultSpeakerColor = Color.white,
                MaxEntries = ConvoCoreDialogueHistoryUI.maxEntries
            };
            HideDialogue();
            ContinueButton?.onClick.AddListener(OnContinueButtonPressed);
            PreviousLineButton?.onClick.AddListener(OnPreviousLineButtonPressed);
            ToggleDialogueHistoryButton?.onClick.AddListener(ToggleDialogueHistoryUI);
            AdvanceDialogueAction?.Enable();
            PreviousDialogueAction?.Enable();
            DontDestroyOnLoad(gameObject);
            ConvoCoreDialogueHistoryUI.InitializeRenderer(ctx);
        }

        private void OnEnable()
        {
            AdvanceDialogueAction?.Enable();
            PreviousDialogueAction?.Enable();
            if (AdvanceDialogueAction != null) AdvanceDialogueAction.performed += OnAdvanceDialoguePerformed;
            if (PreviousDialogueAction != null) PreviousDialogueAction.performed += OnPreviousDialoguePerformed;
        }

        private void OnDisable()
        {
            AdvanceDialogueAction?.Disable();
            PreviousDialogueAction?.Disable();
            if (AdvanceDialogueAction != null) AdvanceDialogueAction.performed -= OnAdvanceDialoguePerformed;
            if (PreviousDialogueAction != null) PreviousDialogueAction.performed -= OnPreviousDialoguePerformed;
        }

        private void OnPreviousLineButtonPressed()
        {
            if (_isTyping && CanSkipTypewriter)
            {
                CompleteTypewriter();
                return;
            }
            _isWaitingForInput = false;
            RaiseReverse();
        }

        private void OnPreviousDialoguePerformed(InputAction.CallbackContext context)
        {
            if (EventSystem.current != null)
            {
                var inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
                if (inputModule != null)
                {
                    if (EventSystem.current.IsPointerOverGameObject(-1)) return;
                }
                else
                {
                    if (EventSystem.current.IsPointerOverGameObject()) return;
                }
            }

            if (_isTyping && !CanSkipTypewriter) return;
            OnPreviousLineButtonPressed();
        }

        private void RefreshNavButtons()
        {
            bool canReverse = ConvoCoreInstance != null && ConvoCoreInstance.CanReverseOneLine && !_historyVisible;
            if (PreviousLineButton != null) PreviousLineButton.interactable = canReverse;
        }

        public override void UpdateDialogueUI(ConvoCoreConversationData.DialogueLineInfo lineInfo, string localizedText,
            string speakerName, CharacterRepresentationBase primaryRepresentation,
            ConvoCoreCharacterProfileBaseData primaryProfile)
        {
            DisplayDialogue(localizedText);
            SpeakerName.text = speakerName;
            SpeakerName.color = primaryProfile.CharacterNameColor;
            _lastSpeakerName = speakerName;
            _lastSpeakerColor = primaryProfile.CharacterNameColor;
            _lastLineText = localizedText;
            _lastLineIndex = lineInfo.ConversationLineIndex;

            lineInfo.EnsureCharacterRepresentationListInitialized();

            int uiCap = Mathf.Max(1, MaxVisibleCharacterSlots);
            int physicalCap = 3;
            int showCount = Mathf.Min(uiCap, physicalCap, lineInfo.CharacterRepresentations.Count);

            HideAllSpriteImages();
            for (int i = 0; i < showCount; i++)
                RenderRepresentation(lineInfo.CharacterRepresentations[i], GetSlotForIndex(i));

            ContinueButton?.gameObject.SetActive(true);
            RefreshNavButtons();
        }

        private IConvoCoreCharacterDisplay RenderRepresentation(
            ConvoCoreConversationData.CharacterRepresentationData data,
            CanvasSlot slot)
        {
            ConvoCoreConversationData conversationData = ConvoCoreInstance?.GetCurrentConversationData();
            if (conversationData == null) return null;

            var representation = GetCharacterRepresentationFromData(conversationData, data);
            if (representation == null) return null;

            var expressionID = data.SelectedExpressionId;

            if (representation is PrefabCharacterRepresentationData prefabRep)
            {
                var anchor = GetSlotAnchor(slot);
                return prefabRepresentationSpawner?.ResolveCharacter(
                    prefabRep,
                    expressionID,
                    data.LineSpecificDisplayOptions,
                    anchor);
            }

            var processed = representation.ProcessExpression(expressionID);

            if (processed is SpriteExpressionMapping spriteMapping)
            {
                RenderSpriteRepresentation(spriteMapping, data.LineSpecificDisplayOptions, slot);
                return null;
            }

            Debug.LogWarning($"[ConvoCoreSampleUI] Representation type '{representation.GetType().Name}' " +
                             $"returned an unhandled result from ProcessExpression. " +
                             $"Override RenderRepresentation in a subclass to handle custom types.");
            return null;
        }

        private void RenderSpriteRepresentation(SpriteExpressionMapping spriteMapping,
            DialogueLineDisplayOptions lineOptions, CanvasSlot slot)
        {
            var displayOptions = lineOptions ?? spriteMapping.DisplayOptions;

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

        private void TryFadeIn(Graphic graphic)
        {
            var fade = graphic.GetComponent<IConvoCoreFadeIn>();
            fade?.FadeIn();
        }

        private Image GetFullBodyImage(CanvasSlot slot) => slot switch
        {
            CanvasSlot.Left   => FullBodyImageLeft,
            CanvasSlot.Center => FullBodyImageCenter,
            CanvasSlot.Right  => FullBodyImageRight,
            _                 => null
        };

        private void HideAllSpriteImages()
        {
            SpeakerPortraitImage?.gameObject.SetActive(false);
            FullBodyImageLeft?.gameObject.SetActive(false);
            FullBodyImageCenter?.gameObject.SetActive(false);
            FullBodyImageRight?.gameObject.SetActive(false);
        }

        private CharacterRepresentationBase GetCharacterRepresentationFromData(ConvoCoreConversationData convoData,
            ConvoCoreConversationData.CharacterRepresentationData data)
        {
            if (!string.IsNullOrEmpty(data.SelectedCharacterID))
            {
                var profile = convoData.ConversationParticipantProfiles.FirstOrDefault(p =>
                    p.CharacterID == data.SelectedCharacterID);
                return profile?.GetRepresentation(data.SelectedRepresentationName);
            }

            return data.SelectedRepresentation;
        }

        public override void DisplayDialogue(string text)
        {
            fullText = text;
            DialogueText.text = text;
            DialogueText.gameObject.SetActive(true);
            SpeakerName.gameObject.SetActive(true);
            DialoguePanel.gameObject.SetActive(true);

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
            ToggleDialogueHistoryUI(false);
            RefreshNavButtons();
        }

        public override IEnumerator WaitForUserInput()
        {
            _isWaitingForInput = true;
            ContinueButton.gameObject.SetActive(true);

            while (_isWaitingForInput)
                yield return null;

            ContinueButton.gameObject.SetActive(false);
        }

        public override IEnumerator PresentChoices(
            List<ConvoCoreConversationData.ChoiceOption> options,
            List<string> localizedLabels,
            ChoiceResult result)
        {
            if (ChoicePanel == null || ChoiceButtonContainer == null || ChoiceButtonPrefab == null)
            {
                Debug.LogWarning("[ConvoCore] ConvoCoreSampleUI: ChoicePanel, ChoiceButtonContainer, or ChoiceButtonPrefab is not assigned. Auto-selecting first choice.");
                result.SelectedIndex = 0;
                yield break;
            }

            ChoicePanel.SetActive(true);
            ContinueButton?.gameObject.SetActive(false);

            var spawnedButtons = new List<GameObject>();

            for (int i = 0; i < localizedLabels.Count; i++)
            {
                int captured = i;
                var instance = Instantiate(ChoiceButtonPrefab, ChoiceButtonContainer);
                spawnedButtons.Add(instance);

                var btn = instance.GetComponent<Button>();
                var label = instance.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null) label.text = localizedLabels[captured];
                if (btn != null) btn.onClick.AddListener(() => result.SelectedIndex = captured);
            }

            yield return new WaitUntil(() => result.IsResolved);

            foreach (var btn in spawnedButtons)
                if (btn != null) Destroy(btn);

            ChoicePanel.SetActive(false);
            ContinueButton?.gameObject.SetActive(true);
        }

        public void OnContinueButtonPressed()
        {
            if (!_isWaitingForInput) return;

            if (_isTyping && CanSkipTypewriter)
            {
                CompleteTypewriter();
                return;
            }

            if (_lastLineIndex >= 0 && !_committedLineIndices.Contains(_lastLineIndex))
            {
                if (!string.IsNullOrEmpty(_lastSpeakerName) && !string.IsNullOrEmpty(_lastLineText))
                {
                    ConvoCoreDialogueHistoryUI.AddLine(_lastSpeakerName, _lastLineText, _lastSpeakerColor);
                    _committedLineIndices.Add(_lastLineIndex);
                }
            }

            _isWaitingForInput = false;
            RaiseAdvance();
        }

        private void OnAdvanceDialoguePerformed(InputAction.CallbackContext context)
        {
            if (AllowLineAdvanceOutsideButton)
            {
                if (EventSystem.current != null)
                {
                    var inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
                    if (inputModule != null)
                    {
                        if (EventSystem.current.IsPointerOverGameObject(-1)) return;
                    }
                    else
                    {
                        if (EventSystem.current.IsPointerOverGameObject()) return;
                    }
                }
            }

            if (_isTyping && !CanSkipTypewriter) return;

            if (_isWaitingForInput && (AllowLineAdvanceOutsideButton || !IsPointerOverUIElement(ContinueButton)))
                OnContinueButtonPressed();
            else if (_isTyping && CanSkipTypewriter)
                CompleteTypewriter();
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

        public void ToggleDialogueHistoryUI() => ToggleDialogueHistoryUI(null);

        public void ToggleDialogueHistoryUI(bool? setVisible)
        {
            if (_togglingGuard) return;

            if (DialogueHistoryPanelRoot == null)
            {
                Debug.LogWarning("[ConvoCore UI] DialogueHistoryPanelRoot not assigned.");
                return;
            }

            bool target = setVisible ?? !_historyVisible;
            _togglingGuard = true;
            _historyVisible = target;

            if (_historyGroup != null)
            {
                _historyGroup.alpha = target ? 1f : 0f;
                _historyGroup.interactable = target;
                _historyGroup.blocksRaycasts = target;
                DialogueHistoryPanelRoot.gameObject.SetActive(true);
            }
            else
            {
                DialogueHistoryPanelRoot.gameObject.SetActive(target);
            }

            if (DialogueHistoryContentBackground)
                DialogueHistoryContentBackground.enabled = target;

            if (ContinueButton != null)
                ContinueButton.interactable = !target;

            if (target && DialogueHistoryScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                DialogueHistoryScrollRect.normalizedPosition = new Vector2(0f, 0f);
            }

            _togglingGuard = false;
            RefreshNavButtons();
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
                    mousePosition = Mouse.current.position.ReadValue();
                else
                    mousePosition = Input.mousePosition;
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
            PreviousDialogueAction?.Disable();
            _committedLineIndices.Clear();
        }
    }
}
