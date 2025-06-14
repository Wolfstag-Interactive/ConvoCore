using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public class ConvoCoreLanguageSettings : ScriptableObject
    {
        [Tooltip("List of available language codes (e.g., EN, FR, ES).")]
        public List<string> SupportedLanguages;
    }
}