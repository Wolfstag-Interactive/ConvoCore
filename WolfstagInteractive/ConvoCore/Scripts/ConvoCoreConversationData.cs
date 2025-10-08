using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
#if UNITY_EDITOR
using WolfstagInteractive.ConvoCore.Editor;
#endif
namespace WolfstagInteractive.ConvoCore
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreConversationData.html")]
[CreateAssetMenu(fileName = "ConvoCoreData",
        menuName = "ConvoCore/ConversationDialogueObject")]
    public partial class ConvoCoreConversationData : ScriptableObject
    {
        public List<ConvoCoreCharacterProfileBaseData> ConversationParticipantProfiles =
            new List<ConvoCoreCharacterProfileBaseData>();
        
        public List<DialogueLineInfo> DialogueLines; // Metadata for all dialogues in the YAML

        [Header("YAML Source (pick one or more)")]
        public TextAsset ConversationYaml; // sample-friendly direct reference

        public bool AllowPersistentOverrides = true; // enable device-side hotfixes
        [Tooltip("Resources path without extension, e.g. ConvoCore/Dialogue/ForestIntro")]
        public string FilePath;
#if UNITY_EDITOR
        [HideInInspector] public UnityEngine.Object SourceYaml; // .yaml or TextAsset
        [HideInInspector] public string SourceYamlAssetPath; // AssetDatabase path for auto-sync
#endif
        [Tooltip("Define the unique key for the conversation.")]
        public string ConversationKey; // Add this field to hold the key

        private Dictionary<string, List<DialogueYamlConfig>> _dialogueDataByKey; // Stored YAML data at runtime

        public ConvoCoreConversationData()
        {
            ConvoCoreYamlUtilities = new ConvoCoreYamlUtilities(this);
        }

        public ConvoCoreYamlUtilities ConvoCoreYamlUtilities { get; }

        // OnValidate is called whenever the object is loaded or a value is changed in the inspector
        private void OnValidate()
        {
            ValidateAndFixDialogueLines();
        }

        /// <summary>
        /// Validates and fixes dialogue line data to ensure proper serialization
        /// </summary>
        public void ValidateAndFixDialogueLines()
        {
            if (DialogueLines == null) return;

            bool verboseLogs = ConvoCoreYamlLoader.Settings?.VerboseLogs ?? false;

            if (verboseLogs)
                Debug.Log($"=== Starting Dialogue Validation for {name} ===");
            
            bool madeChanges = false;

            for (int i = 0; i < DialogueLines.Count; i++)
            {
                var line = DialogueLines[i];
                if (line == null) continue;

                // Validate primary character representation
                if (ValidatePrimaryCharacterRepresentation(line, i))
                {
                    madeChanges = true;
                }

                // Validate secondary and tertiary character representations
                ValidateSecondaryTertiaryRepresentation(line.SecondaryCharacterRepresentation, "Secondary", i);
                ValidateSecondaryTertiaryRepresentation(line.TertiaryCharacterRepresentation, "Tertiary", i);

                // Ensure conversation ID and line index are set
                if (string.IsNullOrEmpty(line.ConversationID))
                {
                    line.ConversationID = ConversationKey;
                    madeChanges = true;
                }

                if (line.ConversationLineIndex != i)
                {
                    line.ConversationLineIndex = i;
                    madeChanges = true;
                }
            }

            if (madeChanges)
            {
                if (verboseLogs)
                    Debug.Log($"Validation completed with automatic fixes applied to {name}.");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
            else
            {
                if (verboseLogs)
                    Debug.Log($"Validation completed - no changes needed for {name}.");
            }
        }
        /// <summary>
        /// Validates primary character representation (must have a valid representation)
        /// </summary>
private bool ValidatePrimaryCharacterRepresentation(DialogueLineInfo line, int lineIndex)
        {
            bool verboseLogs = ConvoCoreYamlLoader.Settings?.VerboseLogs ?? false;

            if (verboseLogs)
                Debug.Log($"Validating line {lineIndex}: CharacterID='{line.characterID}'");

            if (string.IsNullOrEmpty(line.characterID))
            {
                if (verboseLogs)
                    Debug.LogWarning($"Line {lineIndex}: CharacterID is not set for the speaking character.");
                return false;
            }

            var speakerProfile = ResolveCharacterProfile(ConversationParticipantProfiles, line.characterID);
            if (speakerProfile == null)
            {
                if (verboseLogs)
                    Debug.LogWarning($"Line {lineIndex}: No profile found for CharacterID '{line.characterID}'.");
                return false;
            }

            if (verboseLogs)
            {
                Debug.Log($"Line {lineIndex}: Found profile '{speakerProfile.CharacterName}' with {speakerProfile.Representations?.Count ?? 0} representations");

                // Check current state of primary representation
                Debug.Log($"Line {lineIndex}: Current PrimaryCharacterRepresentation state:");
                Debug.Log($"  - SelectedRepresentationName: '{line.PrimaryCharacterRepresentation.SelectedRepresentationName}'");
                Debug.Log($"  - SelectedRepresentation: {(line.PrimaryCharacterRepresentation.SelectedRepresentation != null ? "NOT NULL" : "NULL")}");
                Debug.Log($"  - SelectedCharacterID: '{line.PrimaryCharacterRepresentation.SelectedCharacterID}'");
            }

            // Check if primary representation needs fixing
            bool needsAutoFix = string.IsNullOrEmpty(line.PrimaryCharacterRepresentation.SelectedRepresentationName) &&
                                line.PrimaryCharacterRepresentation.SelectedRepresentation == null &&
                                string.IsNullOrEmpty(line.PrimaryCharacterRepresentation.SelectedCharacterID);

            if (verboseLogs)
                Debug.Log($"Line {lineIndex}: NeedsAutoFix = {needsAutoFix}");

            if (needsAutoFix)
            {
                // Auto-assign the first available representation for the primary character
                if (speakerProfile.Representations != null && speakerProfile.Representations.Count > 0)
                {
                    var firstRep = speakerProfile.Representations[0];
                    
                    if (verboseLogs)
                        Debug.Log($"Line {lineIndex}: First representation found: CharacterRepresentationName='{firstRep.CharacterRepresentationName}', Object={(firstRep.CharacterRepresentationType != null ? "NOT NULL" : "NULL")}");

                    // Set both the name and the object reference
                    line.PrimaryCharacterRepresentation.SelectedRepresentationName = firstRep.CharacterRepresentationName;
                    line.PrimaryCharacterRepresentation.SelectedRepresentation = firstRep.CharacterRepresentationType;

                    if (verboseLogs)
                        Debug.Log($"Line {lineIndex}: Auto-assigned primary representation '{firstRep.CharacterRepresentationName}' for character '{speakerProfile.CharacterName}'.");

                    return true; // Changes were made
                }
                else
                {
                    if (verboseLogs)
                        Debug.LogWarning($"Line {lineIndex}: Character '{speakerProfile.CharacterName}' has no available representations.");
                    return false;
                }
            }
            else
            {
                // Even if not auto-fixing, check if we need to sync the object reference
                bool needsSync = false;

                if (!string.IsNullOrEmpty(line.PrimaryCharacterRepresentation.SelectedRepresentationName) &&
                    line.PrimaryCharacterRepresentation.SelectedRepresentation == null)
                {
                    // We have a name but no object reference - try to resolve it
                    var representation = speakerProfile.GetRepresentation(line.PrimaryCharacterRepresentation.SelectedRepresentationName);
                    if (representation != null)
                    {
                        line.PrimaryCharacterRepresentation.SelectedRepresentation = representation;
                        if (verboseLogs)
                            Debug.Log($"Line {lineIndex}: Synced object reference for representation '{line.PrimaryCharacterRepresentation.SelectedRepresentationName}'.");
                        needsSync = true;
                    }
                    else
                    {
                        if (verboseLogs)
                            Debug.LogWarning($"Line {lineIndex}: Could not resolve representation '{line.PrimaryCharacterRepresentation.SelectedRepresentationName}' in profile '{speakerProfile.CharacterName}'.");
                    }
                }

                if (verboseLogs)
                    Debug.Log($"Line {lineIndex}: Primary representation appears to be already set, skipping auto-fix. Sync needed: {needsSync}");
                return needsSync;
            }
        }
        // <summary>
        /// Forces synchronization of object references for all dialogue lines that have representation names but missing object references
        /// </summary>
        [ContextMenu("Sync All Representation Object References")]
        public void SyncAllRepresentationObjectReferences()
        {
            bool verboseLogs = ConvoCoreYamlLoader.Settings?.VerboseLogs ?? false;

            if (verboseLogs)
                Debug.Log("=== Syncing All Representation Object References ===");
            
            bool madeChanges = false;

            if (DialogueLines == null) return;

            for (int i = 0; i < DialogueLines.Count; i++)
            {
                var line = DialogueLines[i];
                if (line == null) continue;

                // Sync primary character representation
                if (SyncRepresentationObjectReference(line.PrimaryCharacterRepresentation, line.characterID, i, "Primary"))
                {
                    madeChanges = true;
                }

                // Sync secondary character representation
                if (!string.IsNullOrEmpty(line.SecondaryCharacterRepresentation.SelectedCharacterID))
                {
                    if (SyncRepresentationObjectReference(line.SecondaryCharacterRepresentation,
                            line.SecondaryCharacterRepresentation.SelectedCharacterID, i, "Secondary"))
                    {
                        madeChanges = true;
                    }
                }

                // Sync tertiary character representation
                if (!string.IsNullOrEmpty(line.TertiaryCharacterRepresentation.SelectedCharacterID))
                {
                    if (SyncRepresentationObjectReference(line.TertiaryCharacterRepresentation,
                            line.TertiaryCharacterRepresentation.SelectedCharacterID, i, "Tertiary"))
                    {
                        madeChanges = true;
                    }
                }
            }

            if (madeChanges)
            {
                if (verboseLogs)
                    Debug.Log("Representation object reference sync completed with changes.");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }
            else
            {
                if (verboseLogs)
                    Debug.Log("Representation object reference sync completed - no changes needed.");
            }
        }
        /// <summary>
        /// Helper method to sync a single representation object reference
        /// </summary>
        private bool SyncRepresentationObjectReference(CharacterRepresentationData representationData,
            string characterID, int lineIndex, string type)
        {
            if (string.IsNullOrEmpty(representationData.SelectedRepresentationName) ||
                representationData.SelectedRepresentation != null)
            {
                return false; // Nothing to sync
            }

            bool verboseLogs = ConvoCoreYamlLoader.Settings?.VerboseLogs ?? false;

            var profile = ResolveCharacterProfile(ConversationParticipantProfiles, characterID);
            if (profile == null)
            {
                if (verboseLogs)
                    Debug.LogWarning($"Line {lineIndex}: Cannot sync {type} representation - profile not found for CharacterID '{characterID}'.");
                return false;
            }

            var representation = profile.GetRepresentation(representationData.SelectedRepresentationName);
            if (representation != null)
            {
                representationData.SelectedRepresentation = representation;
                if (verboseLogs)
                    Debug.Log($"Line {lineIndex}: Synced {type} representation object reference for '{representationData.SelectedRepresentationName}'.");
                return true;
            }
            else
            {
                if (verboseLogs)
                    Debug.LogWarning($"Line {lineIndex}: Could not find {type} representation '{representationData.SelectedRepresentationName}' in profile '{profile.CharacterName}'.");
                return false;
            }
        }

        /// <summary>
        /// Validates secondary/tertiary character representations (can be None)
        /// </summary>
        private void ValidateSecondaryTertiaryRepresentation(CharacterRepresentationData representationData,
            string type, int lineIndex)
        {
            bool verboseLogs = ConvoCoreYamlLoader.Settings?.VerboseLogs ?? false;

            // For secondary/tertiary characters, having no representation is valid (None selection)
            // But if they have a SelectedCharacterID, they should also have a SelectedRepresentationName (unless intentionally None)

            if (!string.IsNullOrEmpty(representationData.SelectedCharacterID))
            {
                var selectedProfile = ConversationParticipantProfiles
                    .FirstOrDefault(p => p != null && p.CharacterID == representationData.SelectedCharacterID);

                if (selectedProfile == null)
                {
                    if (verboseLogs)
                        Debug.LogWarning($"Line {lineIndex}: {type} character representation references unknown CharacterID '{representationData.SelectedCharacterID}'.");
                    return;
                }

                // If they selected a character but no representation name, it might be intentional "None"
                // or they might need a default representation
                if (string.IsNullOrEmpty(representationData.SelectedRepresentationName))
                {
                    // This is fine - it means "None" selection for secondary/tertiary characters
                    return;
                }

                // Validate that the selected representation exists
                var representation = selectedProfile.GetRepresentation(representationData.SelectedRepresentationName);
                if (representation == null)
                {
                    if (verboseLogs)
                        Debug.LogWarning($"Line {lineIndex}: {type} character representation '{representationData.SelectedRepresentationName}' not found in profile '{selectedProfile.CharacterName}'.");
                }
            }
        }

        /// <summary>
        /// Forces validation of dialogue lines (accessible from context menu)
        /// </summary>
        [ContextMenu("Force Validate Dialogue Lines")]
        public void ForceValidateDialogueLines()
        {
            bool verboseLogs = ConvoCoreYamlLoader.Settings?.VerboseLogs ?? false;
            
            if (verboseLogs)
                Debug.Log("Manually triggering dialogue line validation...");
            
            ValidateAndFixDialogueLines();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// Debug method to inspect character profiles structure
        /// </summary>
        [ContextMenu("Debug Character Profiles")]
        public void DebugCharacterProfiles()
        {
            Debug.Log("=== Character Profiles Debug ===");
            foreach (var profile in ConversationParticipantProfiles)
            {
                if (profile == null)
                {
                    Debug.Log("NULL PROFILE FOUND");
                    continue;
                }

                Debug.Log($"Profile: {profile.CharacterName} (ID: {profile.CharacterID})");
                Debug.Log($"  Representations count: {profile.Representations?.Count ?? 0}");

                if (profile.Representations != null)
                {
                    for (int i = 0; i < profile.Representations.Count; i++)
                    {
                        var rep = profile.Representations[i];
                        Debug.Log($"    [{i}] RepresentationName: '{rep.CharacterRepresentationName}'");
                        Debug.Log($"    [{i}] CharacterRepresentationName: '{rep.CharacterRepresentationName}'");
                        Debug.Log(
                            $"    [{i}] CharacterRepresentation: {(rep.CharacterRepresentationType != null ? rep.CharacterRepresentationType.GetType().Name : "NULL")}");
                    }
                }
            }
        }

        public ConvoCoreCharacterProfileBaseData ResolveCharacterProfile(
            List<ConvoCoreCharacterProfileBaseData> profiles, string characterID)
        {
            if (profiles == null || string.IsNullOrEmpty(characterID))
            {
                bool verboseLogs = ConvoCoreYamlLoader.Settings?.VerboseLogs ?? false;
                if (verboseLogs)
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

            bool verboseLogsEnabled = ConvoCoreYamlLoader.Settings?.VerboseLogs ?? false;
            if (verboseLogsEnabled)
                Debug.LogWarning($"Profile not found for CharacterID: {characterID}");
            return null; // Profile not found
        }

        /// <summary>
        /// Initializes the YAML runtime data.
        /// Should be called before trying to fetch runtime dialogue text.
        /// </summary>
        public void InitializeDialogueData()
        {
            //   Order (by ConvoCoreSettings): Assigned TextAsset → persistentDataPath → Addressables (if enabled) → Resources
            string yamlData = ConvoCoreYamlLoader.Load(this);
            if (string.IsNullOrEmpty(yamlData))
            {
                Debug.LogError(
                    $"YAML not found. Checked direct TextAsset, persistent override, Addressables (if enabled), and Resources using FilePath='{FilePath}'.");
                return;
            }

            try
            {
                _dialogueDataByKey = ConvoCoreYamlParser.Parse(yamlData);
                if (ConvoCoreYamlLoader.Settings?.VerboseLogs == true)
                {
                    Debug.Log(
                        $"Successfully loaded YAML data. Found {_dialogueDataByKey.Count} conversation sections.");
                }

                for (int i = 0; i < DialogueLines.Count; i++)
                {
                    var currentLine = DialogueLines[i];

                    if (_dialogueDataByKey.TryGetValue(currentLine.ConversationID, out var configList))
                    {
                        // Get the config at the same index as our dialogue line
                        var matchingConfig = (currentLine.ConversationLineIndex < configList.Count)
                            ? configList[currentLine.ConversationLineIndex]
                            : null;

                        if (matchingConfig is { LocalizedDialogue: not null })
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
                            var dialogueLine = DialogueLines[i];
                            dialogueLine.LocalizedDialogues = localizedDialogueList;
                            DialogueLines[i] = dialogueLine;
                            if (ConvoCoreYamlLoader.Settings?.VerboseLogs == true)
                            {
                                Debug.Log($"Updated line {i} with {localizedDialogueList.Count} translations");
                            }
                        }
                        else
                        {
                            if (ConvoCoreYamlLoader.Settings?.VerboseLogs == true)
                            {
                                Debug.LogWarning(
                                    $"No matching config found at index {currentLine.ConversationLineIndex} for conversation {currentLine.ConversationID}");
                            }
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

            foreach (var profile in ConversationParticipantProfiles)
            {
                foreach (var representationPair in profile.Representations)
                {
                    if (representationPair == null)
                    {
                        Debug.LogError($"Representation pair on profile: {profile.name} is null.", profile);
                        continue;
                    }

                    if (representationPair.CharacterRepresentationType == null)
                    {
                        Debug.LogError(
                            $"Representation pair on profile: {profile.name} has no CharacterRepresentationType set.",
                            profile);
                        continue;
                    }

                    if (representationPair.CharacterRepresentationType is IConvoCoreRepresentationInitializable
                        initializable)
                    {
                        initializable.Initialize();
                    }
                }
            }
        }

        // Finds the player's profile from the list based on the IsPlayer flag.
        public ConvoCoreCharacterProfileBaseData GetPlayerProfile()
        {
            return ConversationParticipantProfiles.FirstOrDefault(profile => profile.IsPlayerCharacter);
        }

        public IEnumerator ActionsBeforeDialogueLine(ConvoCore core, DialogueLineInfo lineInfo)
        {
            foreach (BaseAction action in lineInfo.ActionsBeforeDialogueLine)
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
                    Debug.LogError("Line " + lineInfo.ConversationLineIndex + " has null action");
                }
            }
        }

        public IEnumerator DoActionsAfterDialogueLine(ConvoCore core, DialogueLineInfo lineInfo)
        {
            foreach (BaseAction action in lineInfo.ActionsAfterDialogueLine)
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
                    Debug.LogError("Line " + lineInfo.ConversationLineIndex + " has null action");
                }
            }
        }
    }

}