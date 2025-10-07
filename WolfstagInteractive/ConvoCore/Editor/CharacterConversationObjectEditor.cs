using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;
using System.Linq;
using System.Collections.Generic;
namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1CharacterConversationObjectEditor.html")]
[CustomEditor(typeof(ConvoCoreConversationData))]
    public class CharacterConversationObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty _conversationKey;
        private SerializedProperty _filePath;

        private SerializedProperty _conversationYaml;
        private SerializedProperty _sourceYaml;
        private SerializedProperty _sourceYamlAssetPath;

        public override void OnInspectorGUI()
        {
            // Update the serialized object
            serializedObject.Update();

            // Initialize required serialized properties
            _filePath                 = serializedObject.FindProperty("FilePath");
            _conversationKey          = serializedObject.FindProperty("ConversationKey");
            _conversationYaml         = serializedObject.FindProperty("ConversationYaml");
            _sourceYaml               = serializedObject.FindProperty("SourceYaml");
            _sourceYamlAssetPath      = serializedObject.FindProperty("SourceYamlAssetPath");

            // Track if any changes are made for validation
            EditorGUI.BeginChangeCheck();

            // Draw properties using custom iteration to handle overrides
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true);

            do
            {
                // Skip custom-handled properties
                if (property.name == "FilePath" ||
                    property.name == "ConversationKey" ||
                    property.name == "ConversationYaml" ||
                    property.name == "SourceYaml" ||
                    property.name == "SourceYamlAssetPath")
                    continue;

                // Default property field rendering
                EditorGUILayout.PropertyField(property, true);
            }
            while (property.NextVisible(false));

            // Draw 'FilePath' field with the browse button
            DrawFilePathField();

            // Draw 'ConversationKey' field with the import button
            DrawConversationKeyField();

            // If any changes were made, validate the data
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                var conversationData = (ConvoCoreConversationData)target;
                conversationData.ValidateAndFixDialogueLines();
                EditorUtility.SetDirty(target);
            }
            DrawLanguagePreviewSection();

            // Add validation tools section
            DrawValidationToolsSection();

            // YAML Linking (hidden intermediary workflow)
            DrawYamlLinkingSection();

            // Apply any modified properties
            serializedObject.ApplyModifiedProperties();
        }
// --- NEW: Language Preview (Editor-only) ---
private void DrawLanguagePreviewSection()
{
    EditorGUILayout.Space();
    EditorGUILayout.BeginVertical("box");
    EditorGUILayout.LabelField("Language (Preview)", EditorStyles.boldLabel);

    // Load settings via your loader (Resources/LanguageSettings)
    var loader   = new WolfstagInteractive.ConvoCore.ConvoCoreLanguageSettingsLoader();
    var settings = loader.LoadLanguageSettings();

    var supported = (settings?.SupportedLanguages ?? new List<string> { "en" })
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (supported.Count == 0) supported.Add("en");

    // Display names: uppercase for readability, but keep originals to pass through
    var display = supported.Select(s => s.ToUpperInvariant()).ToArray();

    var lm = WolfstagInteractive.ConvoCore.ConvoCoreLanguageManager.Instance;
    var current = lm.CurrentLanguage ?? "en";
    var idx = supported.FindIndex(s => string.Equals(s, current, StringComparison.OrdinalIgnoreCase));
    if (idx < 0) idx = 0;

    var newIdx = EditorGUILayout.Popup("Preview Language", idx, display);
    if (newIdx != idx)
    {
        lm.SetLanguage(supported[newIdx]); // pass original, manager handles casing
        Repaint();
    }

    // Optional: show which locales exist in the embedded YAML (to avoid surprises)
    var data = (WolfstagInteractive.ConvoCore.ConvoCoreConversationData)target;
    var yamlLocales = TryGetLocalesFromEmbedded(data);
    if (yamlLocales != null && yamlLocales.Count > 0)
    {
        EditorGUILayout.LabelField("Locales in YAML:", string.Join(", ", yamlLocales));
        if (!yamlLocales.Any(k => string.Equals(k, supported[newIdx], StringComparison.OrdinalIgnoreCase)))
        {
            EditorGUILayout.HelpBox(
                "Selected language not found in YAML; runtime will fall back (e.g., to 'en').",
                MessageType.Info);
        }
    }

    EditorGUILayout.EndVertical();
    EditorGUILayout.Space();
}

// Parse the embedded YAML to list locale keys present (case-insensitive, de-duplicated)
private static List<string> TryGetLocalesFromEmbedded(ConvoCoreConversationData data)
{
    try
    {
        if (data == null || data.ConversationYaml == null) return null;
        var yamlText = data.ConversationYaml.text;
        if (string.IsNullOrEmpty(yamlText)) return null;

        // Uses your runtime parser (already normalizes keys to case-insensitive dictionary)
        var dict = ConvoCoreYamlParser.Parse(yamlText);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in dict)
        {
            var list = kv.Value;
            if (list == null) continue;

            foreach (var cfg in list)
            {
                if (cfg?.LocalizedDialogue == null) continue;
                foreach (var lang in cfg.LocalizedDialogue.Keys)
                {
                    if (!string.IsNullOrWhiteSpace(lang))
                        set.Add(lang.Trim());
                }
            }
        }

        return set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }
    catch
    {
        return null; // keep inspector resilient
    }
}

        private void DrawYamlLinkingSection()
        {
            // Safety: if these props don't exist (older data class), skip drawing the section
            if (_sourceYaml == null || _conversationYaml == null || _sourceYamlAssetPath == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("YAML Linking (Recommended)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Link a plain .yaml file you edit normally. ConvoCore will embed its text as a sub-asset used at runtime. " +
                "This avoids StreamingAssets/Resources requirements and won’t conflict with other YAML tools.",
                MessageType.Info);

            // Source .yaml (DefaultAsset or TextAsset)
            EditorGUILayout.PropertyField(_sourceYaml, new GUIContent("Source .yaml (Editor-only)"));

            // Show current embedded TextAsset (read-only)
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_conversationYaml, new GUIContent("Embedded YAML (used at runtime)"));
            }

            // Show linked path (if any)
            if (!string.IsNullOrEmpty(_sourceYamlAssetPath.stringValue))
            {
                EditorGUILayout.LabelField("Linked Path", _sourceYamlAssetPath.stringValue, EditorStyles.miniLabel);
            }
            // Calculate button width to make all buttons equal
            float buttonWidth = (EditorGUIUtility.currentViewWidth - 20f) / 2f;

            // Buttons row
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(_conversationYaml.objectReferenceValue == null ? "Link & Embed" : "Sync From Source", GUILayout.Height(22),
                    GUILayout.Width(buttonWidth)))
            {
                var data = (ConvoCoreConversationData)target;
                if (_sourceYaml.objectReferenceValue == null)
                {
                    Debug.LogError("Please assign a Source .yaml asset to link.");
                }
                else
                {
                    var srcObj  = _sourceYaml.objectReferenceValue;
                    var srcPath = AssetDatabase.GetAssetPath(srcObj);

                    // Only allow .yml/.yaml or TextAsset
                    var isYamlExt  = srcPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                                     srcPath.EndsWith(".yml",  StringComparison.OrdinalIgnoreCase);
                    var isTextAsset = srcObj is TextAsset;

                    if (!isYamlExt && !isTextAsset)
                    {
                        Debug.LogError("Source must be a .yaml/.yml file or a TextAsset.");
                    }
                    else
                    {
                        // Use the new method that returns the embedded TextAsset
                        var embeddedAsset = TryEmbedFromPath(data, srcPath);
                        if (embeddedAsset != null)
                        {
                            _sourceYamlAssetPath.stringValue = srcPath;
                            
                            // Assign the embedded asset directly to the SerializedProperty
                            _conversationYaml.objectReferenceValue = embeddedAsset;
                            
                            // Apply all changes
                            serializedObject.ApplyModifiedProperties();

                            Debug.Log($"ConvoCore: Embedded YAML text from '{srcPath}' into '{AssetDatabase.GetAssetPath(data)}'.");
                        }
                    }
                }
            }

            using (new EditorGUI.DisabledScope(_sourceYaml.objectReferenceValue == null))
            {
                if (GUILayout.Button("Ping Source", GUILayout.Height(22),GUILayout.ExpandWidth(true)))
                {
                    EditorGUIUtility.PingObject(_sourceYaml.objectReferenceValue);
                }
            }

            EditorGUILayout.EndHorizontal();

            // Second row of buttons with uniform width
            EditorGUILayout.BeginHorizontal();

            // Clear Link button - use ExpandWidth to match first row
            using (new EditorGUI.DisabledScope(_sourceYaml.objectReferenceValue == null && _conversationYaml.objectReferenceValue == null))
            {
                if (GUILayout.Button("Clear Link (keeps embedded)", GUILayout.Height(18), GUILayout.Width(buttonWidth)))
                {
                    _sourceYaml.objectReferenceValue = null;
                    _sourceYamlAssetPath.stringValue = string.Empty;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // Clear Embedded button - use ExpandWidth to match first row
            using (new EditorGUI.DisabledScope(_conversationYaml.objectReferenceValue == null))
            {
                if (GUILayout.Button("Clear Embedded", GUILayout.Height(18), GUILayout.Width(buttonWidth)))
                {
                    if (EditorUtility.DisplayDialog("Clear Embedded YAML", 
                        "Are you sure you want to remove the embedded YAML? This will delete the embedded TextAsset permanently.", 
                        "Yes, Remove", "Cancel"))
                    {
                        var data = (ConvoCoreConversationData)target;
                        ClearEmbeddedYaml(data);
                        
                        // Update the SerializedProperty to reflect the change
                        _conversationYaml.objectReferenceValue = null;
                        serializedObject.ApplyModifiedProperties();
                        
                        Debug.Log("ConvoCore: Cleared embedded YAML.");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }


        private static void ClearEmbeddedYaml(ConvoCoreConversationData data)
        {
            // Path of the Conversation asset
            var convPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(convPath))
            {
                Debug.LogError("ConvoCore: Could not resolve asset path for Conversation asset.");
                return;
            }

            // Remove the current embedded TextAsset from the field
            if (data.ConversationYaml != null)
            {
                Object.DestroyImmediate(data.ConversationYaml, true);
                data.ConversationYaml = null;
            }

            // Also clean up any stray "EmbeddedYaml" sub-assets
            var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(convPath);
            if (reps != null)
            {
                foreach (var rep in reps)
                {
                    if (rep is TextAsset repTA && repTA.name == "EmbeddedYaml")
                    {
                        Object.DestroyImmediate(repTA, true);
                    }
                }
            }

            // Mark dirty and save
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            
            // Force reimport to ensure changes are visible immediately
            AssetDatabase.ImportAsset(convPath, ImportAssetOptions.ForceUpdate);
        }

        private static TextAsset TryEmbedFromPath(ConvoCoreConversationData data, string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath)) return null;

            // Load text either from TextAsset or via File
            string text = null;
            var srcObj = AssetDatabase.LoadAssetAtPath<Object>(sourcePath);
            if (srcObj is TextAsset ta) text = ta.text;
            else                        text = File.ReadAllText(sourcePath);
            if (string.IsNullOrEmpty(text)) return null;

            // Path of the Conversation asset we will embed into
            var convPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(convPath))
            {
                Debug.LogError("ConvoCore: Could not resolve asset path for Conversation asset.");
                return null;
            }

            // --- Clean up any existing embedded assets first ---
            // Remove whatever the field currently points to
            if (data.ConversationYaml != null)
            {
                Object.DestroyImmediate(data.ConversationYaml, true);
                data.ConversationYaml = null;
            }

            // Remove any stray "EmbeddedYaml" sub-assets
            var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(convPath);
            if (reps != null)
            {
                foreach (var rep in reps)
                {
                    if (rep is TextAsset repTA && repTA.name == "EmbeddedYaml")
                    {
                        Object.DestroyImmediate(repTA, true);
                    }
                }
            }

            // Save to ensure cleanup is committed
            AssetDatabase.SaveAssets();

            // --- Create new embedded TextAsset ---
            var embedded = new TextAsset(text) { name = "EmbeddedYaml" };
            
            // Add as sub-asset
            AssetDatabase.AddObjectToAsset(embedded, data);

            // Optional: auto-fill FilePath for persistent/Addressables fallbacks if empty
            if (string.IsNullOrEmpty(data.FilePath))
            {
                var baseName = Path.GetFileNameWithoutExtension(sourcePath);
                data.FilePath = $"ConvoCore/Dialogue/{baseName}";
            }

            // Mark dirty and save
            EditorUtility.SetDirty(embedded);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            
            // Force reimport to ensure the sub-asset is properly recognized
            AssetDatabase.ImportAsset(convPath, ImportAssetOptions.ForceUpdate);
            
            // Return the embedded asset so we can assign it to the SerializedProperty
            return embedded;
        }
        /// Draws validation tools section with buttons for manual validation
        private void DrawValidationToolsSection()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Validation Tools", EditorStyles.boldLabel);
    
            EditorGUILayout.HelpBox(
                "Use these tools to validate and debug your dialogue data. " +
                "The validation will automatically fix missing primary character representations.",
                MessageType.Info
            );

            // Create a horizontal layout for the buttons
            EditorGUILayout.BeginHorizontal();

            // Validate and Fix button
            if (GUILayout.Button("Validate & Fix All Dialogue Lines", GUILayout.Height(25)))
            {
                var conversationData = (ConvoCoreConversationData)target;
                conversationData.ValidateAndFixDialogueLines();
                EditorUtility.SetDirty(target);
                Debug.Log($"Manual validation completed for {conversationData.name}");
            }

            // Debug profiles button
            if (GUILayout.Button("Debug Character Profiles", GUILayout.Height(25)))
            {
                var conversationData = (ConvoCoreConversationData)target;
                conversationData.DebugCharacterProfiles();
            }

            EditorGUILayout.EndHorizontal();

            // Second row of buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload YAML Now",GUILayout.Height(25)))
            {
                var data = (ConvoCoreConversationData)target;
                data.InitializeDialogueData();           // uses the embedded TextAsset first
                EditorUtility.SetDirty(target);
                Debug.Log($"Reloaded YAML for '{data.name}'.");
            }
            // Sync object references button
            if (GUILayout.Button("Sync Object References", GUILayout.Height(25)))
            {
                var conversationData = (ConvoCoreConversationData)target;
                conversationData.SyncAllRepresentationObjectReferences();
                EditorUtility.SetDirty(target);
                Debug.Log($"Object reference sync completed for {conversationData.name}");
            }

            // Force save button
            if (GUILayout.Button("Force Save Asset", GUILayout.Height(25)))
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                Debug.Log($"Forced save completed for {target.name}");
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        /// <summary>
        /// Draws the File Path field with browse functionality.
        /// </summary>
        private void DrawFilePathField()
        {
            // Section header
            EditorGUILayout.LabelField("YML Dialogue Data File Path", EditorStyles.boldLabel);

            // Description
            EditorGUILayout.HelpBox(
                "Specify the path to the YML file containing conversation data. " +
                "Ensure this file exists inside the /StreamingAssets/ folder.",
                MessageType.Info
            );

            // Draw the FilePath property field
            EditorGUILayout.PropertyField(_filePath, new GUIContent("File Path"));

            // Browse button to load file path
            if (GUILayout.Button("Browse YML File"))
            {
                // Open file panel for YML files
                string filePath = EditorUtility.OpenFilePanel("Select YML File", Application.streamingAssetsPath, "yml");
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Ensure the file resides in the StreamingAssets folder
                    if (filePath.StartsWith(Application.streamingAssetsPath))
                    {
                        _filePath.stringValue = filePath.Substring(Application.streamingAssetsPath.Length + 1); // Relative path
                        serializedObject.ApplyModifiedProperties(); // Apply changes
                    }
                    else
                    {
                        Debug.LogError("Selected file must reside inside the StreamingAssets folder.");
                    }
                }
            }
        }

        /// <summary>
        /// Draws the Conversation Key field with an import button.
        /// </summary>
        private void DrawConversationKeyField()
        {
            // Section header
            EditorGUILayout.LabelField("Conversation Key", EditorStyles.boldLabel);

            // Description
            EditorGUILayout.HelpBox(
                "Specify the unique identifier for the conversation. " +
                "Ensure it matches an existing key in your YML data file.",
                MessageType.Info
            );

            // Draw the ConversationKey property field
            EditorGUILayout.PropertyField(_conversationKey, new GUIContent("Conversation Key"));

            // Button to import data using the ConversationKey
            if (GUILayout.Button("Import From YAML For Key"))
            {
                // Ensure the ConversationKey is not empty
                if (string.IsNullOrEmpty(_conversationKey.stringValue))
                {
                    Debug.LogError("Please provide a valid conversation key.");
                }
                else
                {
                    // Safely import the conversation using the provided key
                    ConvoCoreConversationData obj = (ConvoCoreConversationData)target;
                    obj.ConvoCoreYamlUtilities.ImportFromYamlForKey(_conversationKey.stringValue);
                    
                    // After import, validate the data
                    obj.ValidateAndFixDialogueLines();
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}