using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Resolves, tracks, and releases prefab-based character displays for a single UI instance.
    ///
    /// Supports two resolution paths determined by <see cref="CharacterSourceMode"/> on the
    /// representation asset:
    /// <list type="bullet">
    ///   <item><see cref="CharacterSourceMode.SpawnFromPrefab"/> -- instance is drawn from <see cref="ConvoCorePrefabPool"/> and returned on release.</item>
    ///   <item><see cref="CharacterSourceMode.SceneResident"/> -- instance is located via <see cref="ConvoCoreSceneCharacterRegistry"/>. ConvoCore never spawns, pools, or destroys it.</item>
    /// </list>
    ///
    /// Active entries are tracked by the <see cref="Transform"/> passed as the slot anchor.
    /// Passing the same Transform again releases the previous occupant before placing a new one.
    /// Scene-resident characters resolved via <see cref="TryGetSceneResident"/> are not tracked
    /// by slot and are never pooled or destroyed by this spawner.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCorePrefabRepresentationSpawner.html")]
    public class ConvoCorePrefabRepresentationSpawner : MonoBehaviour
    {
        [Tooltip("Required when any representation uses SceneResident source mode. Optional otherwise.")]
        [SerializeField] private ConvoCoreSceneCharacterRegistry sceneCharacterRegistry;

        // Active spawned entries keyed by the slot anchor Transform.
        private readonly Dictionary<Transform, ActiveCharacterEntry> _activeEntries = new();

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Full-service canvas UI path. Spawns (or locates) the character display, binds the
        /// representation, applies the expression and display options, and triggers a fade-in
        /// if the instance implements <see cref="IConvoCoreFadeIn"/>.
        /// Passing the same <paramref name="slotTransform"/> as a previous call releases the
        /// previous occupant before resolving the new one.
        /// Returns null if resolution fails.
        /// </summary>
        public IConvoCoreCharacterDisplay ResolveCharacter(
            PrefabCharacterRepresentationData representation,
            string expressionID,
            DialogueLineDisplayOptions displayOptions,
            Transform slotTransform)
        {
            var display = ResolveDisplayCore(representation, slotTransform);
            if (display == null) return null;

            if (!string.IsNullOrEmpty(expressionID))
                display.ApplyExpression(expressionID);

            if (displayOptions != null)
                display.ApplyDisplayOptions(displayOptions);

            if (_activeEntries.TryGetValue(slotTransform, out var entry) &&
                entry.SourceInstance != null &&
                entry.SourceInstance.TryGetComponent<IConvoCoreFadeIn>(out var fadeIn))
                fadeIn.FadeIn();

            return display;
        }

        /// <summary>
        /// Presence path. Spawns (or locates) the character display and binds the representation
        /// without applying any expression or display options. The caller is responsible for
        /// applying expression and display options after receiving the display.
        /// Returns null if resolution fails.
        /// </summary>
        public IConvoCoreCharacterDisplay SpawnAndBind(
            PrefabCharacterRepresentationData representation,
            Transform slotTransform)
        {
            return ResolveDisplayCore(representation, slotTransform);
        }

        /// <summary>
        /// Looks up a scene-resident display by ID without tracking it in the active-entry
        /// dictionary. The caller owns the display; ConvoCore will not pool or destroy it.
        /// </summary>
        public bool TryGetSceneResident(string sceneCharacterId, out IConvoCoreCharacterDisplay display)
        {
            display = null;
            if (sceneCharacterRegistry == null)
            {
                Debug.LogWarning($"[{nameof(ConvoCorePrefabRepresentationSpawner)}] No ConvoCoreSceneCharacterRegistry assigned. Cannot look up scene-resident character '{sceneCharacterId}'.");
                return false;
            }
            return sceneCharacterRegistry.TryGet(sceneCharacterId, out display);
        }

        /// <summary>
        /// Releases all active tracked entries. Scene-resident characters are removed from
        /// tracking only and are never pooled or destroyed.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var anchor in new List<Transform>(_activeEntries.Keys))
                ReleaseSlot(anchor);

            _activeEntries.Clear();
        }

        // ------------------------------------------------------------------
        // Core resolution
        // ------------------------------------------------------------------

        private IConvoCoreCharacterDisplay ResolveDisplayCore(
            PrefabCharacterRepresentationData representation,
            Transform slotTransform)
        {
            if (slotTransform != null)
                ReleaseSlot(slotTransform);

            IConvoCoreCharacterDisplay display;
            ActiveCharacterEntry entry;

            switch (representation.SourceMode)
            {
                case CharacterSourceMode.SceneResident:
                    display = ResolveSceneResident(representation);
                    if (display == null) return null;
                    entry = new ActiveCharacterEntry(display, isSceneResident: true, sourcePrefab: null);
                    break;

                case CharacterSourceMode.SpawnFromPrefab:
                default:
                    var instance = SpawnFromPool(representation, slotTransform);
                    if (instance == null) return null;
                    display = instance.GetComponentInChildren<IConvoCoreCharacterDisplay>();
                    if (display == null)
                    {
                        Debug.LogWarning($"[{nameof(ConvoCorePrefabRepresentationSpawner)}] Prefab '{representation.CharacterPrefab.name}' has no IConvoCoreCharacterDisplay component.");
                        ConvoCorePrefabPool.Instance.Release(representation.CharacterPrefab, instance);
                        return null;
                    }
                    entry = new ActiveCharacterEntry(display, isSceneResident: false,
                        sourcePrefab: representation.CharacterPrefab, sourceInstance: instance);
                    break;
            }

            display.BindRepresentation(representation);

            if (slotTransform != null)
                _activeEntries[slotTransform] = entry;

            return display;
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private IConvoCoreCharacterDisplay ResolveSceneResident(PrefabCharacterRepresentationData representation)
        {
            if (sceneCharacterRegistry == null)
            {
                Debug.LogWarning($"[{nameof(ConvoCorePrefabRepresentationSpawner)}] Representation '{representation.name}' uses SceneResident mode but no registry is assigned on this spawner.");
                return null;
            }

            if (!sceneCharacterRegistry.TryGet(representation.SceneCharacterId, out var display))
            {
                Debug.LogWarning($"[{nameof(ConvoCorePrefabRepresentationSpawner)}] No scene character registered with ID '{representation.SceneCharacterId}'.");
                return null;
            }

            return display;
        }

        private GameObject SpawnFromPool(PrefabCharacterRepresentationData representation, Transform parent)
        {
            if (representation.CharacterPrefab == null)
            {
                Debug.LogWarning($"[{nameof(ConvoCorePrefabRepresentationSpawner)}] Representation '{representation.name}' is set to SpawnFromPrefab but has no CharacterPrefab assigned.");
                return null;
            }

            if (ConvoCorePrefabPool.Instance == null)
            {
                Debug.LogWarning($"[{nameof(ConvoCorePrefabRepresentationSpawner)}] No ConvoCorePrefabPool found in the scene. Cannot spawn prefab character.");
                return null;
            }

            var instance = ConvoCorePrefabPool.Instance.Spawn(representation.CharacterPrefab, parent);
            instance.name = $"{representation.CharacterPrefab.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 6)}";
            return instance;
        }

        private void ReleaseSlot(Transform anchor)
        {
            if (!_activeEntries.TryGetValue(anchor, out var entry))
                return;

            _activeEntries.Remove(anchor);

            // Scene-resident characters are never pooled or destroyed by ConvoCore.
            if (entry.IsSceneResident)
                return;

            if (entry.SourceInstance == null || entry.SourcePrefab == null)
                return;

            var instance = entry.SourceInstance;
            var prefab = entry.SourcePrefab;

            if (instance.TryGetComponent<IConvoCoreFadeOut>(out var fadeOut))
            {
                fadeOut.FadeOutAndRelease(() =>
                {
                    if (ConvoCorePrefabPool.Instance != null)
                        ConvoCorePrefabPool.Instance.Release(prefab, instance);
                    else
                        Destroy(instance);
                });
            }
            else
            {
                if (ConvoCorePrefabPool.Instance != null)
                    ConvoCorePrefabPool.Instance.Release(prefab, instance);
                else
                    Destroy(instance);
            }
        }

        // ------------------------------------------------------------------
        // Entry tracking
        // ------------------------------------------------------------------

        private readonly struct ActiveCharacterEntry
        {
            public readonly IConvoCoreCharacterDisplay Display;
            public readonly bool IsSceneResident;
            public readonly GameObject SourcePrefab;
            public readonly GameObject SourceInstance;

            public ActiveCharacterEntry(
                IConvoCoreCharacterDisplay display,
                bool isSceneResident,
                GameObject sourcePrefab,
                GameObject sourceInstance = null)
            {
                Display = display;
                IsSceneResident = isSceneResident;
                SourcePrefab = sourcePrefab;
                SourceInstance = sourceInstance;
            }
        }
    }

    public interface IConvoCoreFadeOut
    {
        /// <summary>
        /// Called when a spawned character is being released from a slot.
        /// Invoke <paramref name="onComplete"/> when the fade is finished so the
        /// instance can be returned to the pool. Not called for scene-resident characters.
        /// </summary>
        void FadeOutAndRelease(System.Action onComplete);
    }

    public interface IConvoCoreFadeIn
    {
        /// <summary>
        /// Called immediately after a spawned character appears in a slot.
        /// Not called for scene-resident characters unless the developer explicitly triggers it.
        /// </summary>
        void FadeIn(System.Action onComplete = null);
    }
}
