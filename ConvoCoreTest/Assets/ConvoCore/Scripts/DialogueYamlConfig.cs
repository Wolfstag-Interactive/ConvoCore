using System.Collections.Generic;
using YamlDotNet.Serialization;

public class DialogueYamlConfig 
{
    [YamlMember(Alias = "CharacterID")]
    public string CharacterID { get; set; } 

    [YamlMember(Alias = "ConversationID")]
    public string ConversationID { get; set; } 

    [YamlMember(Alias = "AlternateRepresentation")]
    public string AlternateRepresentation { get; set; } 

    [YamlMember(Alias = "LocalizedDialogue")]
    public Dictionary<string, string> LocalizedDialogue { get; set; } = new Dictionary<string, string>();


}