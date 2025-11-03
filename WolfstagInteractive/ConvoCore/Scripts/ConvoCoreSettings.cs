using UnityEngine;
using System.Collections.Generic;

namespace WolfstagInteractive.ConvoCore
{
    public enum TextSourceKind
    {
        AssignedTextAsset,
        Persistent,
        Addressables,
        Resources
    }

    [CreateAssetMenu(fileName = "ConvoCoreSettings", menuName = "ConvoCore/Settings")]
    public sealed class ConvoCoreSettings : ScriptableObject
    {
        [Header("Order the sources to try (first hit wins)")]
        public TextSourceKind[] SourceOrder = new[]
        {
            TextSourceKind.AssignedTextAsset,
            TextSourceKind.Persistent,
            TextSourceKind.Addressables,
            TextSourceKind.Resources
        };

        [Header("Resources")] public string resourcesRoot = "ConvoCore/Dialogue"; // only used if FilePath given
        [Header("Language Settings")]
        [Tooltip("List of supported language codes (e.g., 'en', 'fr', 'es')")]
        public List<string> SupportedLanguages = new List<string> { "EN" };
        [Tooltip("Currently active language code")]
        public string CurrentLanguage = "EN";
        [Header("Addressables (optional)")] public bool AddressablesEnabled = false; // flip on when project uses it
        public string AddressablesKeyTemplate = "{filePath}.yml"; // maps FilePath -> key
        public bool VerboseLogs = false;
        private static ConvoCoreSettings _instance;

        public static ConvoCoreSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadInstance();
                }
                return _instance;
            }
        }
#if UNITY_EDITOR
        // Optional editor-only check
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            _instance = LoadInstance();
        }
#endif
        private static ConvoCoreSettings LoadInstance()
        {
            // 1. Try already loaded asset
            if (_instance != null)
                return _instance;

            // 2. Try from Resources folder (recommended for builds)
            var loaded = Resources.Load<ConvoCoreSettings>("ConvoCoreSettings");
            if (loaded != null)
            {
                _instance = loaded;
                return _instance;
            }

#if UNITY_EDITOR
            // 3. Try find it anywhere in the project (Editor only)
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ConvoCoreSettings");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<ConvoCoreSettings>(path);
                if (loaded != null)
                {
                    _instance = loaded;
                    return _instance;
                }
            }

            // 4. Create one automatically if none exists
            _instance = CreateInstance<ConvoCoreSettings>();
            string assetPath = "Assets/Resources/ConvoCoreSettings.asset";
            UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.LogWarning($"Created default ConvoCoreSettings at {assetPath}");
            return _instance;
#else
        Debug.LogError("ConvoCoreSettings not found in Resources! Please create one in the Editor.");
        return ScriptableObject.CreateInstance<ConvoCoreSettings>();
#endif
        }
        /// <summary>
        /// Validates that CurrentLanguage is in the SupportedLanguages list
        /// </summary>
        private void OnValidate()
        {
            // Ensure we have at least one language
            if (SupportedLanguages == null || SupportedLanguages.Count == 0)
            {
                SupportedLanguages = new List<string> { "EN" };
            }

            // If current language is not in supported languages, reset to first
            if (string.IsNullOrEmpty(CurrentLanguage) || 
                !SupportedLanguages.Exists(lang => string.Equals(lang, CurrentLanguage, System.StringComparison.OrdinalIgnoreCase)))
            {
                CurrentLanguage = SupportedLanguages[0];
            }
            CleanRendererProfiles();
        }
        // ------------------------------
        // Dialogue History Renderers
        // ------------------------------
        [Tooltip("List of available renderer profiles for dialogue history UI.")]
        [SerializeField] private List<ConvoCoreHistoryRendererProfile> historyRendererProfiles = new();

        public IReadOnlyList<ConvoCoreHistoryRendererProfile> HistoryRendererProfiles => historyRendererProfiles;

        /// <summary>
        /// Returns a renderer profile by its display name.
        /// </summary>
        public ConvoCoreHistoryRendererProfile GetRendererProfile(string rendererName)
        {
            foreach (var p in historyRendererProfiles)
                if (p != null && p.RendererName == rendererName)
                    return p;
            return null;
        }

        /// <summary>
        /// Adds a new profile if it doesn't already exist in the list.
        /// </summary>
        public void AddRendererProfile(ConvoCoreHistoryRendererProfile profile)
        {
            if (profile != null && !historyRendererProfiles.Contains(profile))
                historyRendererProfiles.Add(profile);
        }

        /// <summary>
        /// Removes null or missing profile references.
        /// </summary>
        public void CleanRendererProfiles()
        {
            historyRendererProfiles.RemoveAll(p => p == null);
        }
    }
}