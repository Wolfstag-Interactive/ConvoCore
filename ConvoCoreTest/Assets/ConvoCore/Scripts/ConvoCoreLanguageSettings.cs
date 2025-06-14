using System.Collections.Generic;
using UnityEngine;

namespace CyberRift
{
    [CreateAssetMenu(fileName = "LanguageSettings", menuName = "Settings/LanguageSettings", order = 1)]
    public class ConvoCoreLanguageSettings : ScriptableObject
    {
        [Tooltip("List of available language codes (e.g., EN, FR, ES).")]
        public List<string> SupportedLanguages;
    }
}