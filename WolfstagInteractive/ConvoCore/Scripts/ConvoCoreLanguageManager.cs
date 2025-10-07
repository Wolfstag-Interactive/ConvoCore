using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreLanguageManager.html")]
    public class ConvoCoreLanguageManager
    {
        private static ConvoCoreLanguageManager _instance;
        public static ConvoCoreLanguageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConvoCoreLanguageManager();
                    _instance.Initialize();
                }
                return _instance;
            }
        }
    
        // Allow for loader injection; fallback to a default resource-based loader.
        public IConvoCoreLanguageSettingsLoader LanguageSettingsLoader { get; set; } = new ConvoCoreLanguageSettingsLoader();
    
        private ConvoCoreLanguageSettings _convoCoreLanguageSettings;
    
        public string CurrentLanguage { get; private set; }
        public static Action<string> OnLanguageChanged { get; set; }
    
        private ConvoCoreLanguageManager() {}
    
        private void Initialize()
        {
            _convoCoreLanguageSettings = LanguageSettingsLoader.LoadLanguageSettings();
            if (_convoCoreLanguageSettings == null)
            {
                Debug.LogError("LanguageSettings could not be loaded. Ensure the asset exists and the loader is set up correctly.");
                return;
            }
    
            CurrentLanguage = _convoCoreLanguageSettings.SupportedLanguages[0];
            Debug.Log($"LanguageManager initialized with default language: {CurrentLanguage}");
        }
    
        public List<string> GetSupportedLanguages()
        {
            return _convoCoreLanguageSettings != null ? _convoCoreLanguageSettings.SupportedLanguages : null;
        }
    
        public void SetLanguage(string newLanguage)
        {
            if (_convoCoreLanguageSettings == null || _convoCoreLanguageSettings.SupportedLanguages == null)
            {
                Debug.LogWarning("Language settings are not loaded.");
                return;
            }

            // case-insensitive match, but keep the project's canonical casing
            var match = _convoCoreLanguageSettings.SupportedLanguages
                .Find(l => string.Equals(l?.Trim(), newLanguage?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(match))
            {
                CurrentLanguage = match; // keep canonical casing from settings
                Debug.Log($"Language set to: {CurrentLanguage}");
                OnLanguageChanged?.Invoke(CurrentLanguage);
            }
            else
            {
                Debug.LogWarning($"'{newLanguage}' is not a supported language.");
            }
        }

    }
}