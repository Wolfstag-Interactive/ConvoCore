using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
[HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1SaveSystem_1_1ConvoCoreConversationRunner.html")]
    public class ConvoCoreConversationSaveManager : MonoBehaviour
    {
        [Header("References")]
        public ConvoCoreSaveManager SaveManager;

        [Header("Conversation")]
        [SerializeField] private string _conversationId;

        [SerializeField] private bool _autoCommitOnEnd;
        [SerializeField] private bool _autoCommitOnStart;
        [SerializeField] private bool _autoCommitOnLineComplete;
        [SerializeField] private bool _autoCommitOnChoiceMade;

        [Header("Auto-Restore")]
        [SerializeField] private bool _autoRestoreOnAwake;
        [SerializeField] private bool _autoRestoreOnStart;
        [SerializeField] private ConvoRestoreBehavior _restoreBehavior = ConvoRestoreBehavior.ResumeFromActiveLine;

        // ----- Runtime State -----

        private ConvoCore _convoCore;
        private string _activeLineId;
        private bool _isComplete;
        private List<string> _visitedLineIds = new List<string>();
        private List<ConvoVariableEntry> _conversationVariables = new List<ConvoVariableEntry>();

        public bool IsDirty { get; private set; }
        public DateTime LastCommitTime { get; private set; }
        public string ConversationId => _conversationId;

        // ----- Events -----

        public Action<ConversationSnapshot> OnRestoreDecisionRequired;

        // ----- Lifecycle -----

        private void Awake()
        {
            _convoCore = GetComponent<ConvoCore>();

            if (_convoCore != null)
            {
                _convoCore.StartedConversation += OnConversationStarted;
                _convoCore.EndedConversation += OnConversationEnded;
                _convoCore.CompletedConversation += OnConversationEnded;
            }

            if (_autoRestoreOnAwake)
                TryAutoRestore();
        }

        private void Start()
        {
            if (_autoRestoreOnStart)
                TryAutoRestore();
        }

        private void OnDestroy()
        {
            if (_convoCore != null)
            {
                _convoCore.StartedConversation -= OnConversationStarted;
                _convoCore.EndedConversation -= OnConversationEnded;
                _convoCore.CompletedConversation -= OnConversationEnded;
            }
        }

        // ----- Public Methods -----

        public ConversationSnapshot GetConversationSnapshot()
        {
            return new ConversationSnapshot
            {
                ConversationId = _conversationId,
                ActiveLineId = _activeLineId,
                IsComplete = _isComplete,
                SaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                VisitedLineIds = new List<string>(_visitedLineIds),
                Variables = CloneVariables(_conversationVariables)
            };
        }

        public void RestoreConversationSnapshot(ConversationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[ConvoCoreConversationRunner] Cannot restore null snapshot.");
                return;
            }

            switch (_restoreBehavior)
            {
                case ConvoRestoreBehavior.ResumeFromActiveLine:
                    ApplySnapshot(snapshot);
                    break;
                case ConvoRestoreBehavior.RestartFromBeginning:
                    RestartWithRestoredVariables(snapshot);
                    break;
                case ConvoRestoreBehavior.AskViaEvent:
                    OnRestoreDecisionRequired?.Invoke(snapshot);
                    break;
            }
        }

        public void CommitSnapshot()
        {
            if (SaveManager == null)
            {
                Debug.LogWarning("[ConvoCoreConversationRunner] SaveManager is not assigned. Skipping commit.");
                return;
            }

            if (string.IsNullOrEmpty(_conversationId))
            {
                Debug.LogWarning("[ConvoCoreConversationRunner] Conversation ID is empty. Skipping commit.");
                return;
            }

            var snapshot = GetConversationSnapshot();
            SaveManager.RegisterConversationSnapshot(snapshot);
            IsDirty = false;
            LastCommitTime = DateTime.Now;
        }

        public void ResumeFromSnapshot()
        {
            var snapshot = GetPendingSnapshot();
            if (snapshot != null)
                ApplySnapshot(snapshot);
        }

        public void RestartWithRestoredVariables()
        {
            var snapshot = GetPendingSnapshot();
            if (snapshot != null)
                RestartWithRestoredVariables(snapshot);
        }

        // ----- Internal Lifecycle Hooks -----

        private void OnConversationStarted()
        {
            IsDirty = true;
            if (_autoCommitOnStart)
                CommitSnapshot();
        }

        private void OnConversationEnded()
        {
            _isComplete = true;
            IsDirty = true;
            if (_autoCommitOnEnd)
                CommitSnapshot();
        }

        public void OnLineCompleted()
        {
            IsDirty = true;
            if (_autoCommitOnLineComplete)
                CommitSnapshot();
        }

        public void OnChoiceMade(int choiceIndex)
        {
            IsDirty = true;
            if (_autoCommitOnChoiceMade)
                CommitSnapshot();
        }

        // ----- Read-Only State Accessors (for editor) -----

        public string ActiveLineId => _activeLineId;
        public bool IsComplete => _isComplete;
        public int VisitedLinesCount => _visitedLineIds.Count;
        public int ConversationVariablesCount => _conversationVariables.Count;

        // ----- Private Helpers -----

        private void TryAutoRestore()
        {
            if (SaveManager == null || string.IsNullOrEmpty(_conversationId))
                return;

            var snapshot = SaveManager.GetConversationSnapshot(_conversationId);
            if (snapshot != null)
                RestoreConversationSnapshot(snapshot);
        }

        private void ApplySnapshot(ConversationSnapshot snapshot)
        {
            _activeLineId = snapshot.ActiveLineId;
            _isComplete = snapshot.IsComplete;
            _visitedLineIds = snapshot.VisitedLineIds != null
                ? new List<string>(snapshot.VisitedLineIds)
                : new List<string>();
            _conversationVariables = CloneVariables(snapshot.Variables);
            IsDirty = false;
        }

        private void RestartWithRestoredVariables(ConversationSnapshot snapshot)
        {
            _activeLineId = null;
            _isComplete = false;
            _visitedLineIds.Clear();
            _conversationVariables = CloneVariables(snapshot.Variables);
            IsDirty = false;
        }

        private ConversationSnapshot GetPendingSnapshot()
        {
            if (SaveManager == null || string.IsNullOrEmpty(_conversationId))
                return null;
            return SaveManager.GetConversationSnapshot(_conversationId);
        }

        private static List<ConvoVariableEntry> CloneVariables(List<ConvoVariableEntry> source)
        {
            if (source == null) return new List<ConvoVariableEntry>();

            var result = new List<ConvoVariableEntry>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                result.Add(new ConvoVariableEntry
                {
                    CoreVariable = source[i].CoreVariable?.Clone(),
                    Scope = source[i].Scope,
                    IsReadOnly = source[i].IsReadOnly
                });
            }
            return result;
        }
    }
}