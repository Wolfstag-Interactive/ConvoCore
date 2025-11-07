using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Context container passed to renderers on initialization.
    /// Allows renderer implementations to extract whatever they need.
    /// </summary>
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/structWolfstagInteractive_1_1ConvoCore_1_1DialogueHistoryRendererContext.html")]
[System.Serializable]
    public struct DialogueHistoryRendererContext
    {
        public IDialogueHistoryOutput OutputHandler;

        public Color DefaultSpeakerColor;
        public int MaxEntries;
    }
}