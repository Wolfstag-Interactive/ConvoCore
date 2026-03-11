using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
    /// <summary>
    /// Serializable snapshot of the full game save state, including all conversation progress
    /// and global variables. Assembled and restored by <see cref="ConvoCoreSaveManager"/>
    /// via an <see cref="IConvoSaveProvider"/>.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1SaveSystem_1_1ConvoCoreGameSnapshot.html")]
[Serializable]
    public class ConvoCoreGameSnapshot
    {
        public string SchemaVersion = "1.0";
        public List<ConvoVariableEntry> GlobalVariables = new List<ConvoVariableEntry>();
        public List<ConversationSnapshot> Conversations = new List<ConversationSnapshot>();
        public long SaveTimestamp;
    }
}