using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1ConvoCoreEditor.html")]
[CustomEditor(typeof(ConvoCore))]
    public class ConvoCoreEditor : UnityEditor.Editor
    {
        private ConvoCoreLanguageManager _convoCoreLanguageManager;
        private int _selectedLanguageIndex = 0;

        public override void OnInspectorGUI()
        {
            // Get the target object (DialogueStateMachine)
            ConvoCore convoCore = (ConvoCore)target;

            // Access the LanguageManager Singleton instance
            _convoCoreLanguageManager = ConvoCoreLanguageManager.Instance;

            // Check if the LanguageManager is initialized
            if (_convoCoreLanguageManager == null || _convoCoreLanguageManager.GetSupportedLanguages() == null)
            {
                EditorGUILayout.HelpBox(
                    "LanguageSettings is not found. Please check Assets/Resources for the LanguageSettings " +
                    "and ensure it exists at the root of the folder and that it contains at least one language code " +
                    "or create one below.",
                    MessageType.Error);
                EditorGUILayout.Space();
                if (GUILayout.Button("Create LanguageSettings Object"))
                {
                   ConvoCoreEditorUtilities.CreateLanguageSettingsAsset();
                }
                return; // Prevent further rendering if the LanguageManager is invalid
            }
            DrawDefaultInspector();

            // Display the current language
            EditorGUILayout.LabelField("Current Language:", _convoCoreLanguageManager.CurrentLanguage);

            // Display dropdown to select a language
            var supportedLanguages = _convoCoreLanguageManager.GetSupportedLanguages();
            if (supportedLanguages != null && supportedLanguages.Count > 0)
            {
                // Synchronize the dropdown selection with the current language
                if (_selectedLanguageIndex < 0 || _selectedLanguageIndex >= supportedLanguages.Count)
                {
                    // Default to the current language if the index is invalid
                    _selectedLanguageIndex = supportedLanguages.IndexOf(_convoCoreLanguageManager.CurrentLanguage);
                }

                // Render the dropdown and track changes
                int newSelectedIndex = EditorGUILayout.Popup("Select a Language:", _selectedLanguageIndex,
                    supportedLanguages.ToArray());
                if (newSelectedIndex != _selectedLanguageIndex)
                {
                    Debug.Log(
                        $"Dropdown selection changed to index: {newSelectedIndex}. Language: {supportedLanguages[newSelectedIndex]}");
                    _selectedLanguageIndex = newSelectedIndex; // Update index
                }

                // Add a button to apply the selected language
                if (GUILayout.Button("Apply Language"))
                {
                    var selectedLanguage = supportedLanguages[_selectedLanguageIndex];
                    if (_convoCoreLanguageManager.CurrentLanguage != selectedLanguage)
                    {
                        // Update the language in the LanguageManager
                        _convoCoreLanguageManager.SetLanguage(selectedLanguage);
                        Debug.Log($"Language applied via button: {selectedLanguage}");

                        // Notify the DialogueStateMachine to update its UI for the new language
                        convoCore.UpdateUIForLanguage(selectedLanguage);
                    }
                    else
                    {
                        Debug.Log("Selected language is already active.");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No supported languages found! Please check the LanguageSettings asset.",
                    MessageType.Warning);
            }

            // Add a button to start the conversation
            if (GUILayout.Button("Start Conversation"))
            {
                // Call StartConversation on the DialogueStateMachine
                convoCore.StartConversation();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Conversation State:", convoCore.CurrentDialogueState.ToString());

        }
    }
}