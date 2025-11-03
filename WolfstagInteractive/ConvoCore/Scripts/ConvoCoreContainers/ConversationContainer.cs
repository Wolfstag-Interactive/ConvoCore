using System.Collections.Generic;
using UnityEngine;
namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConversationContainer.html")]
    [CreateAssetMenu(menuName = "ConvoCore/Conversation Container")]
    public sealed class ConversationContainer : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry
        {
            public string Alias;
            public ConvoCoreConversationData ConversationData;
            public bool Enabled = true;
            public float DelayAfterEndSeconds = 0f;
            public string[] Tags;
        }

        public List<Entry> Conversations = new();
        public bool Loop = false;
        public string DefaultStart;
    }
    

}