using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCore.html")]
    public class ConvoCore : MonoBehaviour,IConvoCoreRunner
    {

         private ConvoCoreConversationData ConversationData;
        public ConvoCoreConversationData GetCurrentConversationData() => ConversationData;
        [Header("Conversation Settings")]
        [SerializeReference] public IConvoInput Input = new SingleConversationInput();

        [Header("Conversation UI")]
        public ConvoCoreUIFoundation ConversationUI;
        
        public ConversationState CurrentDialogueState { get; private set; } = ConversationState.Inactive;
        
        private int _currentLineIndex = 0;
        private ConvoCoreDialogueLocalizationHandler LocalizationHandler;
        public event Action StartedConversation;
        public event Action PausedConversation;
        public event Action EndedConversation;
        public event Action CompletedConversation;

        public enum ConversationState
        {
            Inactive,
            Active,
            Paused,
            Completed
        }

        private void Awake()
        {
            LocalizationHandler = new ConvoCoreDialogueLocalizationHandler(ConvoCoreLanguageManager.Instance);
        }

        /// <summary>
        /// Main coroutine that handles the conversation flow
        /// </summary>
        private IEnumerator ExecuteDialogueSequence()
        {
            while (_currentLineIndex < ConversationData.DialogueLines.Count && CurrentDialogueState == ConversationState.Active)
            {
                var line = ConversationData.DialogueLines[_currentLineIndex];
                
                // Resolve the primary character profile
                var primaryProfile = ConversationData.ResolveCharacterProfile(
                    ConversationData.ConversationParticipantProfiles, 
                    line.characterID);

                if (primaryProfile == null)
                {
                    Debug.LogError($"Cannot resolve character profile for ID: '{line.characterID}'. Skipping line {_currentLineIndex}.");
                    _currentLineIndex++;
                    continue;
                }

                // Get the primary character representation
                var primaryRepresentation = GetPrimaryCharacterRepresentation(primaryProfile, line.PrimaryCharacterRepresentation);

                if (primaryRepresentation == null)
                {
                    Debug.LogError($"Cannot resolve primary character representation for '{primaryProfile.CharacterName}'. " +
                                   $"Line index: {_currentLineIndex}. " +
                                   $"CharacterID: '{line.characterID}'. " +
                                   $"Primary characters must have a valid representation. Skipping line.");
                    _currentLineIndex++;
                    continue;
                }

                // Get localized dialogue
                var localizedResult = LocalizationHandler.GetLocalizedDialogue(line);
                if (!localizedResult.Success)
                {
                    Debug.LogError(localizedResult.ErrorMessage);
                    _currentLineIndex++;
                    continue;
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

                // Check and get the player placeholder name and replace with the player's name in the line
                string finalOutputString = ReplacePlayerNameInDialogueLine(localizedResult.Text);

                // Play audio and display dialogue
                yield return StartCoroutine(PlayAudioClipWithAction(line.clip));
                yield return StartCoroutine(
                    PlayDialogueLine(
                        ConversationUI,
                        line,
                        finalOutputString,
                        primaryProfile.CharacterName,
                        primaryRepresentation,primaryProfile // Primary character should always have a representation
                    )
                );

                // Actions after the dialogue line
                if (line.ActionsAfterDialogueLine != null && line.ActionsAfterDialogueLine.Count > 0)
                {
                    yield return StartCoroutine(ConversationData.DoActionsAfterDialogueLine(this, line));
                }

                // Handle line progression method
                if (line.UserInputMethod == ConvoCoreConversationData.DialogueLineProgressionMethod.Timed)
                {
                    yield return new WaitForSeconds(line.TimeBeforeNextLine);
                }
                else
                {
                    yield return StartCoroutine(ConversationUI.WaitForUserInput());
                }

                _currentLineIndex++;
            }

            // End conversation
            CurrentDialogueState = ConversationState.Completed;
            CompletedConversation?.Invoke();
            if (ConversationUI != null)
            {
                ConversationUI.HideDialogue();
            }
            Debug.Log("Conversation completed!");
        }

        /// <summary>
        /// Helper method to get primary character representation (special handling for speakers)
        /// </summary>
        /// <param name="primaryProfile">The character profile of the speaker</param>
        /// <param name="representationData">The representation data from the dialogue line</param>
        private CharacterRepresentationBase GetPrimaryCharacterRepresentation(ConvoCoreCharacterProfileBaseData primaryProfile, ConvoCoreConversationData.CharacterRepresentationData representationData)
        {
            CharacterRepresentationBase result = null;
            
            // For primary characters, if no specific representation is set, try to get the first available one
            if (string.IsNullOrEmpty(representationData.SelectedRepresentationName) && 
                representationData.SelectedRepresentation == null &&
                string.IsNullOrEmpty(representationData.SelectedCharacterID))
            {
                // This looks like an uninitialized primary character representation
                // Try to get the first available representation from the primary character's profile
                if (primaryProfile.Representations != null && primaryProfile.Representations.Count > 0)
                {
                    result = primaryProfile.Representations[0].CharacterRepresentationType;
                    if (result != null)
                    {
                        Debug.LogWarning($"Auto-assigning first available representation '{primaryProfile.Representations[0].CharacterRepresentationName}' from profile '{primaryProfile.CharacterName}' for primary character.");
                        return result;
                    }
                }
                
                Debug.LogError($"Primary character '{primaryProfile.CharacterName}' has no available representations. Please ensure the character profile has at least one representation assigned.");
                return null;
            }
            
            // Use the standard resolution method for other cases
            return GetCharacterRepresentationFromData(primaryProfile, representationData, true);
        }

        /// <summary>
        /// Helper method to get character representation from representation data
        /// </summary>
        /// <param name="profile">The character profile to get representation from</param>
        /// <param name="representationData">The representation data</param>
        /// <param name="isPrimaryCharacter">Whether this is for a primary character (primary characters cannot be "None")</param>
        private CharacterRepresentationBase GetCharacterRepresentationFromData(ConvoCoreCharacterProfileBaseData profile, ConvoCoreConversationData.CharacterRepresentationData representationData, bool isPrimaryCharacter = false)
        {
            CharacterRepresentationBase result = null;
            
            // Check if this is using the new secondary/tertiary system (has SelectedCharacterID)
            if (!string.IsNullOrEmpty(representationData.SelectedCharacterID))
            {
                // Find the profile by the selected character ID
                var selectedProfile = ConversationData.ConversationParticipantProfiles
                    .FirstOrDefault(p => p != null && p.CharacterID == representationData.SelectedCharacterID);
                
                if (selectedProfile != null)
                {
                    if (!string.IsNullOrEmpty(representationData.SelectedRepresentationName))
                    {
                        result = selectedProfile.GetRepresentation(representationData.SelectedRepresentationName);
                        if (result != null)
                        {
                            return result;
                        }
                        Debug.LogWarning($"Representation '{representationData.SelectedRepresentationName}' not found in profile '{selectedProfile.CharacterName}'. Trying fallbacks.");
                        
                        // Fallback: Try to get the first available representation from the selected profile
                        if (selectedProfile.Representations != null && selectedProfile.Representations.Count > 0)
                        {
                            result = selectedProfile.Representations[0].CharacterRepresentationType;
                            if (result != null)
                            {
                                Debug.LogWarning($"Using first available representation from profile '{selectedProfile.CharacterName}'.");
                                return result;
                            }
                        }
                    }
                    else
                    {
                        // Handle "None" case for secondary/tertiary characters only
                        if (!isPrimaryCharacter)
                        {
                            return null; // Valid "None" selection for secondary/tertiary
                        }
                        else
                        {
                            Debug.LogError($"Primary character cannot have 'None' representation. Profile: '{selectedProfile.CharacterName}'");
                            return null;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Profile with CharacterID '{representationData.SelectedCharacterID}' not found.");
                    return null;
                }
            }
            else
            {
                // Handle primary character or fallback to the old system
                if (profile != null)
                {
                    // Check if this is an intentional "None" selection
                    bool isIntentionalNone = string.IsNullOrEmpty(representationData.SelectedRepresentationName) &&
                                            representationData.SelectedRepresentation == null;
                    
                    if (isIntentionalNone)
                    {
                        if (!isPrimaryCharacter)
                        {
                            // This is valid for secondary/tertiary characters
                            return null;
                        }
                        else
                        {
                            // This is NOT valid for primary characters - they must have a representation
                            Debug.LogError($"Primary character '{profile.CharacterName}' cannot have 'None' representation. The primary character must have a valid representation as they are the speaker.");
                            
                            // Try to auto-assign the first available representation as a fallback
                            if (profile.Representations != null && profile.Representations.Count > 0)
                            {
                                result = profile.Representations[0].CharacterRepresentationType;
                                if (result != null)
                                {
                                    Debug.LogWarning($"Auto-assigning first available representation from profile '{profile.CharacterName}' as fallback.");
                                    return result;
                                }
                            }
                            
                            return null; // No valid representation found
                        }
                    }
                    
                    // Try to get representation by name from the provided profile
                    if (!string.IsNullOrEmpty(representationData.SelectedRepresentationName))
                    {
                        result = profile.GetRepresentation(representationData.SelectedRepresentationName);
                        if (result != null)
                        {
                            return result;
                        }
                        Debug.LogWarning($"Representation '{representationData.SelectedRepresentationName}' not found in profile '{profile.CharacterName}'. Trying fallbacks.");
                    }
                    
                    // Fallback: Use SelectedRepresentation if available (for backward compatibility)
                    if (representationData.SelectedRepresentation != null)
                    {
                        return representationData.SelectedRepresentation;
                    }
                    
                    // Fallback: Get the first available representation from the profile
                    if (profile.Representations != null && profile.Representations.Count > 0)
                    {
                        result = profile.Representations[0].CharacterRepresentationType;
                        if (result != null)
                        {
                            Debug.LogWarning($"Using first available representation from profile '{profile.CharacterName}'.");
                            return result;
                        }
                    }
                    
                    Debug.LogError($"No valid representations found for profile '{profile.CharacterName}'.");
                }
                else
                {
                    Debug.LogError("Profile is null, cannot get character representation.");
                }
            }
            
            return null;
        }
        /// <summary>
        /// Plays a dialogue line with the UI foundation
        /// </summary>
        private IEnumerator PlayDialogueLine(ConvoCoreUIFoundation uiFoundation,
            ConvoCoreConversationData.DialogueLineInfo dialogueLineInfo, string localizedText,
            string speakingCharacterName, CharacterRepresentationBase characterRepresentation,
            ConvoCoreCharacterProfileBaseData primaryProfile)
        {
            // Update the UI with dialogue information
            uiFoundation.UpdateDialogueUI(dialogueLineInfo, localizedText, speakingCharacterName, characterRepresentation,primaryProfile);
            
            yield return null; // Wait one frame for UI to update
        }
        /// <summary>
        /// Plays an audio clip if provided
        /// </summary>
        private IEnumerator PlayAudioClipWithAction(AudioClip clip)
        {
            if (clip != null)
            {
                // You can implement audio playing logic here
                // For now, just wait for the clip duration
                yield return new WaitForSeconds(clip.length);
            }
            yield return null;
        }
        /// <summary>
        /// Replaces player name placeholders in dialogue text
        /// </summary>
        private string ReplacePlayerNameInDialogueLine(string dialogueText)
        {
            if (string.IsNullOrEmpty(dialogueText))
                return dialogueText;

            var playerProfile = ConversationData.GetPlayerProfile();
            if (playerProfile != null)
            {
                return dialogueText.Replace("{PlayerName}", playerProfile.CharacterName);
            }
            
            return dialogueText.Replace("{PlayerName}", "Player");
        }
        /// <summary>
        /// Updates UI for language change
        /// </summary>
        public void UpdateUIForLanguage(string selectedLanguage)
        {
            Debug.Log($"Language updated to: {selectedLanguage}");
            
            // Update the localization handler if it exists
            if (LocalizationHandler != null)
            {
                LocalizationHandler = new ConvoCoreDialogueLocalizationHandler(ConvoCoreLanguageManager.Instance);
            }
            
            // If we're currently in an active conversation, refresh the current dialogue line
            if (CurrentDialogueState == ConversationState.Active && 
                ConversationData != null && 
                ConversationData.DialogueLines != null && 
                _currentLineIndex < ConversationData.DialogueLines.Count)
            {
                var currentLine = ConversationData.DialogueLines[_currentLineIndex];
                
                // Get the localized dialogue for the new language
                var localizedResult = LocalizationHandler.GetLocalizedDialogue(currentLine);
                if (localizedResult.Success)
                {
                    // Replace player name placeholders
                    string finalOutputString = ReplacePlayerNameInDialogueLine(localizedResult.Text);
                    
                    // Update the UI with both the new localized text and language code
                    if (ConversationUI != null)
                    {
                        ConversationUI.UpdateForLanguageChange(finalOutputString, selectedLanguage);
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to get localized dialogue for new language '{selectedLanguage}': {localizedResult.ErrorMessage}");
                }
            }
            else
            {
                // If no active conversation, just notify about the language change
                if (ConversationUI != null)
                {
                    ConversationUI.UpdateForLanguageChange(null, selectedLanguage);
                }
            }
        }

        /// <summary>
        /// Pauses the current conversation
        /// </summary>
        public void PauseConversation()
        {
            if (CurrentDialogueState == ConversationState.Active)
            {
                CurrentDialogueState = ConversationState.Paused;
                Debug.Log("Conversation paused.");
                PausedConversation?.Invoke();
            }
        }

        /// <summary>
        /// Resumes a paused conversation
        /// </summary>
        public void ResumeConversation()
        {
            if (CurrentDialogueState == ConversationState.Paused)
            {
                CurrentDialogueState = ConversationState.Active;
                Debug.Log("Conversation resumed.");
            }
        }

        /// <summary>
        /// Stops the current conversation
        /// </summary>
        public void StopConversation()
        {
            CurrentDialogueState = ConversationState.Inactive;
            EndedConversation?.Invoke();
            _currentLineIndex = 0;
            if (ConversationUI != null)
            {
                ConversationUI.HideDialogue();
            }
            Debug.Log("Conversation stopped.");
        }

        public void PlayConversation(ConvoCoreConversationData conversation)
        {
            if (conversation == null)
            {
                Debug.LogError("[ConvoCore] Play called with null conversation.");
                return;
            }
            ConversationData = conversation;
            ConversationData.InitializeDialogueData();
            if (ConversationUI != null)
            {
                ConversationUI.InitializeUI(this);
            }
            if (ConversationData == null)
            {
                Debug.LogError("ConversationData is not assigned!");
                return;
            }
            if (ConversationData.DialogueLines == null || ConversationData.DialogueLines.Count == 0)
            {
                Debug.LogError("No dialogue lines found in ConversationData!");
                return;
            }

            CurrentDialogueState = ConversationState.Active;
            _currentLineIndex = 0;
            StartedConversation?.Invoke();
            StartCoroutine(ExecuteDialogueSequence());
        }
        
        public void PlayConversation()
        {
            if (Input == null)
            {
                Debug.LogWarning("[ConvoCore] Input is null.");
                return;
            }

            // If Input is a SingleConversationInput with a valid conversation, route to the typed overload
            if (Input is SingleConversationInput sci && sci.Conversation != null)
            {
                PlayConversation(sci.Conversation);
                return;
            }

            // Otherwise let the input drive (e.g., ContainerInput starts its own sequencing coroutine)
            Input.Play(this, this);
        }

    }

    public interface IConvoCoreRunner
    {
        void PlayConversation(ConvoCoreConversationData conversation);
        public event Action CompletedConversation;

    }
}