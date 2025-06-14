using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace WolfstagInteractive.ConvoCore
{
    [Serializable]
    public enum ConversationState
    {
        Idle,
        Starting,
        Playing,
        Ended
    }
    [RequireComponent(typeof(AudioSource))][DisallowMultipleComponent]
    public class ConvoCore : MonoBehaviour
    {
        public ConvoCoreConversationData ConversationDataData; // Reference to your dialogue data
        private AudioSource audioSource; //used for playing audio from dialogue lines
        public ConversationState _currentDialogueState { get;private set; } = ConversationState.Idle;
        private ConvoCoreDialogueLocalizationHandler _localizationHandler { get; set; }
        private int currentLineIndex = -1; // Initialize to -1 (no active line)
        
        private IUIFoundation _uiFoundation;
        /// <summary>
        /// Optionally set the UI from the inspector
        /// </summary>
        public ConvoCoreUIFoundation ConversationUI;

        public void SetUI(IUIFoundation uiFoundation)
        {
            _uiFoundation = uiFoundation;
            _uiFoundation.InitializeUI(this);
        }

        private void OnEnable()
        {
            ConvoCoreLanguageManager.OnLanguageChanged += OnLanguageChanged;
            _uiFoundation?.InitializeUI(this);
        }

        private void OnDisable()
        {
            ConvoCoreLanguageManager.OnLanguageChanged -= OnLanguageChanged; 
            _uiFoundation?.Dispose();
        }

        private void Initialize()
        {
            //Set ui from value
            if (ConversationUI != null)
            {
                SetUI(ConversationUI);
            }
            // Access the LanguageManager Singleton instance
            var languageManager = ConvoCoreLanguageManager.Instance;
            //get audio source
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            if (languageManager == null)
            {
                Debug.LogError("LanguageManager is not initialized. Ensure the LanguageSettings asset exists in Resources.");
                return;
            }
            // Initialize the localization handler with the LanguageManager singleton instance
            _localizationHandler = new ConvoCoreDialogueLocalizationHandler(languageManager);
        }
        private void OnLanguageChanged(string newLanguage)
        {
            Debug.Log($"DialogueStateMachine detected language change: {newLanguage}");
            UpdateUIForLanguage(newLanguage); //Refresh the UI for the new language
        }
        public void UpdateUIForLanguage(string newLanguage)
        {
            if (ConversationDataData == null)
            {
                Debug.LogWarning("No conversation object found to update UI.");
                return;
            }
            if (_currentDialogueState == ConversationState.Playing)
            {
                RefreshLocalizedText(this);
                _uiFoundation?.UpdateForLanguageChange(newLanguage);
            }
        }
        public void StartConversation()
        {
            ConversationDataData.InitializeDialogueData();
            StartCoroutine(RunConversation());
        }
       
        private void Start()
        {
            Initialize();
        }
        /// <summary>
        /// Runs the character conversation line by line
        /// </summary>
        /// <returns></returns>
       private IEnumerator RunConversation()
        {
            // Lock UI elements or other systems as needed
            _currentDialogueState = ConversationState.Starting;
            yield return OnConversationStart();

            // Get all profiles from the conversation object
            var profiles = ConversationDataData.ConversationParticipantProfiles;
           
            // Iterate through all dialogue lines
            _currentDialogueState = ConversationState.Playing;
            for (currentLineIndex = 0; currentLineIndex < ConversationDataData.dialogueLines.Count; currentLineIndex++)
            {
                var line = ConversationDataData.dialogueLines[currentLineIndex];

                // Resolve character profile for the line
                var profile = ConversationDataData.ResolveCharacterProfile(profiles, line.characterID);
                if (profile == null)
                {
                    Debug.LogError($"Cannot resolve profile for CharacterID '{line.characterID}'. Skipping line.");
                    continue; // Skip if no profile is found
                }
                // Get localized dialogue
                var localizedResult = _localizationHandler.GetLocalizedDialogue(line);
                if (!localizedResult.Success)
                {
                    Debug.LogError(localizedResult.ErrorMessage);
                }
                else if (localizedResult.IsFallback)
                {
                    Debug.LogWarning(localizedResult.ErrorMessage);
                }
                // Actions before the dialogue line
                if (line.ActionsBeforeDialogueLine != null && line.ActionsBeforeDialogueLine.Count > 0)
                {
                    yield return StartCoroutine(ConversationDataData.ActionsBeforeDialogueLine(this, line));
                }
                // Play audio and display dialogue
                yield return StartCoroutine(PlayAudioClipWithAction(line.clip));
                var characterName = profile.GetNameForRepresentation(line.AlternateRepresentation);
                var portrait = profile.GetEmotionForRepresentation(line.SelectedEmotionName, line.AlternateRepresentation);

                yield return StartCoroutine(
                    PlayDialogueLine(_uiFoundation, line, localizedResult.Text, characterName, portrait)
                );

                // Actions after the dialogue line
                if (line.ActionsAfterDialogueLine != null && line.ActionsAfterDialogueLine.Count > 0)
                {
                    yield return StartCoroutine(ConversationDataData.DoActionsAfterDialogueLine(this, line));
                }
            }

            // Conversation end
            _currentDialogueState = ConversationState.Ended;
            yield return OnConversationEnd();
            _currentDialogueState = ConversationState.Idle;

        }
        private IEnumerator PlayAudioClipWithAction(AudioClip clip)
        {
            if (clip == null)
            {
                yield break;
            }
            audioSource.clip = clip;
            audioSource.Play();
            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }

        private IEnumerator PlayDialogueLine(IUIFoundation uiFoundationInstance,ConvoCoreConversationData.DialogueLines line
            , string localizedText, string characterName, Sprite portrait)
        {
            uiFoundationInstance.UpdateDialogueUI(line, localizedText, characterName, portrait);
            switch (line.UserInputMethod)
            {
                case ConvoCoreConversationData.DialogueLineProgressionMethod.UserInput:
                    // Wait for user input
                    break;
                case ConvoCoreConversationData.DialogueLineProgressionMethod.Timed:
                    // Skip user input
                    yield return new WaitForSeconds(line.TimeBeforeNextLine);
                    break;
            }
        }
        public ConvoCoreConversationData.DialogueLines? GetCurrentlyDisplayedDialogueLine()
        {
            if (currentLineIndex >= 0 && currentLineIndex < ConversationDataData.dialogueLines.Count)
            {
                // Explicit cast to transform the base type or derived type
                return ConversationDataData.dialogueLines[currentLineIndex];
            }
            Debug.LogWarning("No currently displayed dialogue line or index is out of range.");
            return null;
        }

        protected virtual IEnumerator OnConversationStart()
        {
            // Implement your own logic to run before the dialogue tool starts by overriding this function
            yield return null;
        }
        protected virtual IEnumerator OnConversationEnd()
        {
            // Implement your own logic to run before the dialogue tool ends by overriding this function
            yield return null;
        }
        private void RefreshLocalizedText(ConvoCore convoCore)
        {
            // Fetch the currently displayed dialogue line using the helper function
            var currentDialogueLine = convoCore.GetCurrentlyDisplayedDialogueLine();

            if (currentDialogueLine == null)
            {
                Debug.LogWarning("Cannot refresh localized text as no valid dialogue line is currently displayed.");
                return;
            }

            // Use the localization handler to get the localized text for the current dialogue line
            var localizedResult = _localizationHandler.GetLocalizedDialogue(currentDialogueLine.Value);

            // Check if the localized dialogue result was successful
            if (!localizedResult.Success)
            {
                Debug.LogError($"Failed to retrieve localized text: {localizedResult.ErrorMessage}");
                return;
            }
            // Optionally warn about fallback localization
            if (localizedResult.IsFallback)
            {
                Debug.LogWarning($"Using fallback localization for dialogue line: {localizedResult.ErrorMessage}");
            }
            // Update the UI to display the localized text
            _uiFoundation.UpdateForLanguageChange(localizedResult.Text);
        }
        public void ChangeCurrentDialogueLine(int index)
        {
            if (_currentDialogueState != ConversationState.Playing)
            {
                return;
            }
            if (index >= 0 && index < ConversationDataData.dialogueLines.Count)
            {
                currentLineIndex = index;
            }
            else
            {
                Debug.LogWarning("Invalid index for changing current dialogue line.");
            }
        }
    }
}