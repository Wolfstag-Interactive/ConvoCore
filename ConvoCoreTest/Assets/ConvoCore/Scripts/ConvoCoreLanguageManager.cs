using System;
using System.Collections.Generic;
using UnityEngine;

namespace CyberRift.NewDialogueTool
{
    public class ConvoCoreLanguageManager
    {
        private static ConvoCoreLanguageManager _instance;
    
        // Public static property to access the Singleton instance
        public static ConvoCoreLanguageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConvoCoreLanguageManager(); // Create the instance if it doesn't already exist
                    _instance.Initialize(); // Perform lazy initialization
                }
                return _instance;
            }
        }
    
        private ConvoCoreLanguageSettings _convoCoreLanguageSettings;
    
        // Current language
        public string CurrentLanguage { get; private set; }
        public static Action<string> OnLanguageChanged { get; set; }

        // Private constructor to prevent external instantiation
        private ConvoCoreLanguageManager() { }
    
        // Initialization logic for lazy loading
        private void Initialize()
        {
            // Load LanguageSettings resource
            _convoCoreLanguageSettings = Resources.Load<ConvoCoreLanguageSettings>("LanguageSettings");
            if (_convoCoreLanguageSettings == null)
            {
                Debug.LogError("LanguageSettings could not be found in Resources. Ensure the asset exists.");
                return;
            }
    
            // Set default language
            CurrentLanguage = _convoCoreLanguageSettings.SupportedLanguages[0];
            Debug.Log($"LanguageManager initialized with default language: {CurrentLanguage}");
        }
    
        // Get list of supported languages
        public List<string> GetSupportedLanguages()
        {
            return _convoCoreLanguageSettings != null ? _convoCoreLanguageSettings.SupportedLanguages : null;
        }
    
        // Set the current language
        public void SetLanguage(string newLanguage)
        {
            if (_convoCoreLanguageSettings != null && _convoCoreLanguageSettings.SupportedLanguages.IndexOf(newLanguage) != -1)
            {
                CurrentLanguage = newLanguage;
                Debug.Log($"Language set to: {newLanguage}");

                // Trigger the event and pass the new language
                OnLanguageChanged?.Invoke(newLanguage);
            }
            else
            {
                Debug.LogWarning($"'{newLanguage}' is not a supported language.");
            }
        }

    }
}