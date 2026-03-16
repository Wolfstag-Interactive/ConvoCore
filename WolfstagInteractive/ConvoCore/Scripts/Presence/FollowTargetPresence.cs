using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Presence type that spawns characters and keeps them following a scene Transform
    /// during the conversation.
    ///
    /// The follow target is resolved via <see cref="ConvoCoreSceneCharacterRegistry"/> by ID.
    /// A <see cref="ConvoCoreFollowTarget"/> MonoBehaviour is attached to each spawned
    /// character instance to drive the per-frame follow logic.
    ///
    /// On <see cref="OnConversationEnd"/>: releases all spawned instances via the spawner.
    ///
    /// Use case: companion NPC that walks beside the player during a conversation.
    /// </summary>
    [CreateAssetMenu(fileName = "FollowTargetPresence", menuName = "ConvoCore/Presence/Follow Target Presence")]
    public class FollowTargetPresence : ConvoCoreCharacterPresence
    {
        [System.Serializable]
        public class FollowSlotEntry
        {
            [Tooltip("Scene character ID (registered via ConvoCoreSceneCharacterRegistrant) of the target to follow.")]
            public string TargetSceneId;

            [Tooltip("World-space offset applied relative to the target's position.")]
            public Vector3 Offset;
        }

        [SerializeField] private List<FollowSlotEntry> _slots = new();

        [System.NonSerialized] private Dictionary<string, IConvoCoreCharacterDisplay> _cachedDisplays = new();
        [System.NonSerialized] private ConvoCorePrefabRepresentationSpawner _spawner;

        public override IConvoCoreCharacterDisplay ResolvePresence(
            PrefabCharacterRepresentationData representation,
            CharacterPresenceContext context,
            ConvoCorePrefabRepresentationSpawner spawner)
        {
            _spawner = spawner;

            if (_cachedDisplays.TryGetValue(representation.name, out var cached))
                return cached;

            if (context.CharacterIndex >= _slots.Count)
            {
                Debug.LogWarning($"[FollowTargetPresence] No slot defined for character index {context.CharacterIndex}.");
                return null;
            }

            var slot = _slots[context.CharacterIndex];

            // Resolve the follow target via registry.
            if (!spawner.TryGetSceneResident(slot.TargetSceneId, out var targetDisplay))
            {
                Debug.LogWarning($"[FollowTargetPresence] Follow target '{slot.TargetSceneId}' not found in registry.");
                return null;
            }

            // The target MonoBehaviour gives us its Transform.
            var targetMono = targetDisplay as MonoBehaviour;
            if (targetMono == null)
            {
                Debug.LogWarning($"[FollowTargetPresence] Target '{slot.TargetSceneId}' does not implement MonoBehaviour. Cannot resolve follow Transform.");
                return null;
            }

            // Spawn the character with no initial parent (follow component will position it).
            var display = spawner.SpawnAndBind(representation, null);
            if (display == null) return null;

            var displayMono = display as MonoBehaviour;
            if (displayMono != null)
            {
                var follow = displayMono.gameObject.AddComponent<ConvoCoreFollowTarget>();
                follow.Initialize(targetMono.transform, slot.Offset);
            }

            _cachedDisplays[representation.name] = display;
            return display;
        }

        public override void OnConversationEnd()
        {
            _spawner?.ReleaseAll();
            _cachedDisplays.Clear();
            _spawner = null;
        }
    }

    /// <summary>
    /// Added at runtime to spawned characters by <see cref="FollowTargetPresence"/>.
    /// Follows a target Transform with a fixed world-space offset each frame.
    /// </summary>
    public class ConvoCoreFollowTarget : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _offset;

        public void Initialize(Transform target, Vector3 offset)
        {
            _target = target;
            _offset = offset;
        }

        private void LateUpdate()
        {
            if (_target != null)
                transform.position = _target.position + _offset;
        }
    }
}
