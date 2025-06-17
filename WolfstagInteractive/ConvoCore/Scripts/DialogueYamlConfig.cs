using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WolfstagInteractive.ConvoCore
{
    public class DialogueYamlConfig 
    {
        [YamlMember(Alias = "CharacterID")]
        public string CharacterID { get; set; } 
        [YamlMember(Alias = "ConversationID")]
        public string ConversationID { get; set; } 
        [YamlMember(Alias = "LocalizedDialogue")]
        public Dictionary<string, string> LocalizedDialogue { get; set; } = new Dictionary<string, string>();
    }
}