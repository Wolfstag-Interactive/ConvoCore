using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WolfstagInteractive.ConvoCore
{
    public static class ConvoCoreYamlSerializer
    {
        private static readonly ISerializer _serializer = new SerializerBuilder().Build();

        public static string Serialize(Dictionary<string, List<DialogueYamlConfig>> dict)
        {
            dict ??= new Dictionary<string, List<DialogueYamlConfig>>();
            return _serializer.Serialize(dict);
        }
    }
}