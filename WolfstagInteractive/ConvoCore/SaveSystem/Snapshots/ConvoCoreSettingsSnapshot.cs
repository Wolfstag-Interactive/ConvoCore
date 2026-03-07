using System;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1SaveSystem_1_1ConvoCoreSettingsSnapshot.html")]
[Serializable]
    public class ConvoCoreSettingsSnapshot
    {
        public string SchemaVersion = "1.0";
        public string SelectedLanguage;
        public long SaveTimestamp;
    }
}