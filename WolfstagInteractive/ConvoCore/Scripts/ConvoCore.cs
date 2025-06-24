using System;
using System.Collections;
using UnityEngine;

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
        public ConvoCoreConversationData ConversationData; // Reference to your dialogue data
        public Action OnConversationStartEvent;
        public Action OnConversationEndEvent;
        private AudioSource _audioSource; //used for playing audio from dialogue lines
        public ConversationState CurrentDialogueState { get;private set; } = ConversationState.Idle;
        private ConvoCoreDialogueLocalizationHandler LocalizationHandler { get; set; }
        private int _currentLineIndex = -1; // Initialize to -1 (no active line)
      

        
        private IUIFoundation _uiFoundation;
        /// <summary>
        /// Optionally set the UI from the inspector
        /// </summary>
        public ConvoCoreUIFoundation ConversationUI;
        /// <summary>
        /// Set the UI from code
        /// </summary>
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
        /// <summary>
        /// Initializes the ConvoCore instance
        /// </summary>
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
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
            if (languageManager == null)
            {
                Debug.LogError("LanguageManager is not initialized. Ensure the LanguageSettings asset exists in Resources.");
                return;
            }
            // Initialize the localization handler with the LanguageManager singleton instance
            LocalizationHandler = new ConvoCoreDialogueLocalizationHandler(languageManager);
        }
        /// <summary>
        /// Handles language change events from the LanguageManager
        /// </summary>
        /// <param name="newLanguage"></param>
        private void OnLanguageChanged(string newLanguage)
        {
            Debug.Log($"DialogueStateMachine detected language change: {newLanguage}");
            UpdateUIForLanguage(newLanguage); //Refresh the UI for the new language
        }
        /// <summary>
        /// Updates the UI for the current language
        /// </summary>
        /// <param name="newLanguage"></param>
        public void UpdateUIForLanguage(string newLanguage)
        {
            if (ConversationData == null)
            {
                Debug.LogWarning("No conversation object found to update UI.");
                return;
            }
            if (CurrentDialogueState == ConversationState.Playing)
            {
                RefreshLocalizedText();
                _uiFoundation?.UpdateForLanguageChange(newLanguage);
            }
        }
        /// <summary>
        /// Starts the conversation
        /// </summary>
        public void StartConversation()
        {
            ConversationData.InitializeDialogueData();
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
            CurrentDialogueState = ConversationState.Starting;
            yield return OnConversationStart();

            // Get all profiles from the conversation object
            var profiles = ConversationData.ConversationParticipantProfiles;
           
            // Iterate through all dialogue lines
            CurrentDialogueState = ConversationState.Playing;
            for (_currentLineIndex = 0; _currentLineIndex < ConversationData.dialogueLines.Count; _currentLineIndex++)
            {
                var line = ConversationData.dialogueLines[_currentLineIndex];

                // Resolve character profile for the line
                var profile = ConversationData.ResolveCharacterProfile(profiles, line.characterID);
                if (profile == null)
                {
                    Debug.LogError($"Cannot resolve profile for CharacterID '{line.characterID}'. Skipping line.");
                    continue; // Skip if no profile is found
                }
                // Get localized dialogue
                var localizedResult = LocalizationHandler.GetLocalizedDialogue(line);
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
                    yield return StartCoroutine(ConversationData.ActionsBeforeDialogueLine(this, line));
                }
                //check and get player placeholder name and replace with name of player in line
               string finalOutputString = ReplacePlayerNameInDialogueLine(localizedResult.Text);
                
                // Play audio and display dialogue
                yield return StartCoroutine(PlayAudioClipWithAction(line.clip));
                var characterName = profile.GetNameForRepresentation(line.AlternateRepresentation);
                var portrait = profile.GetEmotionForRepresentation(line.SelectedEmotionName, line.AlternateRepresentation);

                yield return StartCoroutine(
                    PlayDialogueLine(_uiFoundation, line, finalOutputString, characterName, portrait)
                );

                // Actions after the dialogue line
                if (line.ActionsAfterDialogueLine != null && line.ActionsAfterDialogueLine.Count > 0)
                {
                    yield return StartCoroutine(ConversationData.DoActionsAfterDialogueLine(this, line));
                }
            }

            // Conversation end
            CurrentDialogueState = ConversationState.Ended;
            yield return OnConversationEnd();
            CurrentDialogueState = ConversationState.Idle;
            _currentLineIndex = -1;
        }
        /// <summary>
        /// Plays the audio clip associated with the dialogue line
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        private IEnumerator PlayAudioClipWithAction(AudioClip clip)
        {
            if (clip == null)
            {
                yield break;
            }
            _audioSource.clip = clip;
            _audioSource.Play();
            while (_audioSource.isPlaying)
            {
                yield return null;
            }
        }
       
        private string ReplacePlayerNameInDialogueLine(string dialogueLine)
        {
            // Find the player profile
            var playerProfile = ConversationData.GetPlayerProfile();
            if (playerProfile != null)
            {
                if (!string.IsNullOrEmpty(playerProfile.CharacterName) &&
                    !string.IsNullOrEmpty(playerProfile.PlayerPlaceholder))
                {
                    dialogueLine = dialogueLine.Replace(playerProfile.PlayerPlaceholder, playerProfile.CharacterName);
                    return dialogueLine; // Replace the placeholder with the player's custom set name'
                }
                
                Debug.LogWarning("Player profile is missing a custom name or placeholder.");
                return dialogueLine; // Return the original line if the player profile is missing a custom name or placeholder
                
            }
            return dialogueLine;
            
        }
        /// <summary>
        /// Plays a dialogue line and handles user input
        /// </summary>
        /// <param name="line">The current dialogue line data</param>
        /// <param name="localizedText">The current localizations text</param>
        /// <param name="characterName">The name of the speaking character of the dialogue line</param>
        /// <param name="portrait">The potrait from the speaking characters character data</param>
        /// <returns></returns>
        private IEnumerator PlayDialogueLine(IUIFoundation uiFoundationInstance,ConvoCoreConversationData.DialogueLines line
            , string localizedText, string characterName, Sprite portrait)
        {
            uiFoundationInstance.UpdateDialogueUI(line, localizedText, characterName, portrait);
            switch (line.UserInputMethod)
            {
                case ConvoCoreConversationData.DialogueLineProgressionMethod.UserInput:
                    // Wait for user input
                    yield return uiFoundationInstance.WaitForUserInput();
                    break;
                case ConvoCoreConversationData.DialogueLineProgressionMethod.Timed:
                    // Skip user input
                    yield return new WaitForSeconds(line.TimeBeforeNextLine);
                    break;
            }
        }
        /// <summary>
        /// Get the currently displayed dialogue line struct data
        /// </summary>
        /// <returns></returns>
        public ConvoCoreConversationData.DialogueLines? GetCurrentlyDisplayedDialogueLine()
        {
            if (_currentLineIndex >= 0 && _currentLineIndex < ConversationData.dialogueLines.Count)
            {
                // Explicit cast to transform the base type or derived type
                return ConversationData.dialogueLines[_currentLineIndex];
            }
            Debug.LogWarning("No currently displayed dialogue line or index is out of range.");
            return null;
        }
        /// <summary>
        /// Logic to perform when the conversation ends, avaible to be overridden if needed.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator OnConversationStart()
        {
            OnConversationStartEvent?.Invoke();
            // Implement your own logic to run before the dialogue tool starts by overriding this function
            yield return null;
        }
        /// <summary>
        /// Logic to perform when the conversation ends, avaible to be overridden if needed.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator OnConversationEnd()
        {
            OnConversationEndEvent?.Invoke();
            // Implement your own logic to run before the dialogue tool ends by overriding this function
            yield return null;
        }
        /// <summary>
        /// In the event of the language changing, refresh the currently display text with the new localization.
        /// </summary>
        private void RefreshLocalizedText()
        {
            // Fetch the currently displayed dialogue line using the helper function
            var currentDialogueLine = GetCurrentlyDisplayedDialogueLine();

            if (currentDialogueLine == null)
            {
                Debug.LogWarning("Cannot refresh localized text as no valid dialogue line is currently displayed.");
                return;
            }

            // Use the localization handler to get the localized text for the current dialogue line
            var localizedResult = LocalizationHandler.GetLocalizedDialogue(currentDialogueLine.Value);

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
        /// <summary>
        /// Change the current dialogue line via a provided index of the conversation while the conversation is playing
        /// </summary>
        /// <param name="index"></param>
        public void ChangeCurrentDialogueLine(int index)
        {
            if (CurrentDialogueState != ConversationState.Playing)
            {
                return;
            }
            if (index >= 0 && index < ConversationData.dialogueLines.Count)
            {
                _currentLineIndex = index;
            }
            else
            {
                Debug.LogWarning("Invalid index for changing current dialogue line.");
            }
        }
        /// <summary>
        /// Get the conversations line count from the current conversation
        /// </summary>
        /// <returns></returns>
        public int GetConversationLineCount()
        {
            return ConversationData.dialogueLines.Count;
        }
        /// <summary>
        /// Get the current dialogues line index within the conversation
        /// </summary>
        /// <returns></returns>
        public int GetCurrentDialogueLineIndex()
        {
            return _currentLineIndex;
        }
    }

}