using System;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
    /// <summary>
    /// Container pairing a <see cref="ConvoCoreVariable"/> with its scope and read-only flag.
    /// Used as the serialized element type in <see cref="ConvoVariableStore"/> persistent entry lists.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1SaveSystem_1_1ConvoVariableEntry.html")]
[Serializable]
    public class ConvoVariableEntry
    {
        public ConvoCoreVariable CoreVariable;
        public ConvoVariableScope Scope;
        public bool IsReadOnly;
    }
}