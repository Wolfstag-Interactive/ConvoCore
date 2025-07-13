using System;
using System.Linq;

namespace WolfstagInteractive.ConvoCore
{
    public class ConvoCoreDialogueLocalizationHandler
    {
        private readonly ConvoCoreLanguageManager _convoCoreLanguageManager;

        /// <summary>
        /// Constructor requires a LanguageManager instance for dependency injection.
        /// </summary>
        /// <param name="convoCoreLanguageManager">Singleton instance of LanguageManager</param>
        public ConvoCoreDialogueLocalizationHandler(ConvoCoreLanguageManager convoCoreLanguageManager)
        {
            _convoCoreLanguageManager = convoCoreLanguageManager ?? throw new ArgumentNullException(nameof(convoCoreLanguageManager));
        }

        /// <summary>
        /// Gets localized text for a dialogue line, handling fallbacks and logging.
        /// </summary>
        public LocalizedDialogueResult GetLocalizedDialogue(ConvoCoreConversationData.DialogueLineInfo lineInfo)
        {
            if (lineInfo.LocalizedDialogues == null || lineInfo.LocalizedDialogues.Count == 0)
            {
                return new LocalizedDialogueResult
                {
                    Success = false,
                    Text = "[Error: Missing Translations]",
                    ErrorMessage =
                        $"LocalizedDialogues is null or empty for line {lineInfo.ConversationLineIndex} in '{lineInfo.ConversationID}'"
                };
            }

            var languageManager = ConvoCoreLanguageManager.Instance;
            string currentLanguage = languageManager.CurrentLanguage;

            // Try to find the translation for the current language.
            var localizedDialogue = lineInfo.LocalizedDialogues
                .FirstOrDefault(ld => ld.Language == currentLanguage);

            if (localizedDialogue.Language != null)
            {
                return new LocalizedDialogueResult
                {
                    Success = true,
                    Text = localizedDialogue.Text,
                    UsedLanguage = currentLanguage
                };
            }

            // Try the fallback language if current language doesn't exist.
            if (currentLanguage != languageManager.CurrentLanguage)
            {
                var fallbackDialogue = lineInfo.LocalizedDialogues
                    .FirstOrDefault(ld => ld.Language == languageManager.CurrentLanguage);

                if (fallbackDialogue.Language != null)
                {
                    return new LocalizedDialogueResult
                    {
                        Success = true,
                        Text = fallbackDialogue.Text,
                        UsedLanguage = languageManager.CurrentLanguage,
                        IsFallback = true,
                        ErrorMessage =
                            $"Missing {currentLanguage} translation, using {languageManager.CurrentLanguage} fallback"
                    };
                }
            }

            // If no specific language is available, use the first available translation.
            var defaultDialogue = lineInfo.LocalizedDialogues.FirstOrDefault();

            if (defaultDialogue.Language != null)
            {
                return new LocalizedDialogueResult
                {
                    Success = true,
                    Text = defaultDialogue.Text,
                    UsedLanguage = defaultDialogue.Language,
                    IsFallback = true,
                    ErrorMessage =
                        $"Missing both {currentLanguage} and {languageManager.CurrentLanguage} translations. Using {defaultDialogue.Language}"
                };
            }

            // If we reach here, there are no translations available.
            return new LocalizedDialogueResult
            {
                Success = false,
                Text = "[Missing Translation]",
                ErrorMessage =
                    $"No translations available for line {lineInfo.ConversationLineIndex} in '{lineInfo.ConversationID}'"
            };
        }
    }

    public class LocalizedDialogueResult
    {
        public bool Success { get; set; }
        public string Text { get; set; }
        public string UsedLanguage { get; set; }
        public bool IsFallback { get; set; }
        public string ErrorMessage { get; set; }
    }
}