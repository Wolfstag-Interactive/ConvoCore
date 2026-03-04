using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1SaveSystem_1_1ConvoRuntimeState.html")]
[CreateAssetMenu(fileName = "NewConvoRuntimeState", menuName = "ConvoCore/Runtime/Convo Runtime State")]
    public class ConvoRuntimeState : ScriptableObject
    {
        public ConvoVariableStore VariableStore;

        [SerializeField] private List<ConversationSnapshot> _conversationStates = new List<ConversationSnapshot>();

        public ConvoCoreGameSnapshot ExportGameSnapshot()
        {
            var snapshot = new ConvoCoreGameSnapshot
            {
                SchemaVersion = "1.0",
                SaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (VariableStore != null)
                snapshot.GlobalVariables = VariableStore.ExportByScope(ConvoVariableScope.Global);

            snapshot.Conversations = new List<ConversationSnapshot>();
            for (int i = 0; i < _conversationStates.Count; i++)
            {
                var src = _conversationStates[i];
                var copy = new ConversationSnapshot
                {
                    ConversationId = src.ConversationId,
                    ActiveLineId = src.ActiveLineId,
                    IsComplete = src.IsComplete,
                    SaveTimestamp = src.SaveTimestamp,
                    VisitedLineIds = src.VisitedLineIds != null
                        ? new List<string>(src.VisitedLineIds)
                        : new List<string>(),
                    Variables = new List<ConvoVariableEntry>()
                };

                if (src.Variables != null)
                {
                    for (int j = 0; j < src.Variables.Count; j++)
                    {
                        copy.Variables.Add(new ConvoVariableEntry
                        {
                            CoreVariable = src.Variables[j].CoreVariable?.Clone(),
                            Scope = src.Variables[j].Scope,
                            IsReadOnly = src.Variables[j].IsReadOnly
                        });
                    }
                }

                snapshot.Conversations.Add(copy);
            }

            return snapshot;
        }

        public void RestoreFromGameSnapshot(ConvoCoreGameSnapshot snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[ConvoRuntimeState] Cannot restore from null snapshot.");
                return;
            }

            if (VariableStore != null)
            {
                VariableStore.ClearByScope(ConvoVariableScope.Global);
                if (snapshot.GlobalVariables != null)
                    VariableStore.RestoreEntries(snapshot.GlobalVariables);
            }

            _conversationStates.Clear();
            if (snapshot.Conversations != null)
            {
                for (int i = 0; i < snapshot.Conversations.Count; i++)
                    _conversationStates.Add(snapshot.Conversations[i]);
            }
        }

        public void RecordConversationState(ConversationSnapshot snapshot)
        {
            if (snapshot == null) return;

            for (int i = 0; i < _conversationStates.Count; i++)
            {
                if (_conversationStates[i].ConversationId == snapshot.ConversationId)
                {
                    _conversationStates[i] = snapshot;
                    return;
                }
            }
            _conversationStates.Add(snapshot);
        }

        public ConversationSnapshot GetConversationState(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId)) return null;

            for (int i = 0; i < _conversationStates.Count; i++)
            {
                if (_conversationStates[i].ConversationId == conversationId)
                    return _conversationStates[i];
            }
            return null;
        }
    }
}