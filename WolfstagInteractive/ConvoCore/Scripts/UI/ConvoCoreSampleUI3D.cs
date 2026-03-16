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

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// World-space dialogue UI for 3D character conversations. No sprite support; no slot system.
    /// Character placement is entirely presence-driven via <see cref="ConvoCoreCharacterPresence"/>.
    ///
    /// Characters are persistent across lines. Expression application is the only per-line
    /// operation; characters are not despawned and re-spawned between lines.
    ///
    /// <see cref="ConvoCoreCharacterPresence.OnConversationBegin"/> is called when
    /// <see cref="ConvoCore.StartedConversation"/> fires.
    /// <see cref="ConvoCoreCharacterPresence.OnConversationEnd"/> is called when
    /// <see cref="ConvoCore.EndedConversation"/> fires. The spawner is never called directly
    /// by this UI for release; that responsibility belongs to the presence.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreSampleUI3D.html")]
    public class ConvoCoreSampleUI3D : ConvoCoreUIFoundation
    {
        [Header("Character Presence")]
        [Tooltip("Presence asset that determines how characters are placed in the world.")]
        [SerializeField] private ConvoCoreCharacterPresence characterPresence;

        [Header("Spawner")]
        [Tooltip("Spawner passed to the presence for prefab lifecycle management.")]
        [SerializeField] private ConvoCorePrefabRepresentationSpawner prefabRepresentationSpawner;

        [Header("Choice UI")]
        [SerializeField] private GameObject ChoicePanel;
        [SerializeField] private Transform ChoiceButtonContainer;
        [SerializeField] private GameObject ChoiceButtonPrefab;

        [Header("Dialogue UI")]
        [SerializeField] private TextMeshProUGUI DialogueText;
        [SerializeField] private TextMeshProUGUI SpeakerName;
        [SerializeField] private GameObject DialoguePanel;
        [SerializeField] private Button ContinueButton;
        [SerializeField] private Button PreviousLineButton;

        [Header("Dialogue History")]
        [SerializeField] private RectTransform DialogueHistoryPanelRoot;
        [SerializeField] private ScrollRect DialogueHistoryScrollRect;
        [SerializeField] private Image DialogueHistoryContentBackground;
        [SerializeField] private TMP_Text DialogueHistoryText;
        [SerializeField] private Button ToggleDialogueHistoryButton;

        [Header("Settings")]
        [SerializeField] private bool AllowLineAdvanceOutsideButton;
        [SerializeField] private bool EnableTypewriterEffect = true;
        [SerializeField] private float TypewriterSpeed = 0.05f;
        [SerializeField] private bool CanSkipTypewriter = true;
        public bool AutoHideUIOnStart;

        [Header("Input")]
        [SerializeField] private InputAction AdvanceDialogueAction;
        [SerializeField] private InputAction PreviousDialogueAction;

        private Coroutine _typewriterCoroutine;
        private bool _isTyping;
        private string _fullText = "";
        private bool _isWaitingForInput;
        private bool _historyVisible;
        private CanvasGroup _historyGroup;
        private bool _togglingGuard;

        private string _lastSpeakerName;
        private Color _lastSpeakerColor;
        private string _lastLineText;
        private int _lastLineIndex = -1;
        private readonly HashSet<int> _committedLineIndices = new();

        // ------------------------------------------------------------------
        // Initialization
        // ------------------------------------------------------------------

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
                    Debug.LogWarning($"[ConvoCoreSampleUI3D] No ConvoCorePrefabRepresentationSpawner assigned on '{gameObject.name}'.");
            }

            // Subscribe to conversation lifecycle events so the presence can be notified.
            ConvoCoreInstance.StartedConversation += OnConversationStarted;
            ConvoCoreInstance.EndedConversation   += OnConversationEnded;

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

        private void OnConversationStarted() => characterPresence?.OnConversationBegin();

        private void OnConversationEnded() => characterPresence?.OnConversationEnd();

        private void OnEnable()
        {
            AdvanceDialogueAction?.Enable();
            PreviousDialogueAction?.Enable();
            if (AdvanceDialogueAction != null) AdvanceDialogueAction.performed += OnAdvancePerformed;
            if (PreviousDialogueAction != null) PreviousDialogueAction.performed += OnPreviousPerformed;
        }

        private void OnDisable()
        {
            AdvanceDialogueAction?.Disable();
            PreviousDialogueAction?.Disable();
            if (AdvanceDialogueAction != null) AdvanceDialogueAction.performed -= OnAdvancePerformed;
            if (PreviousDialogueAction != null) PreviousDialogueAction.performed -= OnPreviousPerformed;
        }

        // ------------------------------------------------------------------
        // Dialogue UI overrides
        // ------------------------------------------------------------------

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

            if (characterPresence != null && prefabRepresentationSpawner != null)
            {
                int count = lineInfo.CharacterRepresentations.Count;
                for (int i = 0; i < count; i++)
                {
                    var charData = lineInfo.CharacterRepresentations[i];
                    var rep = GetRepresentationFromData(ConvoCoreInstance.GetCurrentConversationData(), charData);

                    if (rep is not PrefabCharacterRepresentationData prefabRep)
                        continue;

                    var ctx = new CharacterPresenceContext
                    {
                        CharacterIndex  = i,
                        TotalCharacters = count,
                        DisplayOptions  = charData.LineSpecificDisplayOptions
                    };

                    var display = characterPresence.ResolvePresence(prefabRep, ctx, prefabRepresentationSpawner);
                    if (display == null)
                    {
                        Debug.LogWarning($"[ConvoCoreSampleUI3D] Presence returned null for character {i} ('{rep.name}'). Expression will not be applied.");
                        continue;
                    }

                    display.BindRepresentation(rep);

                    if (charData.LineSpecificDisplayOptions != null)
                        display.ApplyDisplayOptions(charData.LineSpecificDisplayOptions);

                    if (!string.IsNullOrEmpty(charData.SelectedExpressionId))
                        display.ApplyExpression(charData.SelectedExpressionId);
                }
            }

            ContinueButton?.gameObject.SetActive(true);
            RefreshNavButtons();
        }

        public override void DisplayDialogue(string text)
        {
            _fullText = text;
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
                _isTyping = false;
            }
        }

        public override void HideDialogue()
        {
            DialogueText.gameObject.SetActive(false);
            SpeakerName.gameObject.SetActive(false);
            DialoguePanel.gameObject.SetActive(false);
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
                Debug.LogWarning("[ConvoCoreSampleUI3D] ChoicePanel, ChoiceButtonContainer, or ChoiceButtonPrefab not assigned. Auto-selecting first choice.");
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

        // ------------------------------------------------------------------
        // Input and button handling
        // ------------------------------------------------------------------

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

        private void OnAdvancePerformed(InputAction.CallbackContext context)
        {
            if (AllowLineAdvanceOutsideButton && IsPointerOverUI()) return;
            if (_isTyping && !CanSkipTypewriter) return;

            if (_isWaitingForInput && (AllowLineAdvanceOutsideButton || !IsPointerOverUIElement(ContinueButton)))
                OnContinueButtonPressed();
            else if (_isTyping && CanSkipTypewriter)
                CompleteTypewriter();
        }

        private void OnPreviousPerformed(InputAction.CallbackContext context)
        {
            if (IsPointerOverUI()) return;
            if (_isTyping && !CanSkipTypewriter) return;
            OnPreviousLineButtonPressed();
        }

        private void RefreshNavButtons()
        {
            bool canReverse = ConvoCoreInstance != null && ConvoCoreInstance.CanReverseOneLine && !_historyVisible;
            if (PreviousLineButton != null) PreviousLineButton.interactable = canReverse;
        }

        // ------------------------------------------------------------------
        // Typewriter
        // ------------------------------------------------------------------

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
            DialogueText.text = _fullText;
            _isTyping = false;
        }

        // ------------------------------------------------------------------
        // History
        // ------------------------------------------------------------------

        public void ToggleDialogueHistoryUI() => ToggleDialogueHistoryUI(null);

        public void ToggleDialogueHistoryUI(bool? setVisible)
        {
            if (_togglingGuard || DialogueHistoryPanelRoot == null) return;

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

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private CharacterRepresentationBase GetRepresentationFromData(ConvoCoreConversationData convoData,
            ConvoCoreConversationData.CharacterRepresentationData data)
        {
            if (!string.IsNullOrEmpty(data.SelectedCharacterID))
            {
                var profile = convoData?.ConversationParticipantProfiles.FirstOrDefault(p =>
                    p.CharacterID == data.SelectedCharacterID);
                return profile?.GetRepresentation(data.SelectedRepresentationName);
            }
            return data.SelectedRepresentation;
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            var inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            return inputModule != null
                ? EventSystem.current.IsPointerOverGameObject(-1)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsPointerOverUIElement(Component uiElement)
        {
            if (uiElement == null) return false;
            var rt = uiElement.GetComponent<RectTransform>();
            if (rt == null) return false;

            Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
            mousePosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;
#else
            mousePosition = Input.mousePosition;
#endif
            return rt.rect.Contains(rt.InverseTransformPoint(mousePosition));
        }

        private void OnDestroy()
        {
            if (ConvoCoreInstance != null)
            {
                ConvoCoreInstance.StartedConversation -= OnConversationStarted;
                ConvoCoreInstance.EndedConversation   -= OnConversationEnded;
            }

            AdvanceDialogueAction?.Disable();
            PreviousDialogueAction?.Disable();
            _committedLineIndices.Clear();
        }
    }
}
