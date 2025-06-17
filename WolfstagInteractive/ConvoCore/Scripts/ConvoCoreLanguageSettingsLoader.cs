using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{


    public class ConvoCoreLanguageSettingsLoader : IConvoCoreLanguageSettingsLoader
    {
        public ConvoCoreLanguageSettings LoadLanguageSettings()
        {
            // Use the path matching your asset location in the Resources folder.
            return Resources.Load<ConvoCoreLanguageSettings>("LanguageSettings");
        }

    }

    public interface IConvoCoreLanguageSettingsLoader
    {
        ConvoCoreLanguageSettings LoadLanguageSettings();
    }
}