using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Centralized YAML -> object parse for ConvoCore.
    /// - Parses your schema: Dictionary&lt;string, List&lt;DialogueYamlConfig&gt;&gt;.
    /// - Normalizes locale keys so lookups are case-insensitive (EN/en/en-US).
    /// </summary>
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreYamlParser.html")]
    public static class ConvoCoreYamlParser
    {
        // Build the YamlDotNet deserializer once
        private static readonly IDeserializer _deserializer = new DeserializerBuilder().Build();

        /// <summary>
        /// Parse YAML text to Dictionary{conversationKey -> List of DialogueYamlConfig}.
        /// Also normalizes LocalizedDialogue keys to lower-invariant and uses a case-insensitive map.
        /// </summary>
        public static Dictionary<string, List<DialogueYamlConfig>> Parse(string yamlText)
        {
            if (string.IsNullOrWhiteSpace(yamlText))
                return new Dictionary<string, List<DialogueYamlConfig>>();

            var dict = _deserializer
                           .Deserialize<Dictionary<string, List<DialogueYamlConfig>>>(yamlText)
                       ?? new Dictionary<string, List<DialogueYamlConfig>>();

            // Normalize locale keys for each conversation list
            foreach (var kv in dict)
                NormalizeLocales(kv.Value);

            return dict;
        }

        /// <summary>Safe parse that won’t throw. Returns false and sets error on failure.</summary>
        public static bool TryParse(string yamlText, out Dictionary<string, List<DialogueYamlConfig>> result, out string error)
        {
            try
            {
                result = Parse(yamlText);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                result = null;
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Convenience: enumerate available conversation keys from a parsed dict.</summary>
        public static IEnumerable<string> GetConversationKeys(Dictionary<string, List<DialogueYamlConfig>> dict)
            => (IEnumerable<string>)dict?.Keys ?? Array.Empty<string>();

        /// <summary>
        /// Lowercases/normalizes LocalizedDialogue keys and swaps in a case-insensitive dictionary.
        /// </summary>
        private static void NormalizeLocales(List<DialogueYamlConfig> list)
        {
            if (list == null) return;

            foreach (var cfg in list)
            {
                if (cfg?.LocalizedDialogue == null) continue;

                var norm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in cfg.LocalizedDialogue)
                {
                    if (string.IsNullOrEmpty(p.Key)) continue;
                    var key = p.Key.Trim().ToLowerInvariant();
                    norm[key] = p.Value;
                }
                cfg.LocalizedDialogue = norm;
            }
        }
    }
}