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

        [Header("Debug")] public bool VerboseLogs = false;
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
        }
    }
}