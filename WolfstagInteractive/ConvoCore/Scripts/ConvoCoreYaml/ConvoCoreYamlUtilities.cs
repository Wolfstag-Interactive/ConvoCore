using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;

namespace WolfstagInteractive.ConvoCore
{
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreYamlUtilities.html")]
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
            // BEGIN: relaxed validation – allow either a direct TextAsset reference OR a FilePath
            if (string.IsNullOrEmpty(_convoCoreConversationData.FilePath) && _convoCoreConversationData.ConversationYaml == null)
            {
                Debug.LogError("filePath not set for the conversation and no ConversationYaml assigned!");
                return;
            }
            // END: relaxed validation

            // Load YAML text via the flexible loader:
            //   Order (configurable via ConvoCoreSettings): TextAsset -> persistentDataPath -> Addressables (optional) -> Resources
            string yamlData = ConvoCoreYamlLoader.Load(_convoCoreConversationData);
            if (string.IsNullOrEmpty(yamlData))
            {
                // Preserve original-style error messaging but reflect new source paths.
                string sourcesMsg = $"Checked direct TextAsset, persistent override, Addressables (if enabled), and Resources using FilePath='{_convoCoreConversationData.FilePath}'.";
                Debug.LogError($"YAML file not found. {sourcesMsg}");
                return;
            }

            // Deserialize YAML content
            var deserializer = new DeserializerBuilder().Build();

            Dictionary<string, List<DialogueYamlConfig>> dialoguesBySection;
            try
            {
                dialoguesBySection = deserializer.Deserialize<Dictionary<string, List<DialogueYamlConfig>>>(yamlData) ?? new();
                // Normalize locale keys once (so EN/en/en-US behave consistently)
                foreach (var kv in dialoguesBySection)
                    NormalizeLocales(kv.Value);
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
            List<ConvoCoreConversationData.DialogueLineInfo> updatedDialogueLines = new List<ConvoCoreConversationData.DialogueLineInfo>(yamlConfigs.Count);

            for (int i = 0; i < yamlConfigs.Count; i++)
            {
                DialogueYamlConfig yamlConfig = yamlConfigs[i];

                // Map the localized dialogue dictionary to a list
                List<ConvoCoreConversationData.LocalizedDialogue> localizedDialogueList = new List<ConvoCoreConversationData.LocalizedDialogue>();
                if (yamlConfig.LocalizedDialogue != null)
                {
                    foreach (var kvp in yamlConfig.LocalizedDialogue)
                    {
                        localizedDialogueList.Add(new ConvoCoreConversationData.LocalizedDialogue
                        {
                            Language = kvp.Key,
                            Text = kvp.Value
                        });
                    }
                }

                // Preserve existing actions/clip for the same key+index if present
                var existing = FindExistingLine(_convoCoreConversationData, conversationKey, i);

                // Create a new DialogueLine and map the fields correctly
                ConvoCoreConversationData.DialogueLineInfo newLineInfo = new ConvoCoreConversationData.DialogueLineInfo(conversationKey)
                {
                    ConversationID = conversationKey,
                    characterID = yamlConfig.CharacterID,
                    ConversationLineIndex = i,
                    LocalizedDialogues = localizedDialogueList,

                    // Preserve previously authored actions if they exist; otherwise keep empty lists
                    ActionsBeforeDialogueLine = existing?.ActionsBeforeDialogueLine != null
                        ? new List<BaseAction>(existing.ActionsBeforeDialogueLine)
                        : new List<BaseAction>(),

                    ActionsAfterDialogueLine = existing?.ActionsAfterDialogueLine != null
                        ? new List<BaseAction>(existing.ActionsAfterDialogueLine)
                        : new List<BaseAction>(),

                    // Preserve any previously assigned clip
                    clip = existing?.clip
                };

                updatedDialogueLines.Add(newLineInfo);
            }

            // Replace the dialogueLines with the updated list
            _convoCoreConversationData.DialogueLines = updatedDialogueLines;
        }

        // --- helpers ---

        private static ConvoCoreConversationData.DialogueLineInfo FindExistingLine(
            ConvoCoreConversationData data, string key, int index)
        {
            if (data?.DialogueLines == null) return null;
            for (int i = 0; i < data.DialogueLines.Count; i++)
            {
                var dl = data.DialogueLines[i];
                if (dl.ConversationID == key && dl.ConversationLineIndex == index)
                    return dl;
            }
            return null;
        }

        private static void NormalizeLocales(List<DialogueYamlConfig> list)
        {
            if (list == null) return;
            foreach (var cfg in list)
            {
                if (cfg?.LocalizedDialogue == null) continue;

                // Normalize locale keys to lower-invariant and ensure case-insensitive lookups
                var norm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in cfg.LocalizedDialogue)
                {
                    if (string.IsNullOrEmpty(p.Key)) continue;
                    var key = p.Key.Trim().ToLowerInvariant();
                    norm[key] = p.Value;
                }
                cfg.LocalizedDialogue = norm;
            }
        }
    }
}