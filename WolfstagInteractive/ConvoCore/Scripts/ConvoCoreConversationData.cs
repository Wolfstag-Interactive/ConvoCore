using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using YamlDotNet.Serialization;
using System.IO;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "ConvoCoreData",
        menuName = "ConvoCore/ConversationDialogueObject")]
    public class ConvoCoreConversationData : ScriptableObject
    {
        public List<ConvoCoreCharacterProfileBaseData> ConversationParticipantProfiles =
            new List<ConvoCoreCharacterProfileBaseData>();

        [Serializable]
        public struct LocalizedDialogue
        {
            public string Language;
            public string Text;
        }

        [Serializable]
        public enum DialogueLineProgressionMethod
        {
            UserInput,
            Timed
        }

        [Serializable]
        public struct DialogueLines
        {
            public string ConversationID; // Key or ConversationID in the YAML
            public int ConversationLineIndex; // Line index within the conversation
            public string characterID; //ID of the character speaking the line
            public string AlternateRepresentation;

            [Tooltip("Specify the emotion for this dialogue line by name.")]
            public string SelectedEmotionName; // Name of the emotion selected via dropdown

            public List<LocalizedDialogue> LocalizedDialogues; // Localized dialogues per language
            public AudioClip clip; // Audio associated with the line
            public List<BaseAction> ActionsBeforeDialogueLine; // Actions before the dialogue line
            public List<BaseAction> ActionsAfterDialogueLine; // Actions after the dialogue line

            public DialogueLineProgressionMethod
                UserInputMethod; // Whether to wait for user input before continuing to the next line

            public float TimeBeforeNextLine; // Time in seconds to wait before continuing to the next line
        }

        public List<DialogueLines> dialogueLines; // Metadata for all dialogues in the YAML

        [Tooltip("Specify the YAML file path from StreamingAssets.")]
        public string FilePath; // Path to the YAML file

        [Tooltip("Define the unique key for the conversation.")]
        public string ConversationKey; // Add this field to hold the key

        private Dictionary<string, List<DialogueYamlConfig>> _dialogueDataByKey; // Stored YAML data at runtime

        public ConvoCoreConversationData()
        {
            ConvoCoreYamlUtilities = new ConvoCoreYamlUtilities(this);
        }

        public ConvoCoreYamlUtilities ConvoCoreYamlUtilities { get; }

        public ConvoCoreCharacterProfileBaseData ResolveCharacterProfile(
            List<ConvoCoreCharacterProfileBaseData> profiles, string characterID)
        {
            if (profiles == null || string.IsNullOrEmpty(characterID))
            {
                Debug.LogWarning("CharacterID is missing or no profiles are available.");
                return null;
            }

            foreach (var profile in profiles)
            {
                if (profile.CharacterID == characterID)
                {
                    return profile;
                }
            }

            Debug.LogWarning($"Profile not found for CharacterID: {characterID}");
            return null; // Profile not found
        }

        /// <summary>
        /// Initializes the YAML runtime data.
        /// Should be called before trying to fetch runtime dialogue text.
        /// </summary>
        public void InitializeDialogueData()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                Debug.LogError("filePath not set for the conversation!");
                return;
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, FilePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"YAML file not found at: {fullPath}");
                return;
            }

            var deserializer = new DeserializerBuilder().Build();
            string yamlData = File.ReadAllText(fullPath);
            try
            {
                _dialogueDataByKey = deserializer.Deserialize<Dictionary<string, List<DialogueYamlConfig>>>(yamlData);
                Debug.Log($"Successfully loaded YAML data. Found {_dialogueDataByKey.Count} conversation sections.");

                for (int i = 0; i < dialogueLines.Count; i++)
                {
                    var currentLine = dialogueLines[i];

                    if (_dialogueDataByKey.TryGetValue(currentLine.ConversationID, out var configList))
                    {
                        // Get the config at the same index as our dialogue line
                        var matchingConfig = (currentLine.ConversationLineIndex < configList.Count)
                            ? configList[currentLine.ConversationLineIndex]
                            : null;

                        if (matchingConfig != null && matchingConfig.LocalizedDialogue != null)
                        {
                            // Initialize or update the list of localized dialogues
                            List<LocalizedDialogue> localizedDialogueList = new List<LocalizedDialogue>();
                            foreach (var kvp in matchingConfig.LocalizedDialogue)
                            {
                                localizedDialogueList.Add(new LocalizedDialogue
                                {
                                    Language = kvp.Key,
                                    Text = kvp.Value
                                });
                            }

                            // Update the dialogue line with localization data
                            var dialogueLine = dialogueLines[i];
                            dialogueLine.LocalizedDialogues = localizedDialogueList;
                            dialogueLines[i] = dialogueLine;

                            Debug.Log($"Updated line {i} with {localizedDialogueList.Count} translations");
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"No matching config found at index {currentLine.ConversationLineIndex} for conversation {currentLine.ConversationID}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No config list found for ConversationID: '{currentLine.ConversationID}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize dialogue data: {ex.Message}\n{ex.StackTrace}");
                _dialogueDataByKey = null;
            }
        }

        private string ReplacePlayerNameInDialogueLine(string dataDialogue)
        {
            string playerStringSymbolToReplace = "<PlayerName>";
            string playerChosenName = "";
            return dataDialogue.Replace(playerStringSymbolToReplace, playerChosenName);
        }

        public IEnumerator ActionsBeforeDialogueLine(ConvoCore core, DialogueLines line)
        {
            foreach (BaseAction action in line.ActionsBeforeDialogueLine)
            {
                if (action != null)
                {
                    Debug.Log("Doing " + action.name);
                    //create copy of action object while retaining the originals values
                    var actionInstance = Instantiate(action);
                    //feed the copy to coroutine runner to do the action
                    yield return core.StartCoroutine(actionInstance.DoAction());
                    Debug.Log("Action fired: " + action.name);
                    //dispose of the action after doing it
                    DestroyImmediate(actionInstance);
                }
                else
                {
                    Debug.LogError("Line " + line.ConversationLineIndex + " has null action");
                }
            }
        }

        public IEnumerator DoActionsAfterDialogueLine(ConvoCore core, DialogueLines line)
        {
            foreach (BaseAction action in line.ActionsAfterDialogueLine)
            {
                if (action != null)
                {
                    Debug.Log("Doing " + action.name);
                    //create copy of action object while retaining the originals values
                    var actionInstance = Instantiate(action);
                    //feed the copy to coroutine runner to do the action
                    yield return core.StartCoroutine(actionInstance.DoAction());
                    Debug.Log("Action fired: " + action.name);
                    //dispose of the action after doing it
                    DestroyImmediate(actionInstance);
                }
                else
                {
                    Debug.LogError("Line " + line.ConversationLineIndex + " has null action");
                }
            }
        }

    }
}