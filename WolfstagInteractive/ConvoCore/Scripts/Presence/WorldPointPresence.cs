using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Presence type that places characters at authored world positions.
    ///
    /// On <see cref="OnConversationBegin"/>: creates marker Transforms at the authored positions.
    /// On <see cref="ResolvePresence"/>: spawns the prefab via the spawner and parents it to the
    /// marker for the character's slot index. Characters that appear on multiple lines are cached
    /// and not re-spawned.
    /// On <see cref="OnConversationEnd"/>: releases all spawned instances via the spawner and
    /// destroys the marker Transforms.
    ///
    /// Use case: characters that should appear at specific authored world positions during a conversation.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldPointPresence", menuName = "ConvoCore/Presence/World Point Presence")]
    public class WorldPointPresence : ConvoCoreCharacterPresence
    {
        [Tooltip("One entry per character slot. Index 0 is the first character to appear on any line, etc.")]
        [SerializeField] private List<WorldPointEntry> _worldPoints = new();

        [System.Serializable]
        public class WorldPointEntry
        {
            public Vector3 Position;
            public Vector3 EulerRotation;
        }

        // Runtime state -- not serialized.
        [System.NonSerialized] private List<Transform> _markers = new();
        [System.NonSerialized] private Dictionary<string, IConvoCoreCharacterDisplay> _cachedDisplays = new();
        [System.NonSerialized] private ConvoCorePrefabRepresentationSpawner _spawner;

        public override void OnConversationBegin()
        {
            CleanupMarkers();
            _cachedDisplays.Clear();

            foreach (var entry in _worldPoints)
            {
                var go = new GameObject($"_ConvoCore_WorldPoint_{entry.Position}");
                go.transform.position = entry.Position;
                go.transform.rotation = Quaternion.Euler(entry.EulerRotation);
                _markers.Add(go.transform);
            }
        }

        public override IConvoCoreCharacterDisplay ResolvePresence(
            PrefabCharacterRepresentationData representation,
            CharacterPresenceContext context,
            ConvoCorePrefabRepresentationSpawner spawner)
        {
            _spawner = spawner;

            // Return cached display for characters already resolved this conversation.
            if (_cachedDisplays.TryGetValue(representation.name, out var cached))
                return cached;

            if (context.CharacterIndex >= _markers.Count)
            {
                Debug.LogWarning($"[WorldPointPresence] No world point defined for character index {context.CharacterIndex}. " +
                                 $"Add entries to the World Points list.");
                return null;
            }

            var marker = _markers[context.CharacterIndex];
            var display = spawner.SpawnAndBind(representation, marker);

            if (display != null)
                _cachedDisplays[representation.name] = display;

            return display;
        }

        public override void OnConversationEnd()
        {
            _spawner?.ReleaseAll();
            CleanupMarkers();
            _cachedDisplays.Clear();
            _spawner = null;
        }

        private void CleanupMarkers()
        {
            foreach (var m in _markers)
                if (m != null)
                    Destroy(m.gameObject);
            _markers.Clear();
        }
    }
}
