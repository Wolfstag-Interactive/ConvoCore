using System;
using System.Collections.Generic;

namespace WolfstagInteractive.ConvoCore
{
    public class ConvoCoreDialogueLocalizationHandler
    {
        private readonly ConvoCoreLanguageManager _convoCoreLanguageManager;

        /// <summary>
        /// Constructor requires a LanguageManager instance for dependency injection.
        /// </summary>
        public ConvoCoreDialogueLocalizationHandler(ConvoCoreLanguageManager convoCoreLanguageManager)
        {
            _convoCoreLanguageManager = convoCoreLanguageManager ?? throw new ArgumentNullException(nameof(convoCoreLanguageManager));
        }

        /// <summary>
        /// Gets localized text for a dialogue line, with case-insensitive and base-locale fallback.
        /// Tries: exact -> base (fr-CA -> fr) -> "en" -> base("en") -> first available.
        /// </summary>
        public LocalizedDialogueResult GetLocalizedDialogue(ConvoCoreConversationData.DialogueLineInfo lineInfo)
        {
            if (lineInfo?.LocalizedDialogues == null || lineInfo.LocalizedDialogues.Count == 0)
            {
                return new LocalizedDialogueResult
                {
                    Success = false,
                    Text = "[Error: Missing Translations]",
                    ErrorMessage = $"LocalizedDialogues is null or empty for line {lineInfo?.ConversationLineIndex} in '{lineInfo?.ConversationID}'"
                };
            }

            // Build a case-insensitive map from the list
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ld in lineInfo.LocalizedDialogues)
            {
                if (!string.IsNullOrEmpty(ld.Language) && ld.Text != null)
                    map[ld.Language] = ld.Text; // last-in wins if duplicates
            }

            string requested = _convoCoreLanguageManager?.CurrentLanguage ?? "en";
            string fallback = "en";

            // 1) exact
            if (map.TryGetValue(requested, out var text))
            {
                return new LocalizedDialogueResult { Success = true, Text = text, UsedLanguage = requested };
            }

            // 2) base of requested (fr-CA -> fr)
            var baseReq = BaseLang(requested);
            if (!baseReq.Equals(requested, StringComparison.OrdinalIgnoreCase) &&
                map.TryGetValue(baseReq, out text))
            {
                return new LocalizedDialogueResult
                {
                    Success = true,
                    Text = text,
                    UsedLanguage = baseReq,
                    IsFallback = true,
                    ErrorMessage = $"Missing '{requested}', using '{baseReq}'."
                };
            }

            // 3) fallback ("en")
            if (map.TryGetValue(fallback, out text))
            {
                return new LocalizedDialogueResult
                {
                    Success = true,
                    Text = text,
                    UsedLanguage = fallback,
                    IsFallback = true,
                    ErrorMessage = $"Missing '{requested}', using '{fallback}'."
                };
            }

            // 4) base of fallback
            var baseFb = BaseLang(fallback);
            if (!baseFb.Equals(fallback, StringComparison.OrdinalIgnoreCase) &&
                map.TryGetValue(baseFb, out text))
            {
                return new LocalizedDialogueResult
                {
                    Success = true,
                    Text = text,
                    UsedLanguage = baseFb,
                    IsFallback = true,
                    ErrorMessage = $"Missing '{requested}', using '{baseFb}'."
                };
            }

            // 5) first available
            foreach (var kv in map)
            {
                return new LocalizedDialogueResult
                {
                    Success = true,
                    Text = kv.Value,
                    UsedLanguage = kv.Key,
                    IsFallback = true,
                    ErrorMessage = $"Missing '{requested}', using '{kv.Key}'."
                };
            }

            // Shouldn't get here, but be safe
            return new LocalizedDialogueResult
            {
                Success = false,
                Text = "[Missing Translation]",
                ErrorMessage = $"No translations available for line {lineInfo.ConversationLineIndex} in '{lineInfo.ConversationID}'"
            };
        }

        private static string BaseLang(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;
            var i = code.IndexOfAny(new[] { '-', '_' });
            return i > 0 ? code.Substring(0, i) : code;
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