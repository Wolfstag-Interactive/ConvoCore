using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;

namespace WolfstagInteractive.ConvoCore
{
    public class ConvoCoreYamlUtilities
    {
        private readonly ConvoCoreConversationData _convoCoreConversationData;

        public ConvoCoreYamlUtilities(ConvoCoreConversationData convoCoreConversationData)
        {
            _convoCoreConversationData = convoCoreConversationData;
        }

        /// <summary>
        /// Imports dialogue metadata from the YAML file.
        /// Keeps actions intact for existing keys/indices during reimport.
        /// </summary>
        public void ImportFromYamlForKey(string conversationKey)
        {
            if (string.IsNullOrEmpty(_convoCoreConversationData.FilePath))
            {
                Debug.LogError("filePath not set for the conversation!");
                return;
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, _convoCoreConversationData.FilePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"YAML file not found at: {fullPath}");
                return;
            }

            // Deserialize YAML content
            var deserializer = new DeserializerBuilder().Build();
            string yamlData = File.ReadAllText(fullPath);

            Dictionary<string, List<DialogueYamlConfig>> dialoguesBySection;
            try
            {
                dialoguesBySection = deserializer.Deserialize<Dictionary<string, List<DialogueYamlConfig>>>(yamlData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse YAML file: {ex.Message}");
                return;
            }

            if (!dialoguesBySection.TryGetValue(conversationKey, out var yamlConfigs))
            {
                Debug.LogError($"Conversation key '{conversationKey}' not found in YAML.");
                return;
            }

            // Get the dialogue lines for the specified key
            Debug.Log($"Importing {yamlConfigs.Count} lines for conversation key '{conversationKey}'.");
            if (_convoCoreConversationData.DialogueLines == null)
            {
                _convoCoreConversationData.DialogueLines = new List<ConvoCoreConversationData.DialogueLineInfo>();
            }

            // Temporary list for the updated dialogue lines
            List<ConvoCoreConversationData.DialogueLineInfo> updatedDialogueLines = new List<ConvoCoreConversationData.DialogueLineInfo>();

            for (int i = 0; i < yamlConfigs.Count; i++)
            {
                DialogueYamlConfig yamlConfig = yamlConfigs[i];

                // Map the localized dialogue dictionary to a list
                List<ConvoCoreConversationData.LocalizedDialogue> localizedDialogueList = new List<ConvoCoreConversationData.LocalizedDialogue>();
                foreach (var kvp in yamlConfig.LocalizedDialogue)
                {
                    localizedDialogueList.Add(new ConvoCoreConversationData.LocalizedDialogue
                    {
                        Language = kvp.Key,
                        Text = kvp.Value
                    });
                }

                // Create a new DialogueLine and map the fields correctly
                ConvoCoreConversationData.DialogueLineInfo newLineInfo = new ConvoCoreConversationData.DialogueLineInfo
                {
                    ConversationID = conversationKey,
                    characterID = yamlConfig.CharacterID,
                    ConversationLineIndex = i,
                    LocalizedDialogues = localizedDialogueList,
                    ActionsBeforeDialogueLine = new List<BaseAction>(),
                    ActionsAfterDialogueLine = new List<BaseAction>(),
                    clip = null
                };

                updatedDialogueLines.Add(newLineInfo);
            }

            // Replace the dialogueLines with the updated list
            _convoCoreConversationData.DialogueLines = updatedDialogueLines;
        }
    }
}