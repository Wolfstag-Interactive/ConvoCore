using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public enum ConversationContainerMode
    {
        Playlist,
        Selector
    }

    public enum ConversationSelectionMode
    {
        First,
        Sequential,
        Random,
        WeightedRandom,
    }

    public readonly struct ConversationBranchResult
    {
        public readonly ConvoCoreConversationData Conversation;
        public readonly int StartLineIndex;

        public ConversationBranchResult(ConvoCoreConversationData conversation, int startLineIndex)
        {
            Conversation = conversation;
            StartLineIndex = startLineIndex;
        }
    }

    [HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConversationContainer.html")]
    [CreateAssetMenu(menuName = "ConvoCore/Conversation Container")]
    public sealed class ConversationContainer : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string Alias;
            public ConvoCoreConversationData ConversationData;
            public bool Enabled = true;

            [Tooltip("Only used when this container is played as a Playlist.")]
            public float DelayAfterEndSeconds = 0f;

            public string[] Tags;

            [Min(0f)]
            [Tooltip("Only used in WeightedRandom selection mode.")]
            public float Weight = 1f;

            [Min(0)]
            [Tooltip("Only used when this container is used as a Selector branch target.")]
            public int StartLineIndex = 0;
        }

        [Header("Common")]
        public ConversationContainerMode ContainerMode = ConversationContainerMode.Playlist;

        [Tooltip("Used when ContainerMode is Selector.")]
        public ConversationSelectionMode SelectionMode = ConversationSelectionMode.First;

        public List<Entry> Conversations = new();

        [Header("Playlist Mode")]
        public bool Loop = false;
        public string DefaultStart;

        private static readonly Dictionary<ConversationContainer, int> _sequentialIndices =
            new Dictionary<ConversationContainer, int>();

        public ConversationBranchResult ResolveForBranch(IConversationContext context, string aliasOrName = null)
        {
            if (Conversations == null || Conversations.Count == 0)
                return default;

            var candidates = Conversations
                .Where(e => e != null && e.Enabled && e.ConversationData != null)
                .ToList();

            if (!string.IsNullOrEmpty(aliasOrName))
            {
                var filtered = candidates.Where(e =>
                        (!string.IsNullOrEmpty(e.Alias) &&
                         e.Alias.Equals(aliasOrName, StringComparison.OrdinalIgnoreCase)) ||
                        (e.ConversationData != null &&
                         e.ConversationData.name.Equals(aliasOrName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (filtered.Count > 0)
                    candidates = filtered;
            }

            if (candidates.Count == 0)
                return default;

            var mode = SelectionMode;
            if (ContainerMode == ConversationContainerMode.Playlist)
            {
                Debug.LogWarning($"[ConvoCore] ConversationContainer '{name}' used as branch target but is in Playlist mode. Treating as 'First' selector.");
                mode = ConversationSelectionMode.First;
            }

            Entry chosen;
            switch (mode)
            {
                case ConversationSelectionMode.First:
                    chosen = candidates[0];
                    break;

                case ConversationSelectionMode.Random:
                    chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    break;

                case ConversationSelectionMode.Sequential:
                    chosen = ResolveSequentialEntry(candidates);
                    break;

                case ConversationSelectionMode.WeightedRandom:
                    chosen = ResolveWeightedRandomEntry(candidates);
                    break;

                default:
                    chosen = candidates[0];
                    break;
            }

            if (chosen == null || chosen.ConversationData == null)
                return default;

            int startIndex = Mathf.Max(0, chosen.StartLineIndex);

            // Clamp against actual line count when possible
            var lines = chosen.ConversationData.DialogueLines;
            if (lines != null && lines.Count > 0)
                startIndex = Mathf.Clamp(startIndex, 0, lines.Count - 1);
            else
                startIndex = 0;

            return new ConversationBranchResult(chosen.ConversationData, startIndex);
        }

        private Entry ResolveSequentialEntry(List<Entry> candidates)
        {
            if (!_sequentialIndices.TryGetValue(this, out var idx))
                idx = 0;

            if (candidates.Count == 0)
                return null;

            if (idx >= candidates.Count)
                idx = candidates.Count - 1;

            var entry = candidates[idx];
            idx = (idx + 1) % candidates.Count;
            _sequentialIndices[this] = idx;

            return entry;
        }

        private static Entry ResolveWeightedRandomEntry(List<Entry> candidates)
        {
            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
                total += Mathf.Max(0f, candidates[i].Weight);

            if (total <= 0f)
                return candidates[0];

            float roll = UnityEngine.Random.value * total;
            float accum = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                accum += Mathf.Max(0f, candidates[i].Weight);
                if (roll <= accum)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }
    }
}