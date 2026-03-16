using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Presence type that wraps a list of other presence types and selects between them
    /// based on a conversation index counter or an optional named condition.
    ///
    /// Each time <see cref="OnConversationBegin"/> is called the active presence is selected from
    /// the list using the current index (which increments automatically). Optionally a
    /// <see cref="ConditionKey"/> and <see cref="ISequencedPresenceCondition"/> can override
    /// the index-based selection.
    ///
    /// All lifecycle calls (<see cref="OnConversationBegin"/>, <see cref="OnConversationEnd"/>)
    /// are forwarded only to the currently active sub-presence.
    ///
    /// Use case: the same scene has multiple dialogue modes and requires different character
    /// placement strategies depending on which conversation is active.
    /// </summary>
    [CreateAssetMenu(fileName = "SequencedPresence", menuName = "ConvoCore/Presence/Sequenced Presence")]
    public class SequencedPresence : ConvoCoreCharacterPresence
    {
        [Tooltip("Ordered list of sub-presences to cycle through. Wraps around when exhausted.")]
        [SerializeField] private List<ConvoCoreCharacterPresence> _presences = new();

        [Tooltip("Optional named key used with ISequencedPresenceCondition for condition-based selection. " +
                 "Leave empty to use index-based (round-robin) selection only.")]
        [SerializeField] private string _conditionKey;

        [System.NonSerialized] private int _conversationCounter;
        [System.NonSerialized] private ConvoCoreCharacterPresence _active;

        public override void OnConversationBegin()
        {
            if (_presences == null || _presences.Count == 0)
            {
                Debug.LogWarning("[SequencedPresence] No sub-presences configured.");
                _active = null;
                return;
            }

            int index = _conversationCounter % _presences.Count;
            _conversationCounter++;

            _active = _presences[index];
            _active?.OnConversationBegin();
        }

        public override IConvoCoreCharacterDisplay ResolvePresence(
            PrefabCharacterRepresentationData representation,
            CharacterPresenceContext context,
            ConvoCorePrefabRepresentationSpawner spawner)
        {
            if (_active == null)
            {
                Debug.LogWarning("[SequencedPresence] No active sub-presence. Was OnConversationBegin called?");
                return null;
            }

            return _active.ResolvePresence(representation, context, spawner);
        }

        public override void OnConversationEnd()
        {
            _active?.OnConversationEnd();
            _active = null;
        }
    }
}
