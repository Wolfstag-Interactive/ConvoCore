using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Context container passed to renderers on initialization.
    /// Allows renderer implementations to extract whatever they need.
    /// </summary>
[HelpURL("https://docs.wolfstaginteractive.com/convocore/api/structWolfstagInteractive_1_1ConvoCore_1_1DialogueHistoryRendererContext.html")]
[System.Serializable]
    public struct DialogueHistoryRendererContext
    {
        public IDialogueHistoryOutput OutputHandler;

        public Color DefaultSpeakerColor;
        public int MaxEntries;
    }
}