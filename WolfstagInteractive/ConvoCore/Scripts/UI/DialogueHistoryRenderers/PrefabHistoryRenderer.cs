using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Renderer that requests prefab-based dialogue entry creation via IDialogueHistoryOutputPrefab.
    /// Does not directly instantiate or manipulate Unity UI elements.
    /// </summary>
    public class PrefabHistoryRenderer : IConvoCoreHistoryRenderer
    {
        public string RendererName => "Prefab";

        private IDialogueHistoryOutputPrefab _output;
        private readonly List<DialogueHistoryEntry> _entries = new();
        private int _maxEntries = 100;

        public void Initialize(object context = null)
        {
            if (context is DialogueHistoryRendererContext ctx)
            {
                _output = (IDialogueHistoryOutputPrefab)ctx.OutputHandler;
                _maxEntries = ctx.MaxEntries > 0 ? ctx.MaxEntries : 100;
            }
            _output?.Clear();
        }

        public void Clear()
        {
            _entries.Clear();
            _output?.Clear();
        }

        public void RenderAll(IReadOnlyList<DialogueHistoryEntry> entries)
        {
            Clear();
            foreach (var e in entries)
                RenderEntry(e);
        }

        public void RenderEntry(DialogueHistoryEntry entry)
        {
            if (_output == null)
            {
                Debug.LogWarning("[PrefabHistoryRenderer] No valid output handler assigned.");
                return;
            }

            _entries.Add(entry);
            if (_entries.Count > _maxEntries)
                _entries.RemoveAt(0);

            _output.SpawnEntry(entry);
            _output.RefreshView();
        }

        public void Tick(float deltaTime) { }
    }
}