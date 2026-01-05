using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL(
        "https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreYamlUtilities.html")]
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
        /// <summary>
        /// Imports dialogue metadata from the YAML file.
        /// Preserves Unity-authored per-line data by ConversationID+LineId (fallback to index for legacy).
        /// </summary>
        public void ImportFromYamlForKey(string conversationKey)
        {
            if (string.IsNullOrEmpty(_convoCoreConversationData.FilePath) &&
                _convoCoreConversationData.ConversationYaml == null)
            {
                Debug.LogError("filePath not set for the conversation and no ConversationYaml assigned!");
                return;
            }

            string yamlData = _convoCoreConversationData.ConversationYaml != null
                ? _convoCoreConversationData.ConversationYaml.text
                : ConvoCoreYamlLoader.Load(_convoCoreConversationData);
            if (string.IsNullOrEmpty(yamlData))
            {
                string sourcesMsg =
                    $"Checked direct TextAsset, persistent override, Addressables (if enabled), and Resources using FilePath='{_convoCoreConversationData.FilePath}'.";
                Debug.LogError($"YAML file not found. {sourcesMsg}");
                return;
            }
            
            Dictionary<string, List<DialogueYamlConfig>> dialoguesBySection;
            try
            {
                dialoguesBySection = ConvoCoreYamlParser.Parse(yamlData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse YAML file: {ex.Message}");
                return;
            }

            if (!dialoguesBySection.TryGetValue(conversationKey, out var yamlConfigs) || yamlConfigs == null)
            {
                Debug.LogError($"Conversation key '{conversationKey}' not found in YAML.");
                return;
            }

            bool idsAdded = false;
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < yamlConfigs.Count; i++)
            {
                var cfg = yamlConfigs[i];
                if (cfg == null) continue;

                if (string.IsNullOrWhiteSpace(cfg.LineID))
                {
                    cfg.LineID = ConvoCoreLineID.NewLineID();
                    idsAdded = true;
                }

                if (!seenIds.Add(cfg.LineID))
                {
                    Debug.LogError(
                        $"Duplicate LineId '{cfg.LineID}' detected in conversation '{conversationKey}'. " +
                        "LineIds must be unique. Fix the YAML and reimport.");
                    return;
                }
            }

#if UNITY_EDITOR
            if (idsAdded)
            {
                try
                {
                    string updatedYaml = ConvoCoreYamlSerializer.Serialize(dialoguesBySection);

                    bool wrote = false;
                    string assetPath = null;

                    if (!string.IsNullOrEmpty(_convoCoreConversationData.SourceYamlAssetPath))
                        assetPath = _convoCoreConversationData.SourceYamlAssetPath;
                    else if (_convoCoreConversationData.ConversationYaml != null)
                        assetPath = UnityEditor.AssetDatabase.GetAssetPath(_convoCoreConversationData.ConversationYaml);

                    // Writeback only for Assets paths. Never Packages.
                    if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/"))
                    {
                        var fullPath = System.IO.Path.GetFullPath(assetPath);
                        System.IO.File.WriteAllText(fullPath, updatedYaml);
                        UnityEditor.AssetDatabase.ImportAsset(assetPath);
                        wrote = true;
                    }

                    if (!wrote)
                    {
                        Debug.LogWarning(
                            "ConvoCore: LineId values were generated but could not be written back because the YAML source is not a writable Assets/ project file. " +
                            "To lock IDs (and enable safe CSV import), link a YAML asset under Assets/ or embed YAML into the Conversation asset.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ConvoCore: Failed to write back generated LineIds to YAML source. {ex.Message}");
                }
            }
#endif

            Debug.Log($"Importing {yamlConfigs.Count} lines for conversation key '{conversationKey}'.");

            if (_convoCoreConversationData.DialogueLines == null)
                _convoCoreConversationData.DialogueLines = new List<ConvoCoreConversationData.DialogueLineInfo>();

            var updatedDialogueLines = new List<ConvoCoreConversationData.DialogueLineInfo>(yamlConfigs.Count);

            for (int i = 0; i < yamlConfigs.Count; i++)
            {
                var yamlConfig = yamlConfigs[i] ?? new DialogueYamlConfig();

                var localizedDialogueList = new List<ConvoCoreConversationData.LocalizedDialogue>();
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

                // Preserve existing Unity-authored data by ConversationID+LineId (fallback: same index)
                var existing = FindExistingLine(_convoCoreConversationData, conversationKey, yamlConfig.LineID, i);

                var newLineInfo = new ConvoCoreConversationData.DialogueLineInfo(conversationKey)
                {
                    ConversationID = conversationKey,
                    ConversationLineIndex = i,
                    LineID = yamlConfig.LineID, // Your field name is LineID
                    characterID = yamlConfig.CharacterID,
                    LocalizedDialogues = localizedDialogueList,

                    // Preserve authored state
                    CharacterRepresentations = existing?.CharacterRepresentations != null
                        ? new List<ConvoCoreConversationData.CharacterRepresentationData>(existing
                            .CharacterRepresentations)
                        : new List<ConvoCoreConversationData.CharacterRepresentationData>(),

                    ActionsBeforeDialogueLine = existing?.ActionsBeforeDialogueLine != null
                        ? new List<BaseDialogueLineAction>(existing.ActionsBeforeDialogueLine)
                        : new List<BaseDialogueLineAction>(),

                    ActionsAfterDialogueLine = existing?.ActionsAfterDialogueLine != null
                        ? new List<BaseDialogueLineAction>(existing.ActionsAfterDialogueLine)
                        : new List<BaseDialogueLineAction>(),

                    clip = existing?.clip,

                    UserInputMethod = existing != null
                        ? existing.UserInputMethod
                        : ConvoCoreConversationData.DialogueLineProgressionMethod.UserInput,
                    TimeBeforeNextLine = existing != null ? existing.TimeBeforeNextLine : 0f,
                    LineContinuationSettings = existing != null
                        ? existing.LineContinuationSettings
                        : new ConvoCoreConversationData.LineContinuation
                        {
                            Mode = ConvoCoreConversationData.LineContinuationMode.Continue,
                            TargetAliasOrName = null,
                            TargetContainer = null,
                            PushReturnPoint = false
                        }
                };

                newLineInfo.EnsureCharacterRepresentationListInitialized();
                updatedDialogueLines.Add(newLineInfo);
            }

            _convoCoreConversationData.DialogueLines = updatedDialogueLines;
        }

        /// <summary>
        /// Searches for an existing dialogue line in the provided conversation data.
        /// Matches lines based on ConversationID and LineID if available, or falls back to the provided index.
        /// </summary>
        /// <param name="data">The conversation data containing a list of dialogue lines to search.</param>
        /// <param name="key">The unique identifier for the conversation to match.</param>
        /// <param name="lineId">The unique identifier for the specific line of dialogue. Can be null or empty.</param>
        /// <param name="indexFallback">The fallback index to use when LineID is not provided or no match is found by LineID.</param>
        /// <returns>
        /// The matching <c>DialogueLineInfo</c> instance if found, or <c>null</c> if no match is found.
        /// </returns>
        private static ConvoCoreConversationData.DialogueLineInfo FindExistingLine(
            ConvoCoreConversationData data, string key, string lineId, int indexFallback)
        {
            if (data?.DialogueLines == null) return null;

            if (!string.IsNullOrEmpty(lineId))
            {
                for (int i = 0; i < data.DialogueLines.Count; i++)
                {
                    var dl = data.DialogueLines[i];
                    if (dl.ConversationID == key && dl.LineID == lineId)
                        return dl;
                }
            }

            for (int i = 0; i < data.DialogueLines.Count; i++)
            {
                var dl = data.DialogueLines[i];
                if (dl.ConversationID == key && dl.ConversationLineIndex == indexFallback)
                    return dl;
            }
            return null;
        }
    }
}