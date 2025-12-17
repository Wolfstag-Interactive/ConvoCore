using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public enum BranchTargetType
    {
        ConversationStart,
        ConversationLine,
        RandomFromList,
        UseConversationContainer,
        EndConversation
    }

    [Serializable]
    public sealed class BranchEntry
    {
        public string Key;

        public BranchTargetType TargetType;

        public ConvoCoreConversationData TargetConversation;
        public int TargetLineIndex;

        // For RandomFromList
        public List<int> LineIndexPool = new();

        // For UseConversationContainer
        public ConversationContainer TargetContainer;
        public string ContainerStartAliasOrName;
        public bool? ContainerLoopOverride = null;
    }

    public struct BranchResult
    {
        public bool EndsConversation;
        public ConvoCoreConversationData Conversation;
        public int LineIndex;
    }

    [CreateAssetMenu(menuName = "ConvoCore/Conversation Branch Container")]
    public sealed class ConversationBranchContainer : ScriptableObject
    {
        public List<BranchEntry> Entries = new();

        private Dictionary<string, BranchEntry> _lookup;

        private void OnEnable()
        {
            _lookup = new Dictionary<string, BranchEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in Entries)
            {
                if (string.IsNullOrWhiteSpace(e.Key)) 
                    continue;

                if (!_lookup.ContainsKey(e.Key))
                    _lookup.Add(e.Key, e);
            }
        }

        public bool TryResolve(string key, IConversationContext context, out BranchResult result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(key) || _lookup == null)
                return false;

            if (!_lookup.TryGetValue(key, out var entry))
                return false;

            result = ResolveEntry(entry, context);
            return true;
        }

        private static BranchResult ResolveEntry(BranchEntry entry, IConversationContext context)
        {
            switch (entry.TargetType)
            {
                case BranchTargetType.EndConversation:
                    return new BranchResult { EndsConversation = true };

                case BranchTargetType.ConversationStart:
                    return ResolveConversationStart(entry);

                case BranchTargetType.ConversationLine:
                    return ResolveConversationLine(entry);

                case BranchTargetType.RandomFromList:
                    return ResolveRandomFromList(entry);

                case BranchTargetType.UseConversationContainer:
                    return ResolveUsingConversationContainer(entry, context);

                default:
                    return new BranchResult { EndsConversation = true };
            }
        }

        private static BranchResult ResolveConversationStart(BranchEntry entry)
        {
            if (entry.TargetConversation == null)
                return new BranchResult { EndsConversation = true };

            return new BranchResult
            {
                EndsConversation = false,
                Conversation = entry.TargetConversation,
                LineIndex = 0
            };
        }

        private static BranchResult ResolveConversationLine(BranchEntry entry)
        {
            if (entry.TargetConversation == null)
                return new BranchResult { EndsConversation = true };

            var lines = entry.TargetConversation.DialogueLines; // whatever your property is
            var idx = Mathf.Clamp(entry.TargetLineIndex, 0, lines.Count - 1);

            return new BranchResult
            {
                EndsConversation = false,
                Conversation = entry.TargetConversation,
                LineIndex = idx
            };
        }

        private static BranchResult ResolveRandomFromList(BranchEntry entry)
        {
            var convo = entry.TargetConversation;
            if (convo == null || entry.LineIndexPool == null || entry.LineIndexPool.Count == 0)
                return new BranchResult { EndsConversation = true };

            var lines = convo.DialogueLines;
            var chosenIndex = entry.LineIndexPool[UnityEngine.Random.Range(0, entry.LineIndexPool.Count)];
            var idx = Mathf.Clamp(chosenIndex, 0, lines.Count - 1);

            return new BranchResult
            {
                EndsConversation = false,
                Conversation = convo,
                LineIndex = idx
            };
        }

        private static BranchResult ResolveUsingConversationContainer(BranchEntry entry, IConversationContext context)
        {
            var container = entry.TargetContainer;
            if (container == null)
                return new BranchResult { EndsConversation = true };

            var convo = ResolveContainerSingle(container, entry.ContainerStartAliasOrName, entry.ContainerLoopOverride);
            if (convo == null)
                return new BranchResult { EndsConversation = true };

            return new BranchResult
            {
                EndsConversation = false,
                Conversation = convo,
                LineIndex = 0
            };
        }

        private static ConvoCoreConversationData ResolveContainerSingle(
            ConversationContainer container,
            string startAliasOrName,
            bool? loopOverride)
        {
            if (container == null || container.Conversations == null || container.Conversations.Count == 0)
                return null;

            container.Conversations.RemoveAll(e => e == null || e.ConversationData == null);
            if (container.Conversations.Count == 0)
                return null;

            var start = ConversationContainerRuntime.ResolveIndex(
                container,
                string.IsNullOrEmpty(startAliasOrName) ? container.DefaultStart : startAliasOrName
            );

            var e = container.Conversations[start];
            if (!e.Enabled || e.ConversationData == null)
                return null;

            return e.ConversationData;
        }
    }
}