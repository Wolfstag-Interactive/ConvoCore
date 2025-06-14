using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
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
            if (_convoCoreLanguageSettings != null &&
                _convoCoreLanguageSettings.SupportedLanguages.IndexOf(newLanguage) != -1)
            {
                CurrentLanguage = newLanguage;
                Debug.Log($"Language set to: {newLanguage}");
                OnLanguageChanged?.Invoke(newLanguage);
            }
            else
            {
                Debug.LogWarning($"'{newLanguage}' is not a supported language.");
            }
        }
    }
}