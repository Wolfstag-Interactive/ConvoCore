using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Add this component to any scene GameObject that should be driven by ConvoCore
    /// as a scene-resident character (i.e. not spawned from a pool).
    ///
    /// The component resolves an <see cref="IConvoCoreCharacterDisplay"/> from this
    /// GameObject or its children and registers it with the assigned
    /// <see cref="ConvoCoreSceneCharacterRegistry"/> on enable, unregistering on disable.
    ///
    /// The <see cref="CharacterId"/> must match the Scene Character ID set on
    /// the <see cref="PrefabCharacterRepresentationData"/> asset configured in SceneResident mode.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api")]
    public class ConvoCoreSceneCharacterRegistrant : MonoBehaviour
    {
        [Tooltip("Must match the Scene Character ID on the PrefabCharacterRepresentationData asset.")]
        [SerializeField] private string characterId;

        [Tooltip("The registry to register with. Must be assigned -- no singleton lookup is performed.")]
        [SerializeField] private ConvoCoreSceneCharacterRegistry registry;

        private IConvoCoreCharacterDisplay _display;

        private void Awake()
        {
            _display = GetComponentInChildren<IConvoCoreCharacterDisplay>(includeInactive: true);

            if (_display == null)
                Debug.LogWarning($"[ConvoCoreSceneCharacterRegistrant] No IConvoCoreCharacterDisplay found on '{gameObject.name}' or its children. This character will not be available to ConvoCore.");
        }

        private void OnEnable()
        {
            if (registry == null)
            {
                Debug.LogWarning($"[ConvoCoreSceneCharacterRegistrant] No registry assigned on '{gameObject.name}'. Character '{characterId}' will not be registered.");
                return;
            }

            if (_display == null) return;

            registry.Register(characterId, _display);
        }

        private void OnDisable()
        {
            registry?.Unregister(characterId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(characterId))
                Debug.LogWarning($"[ConvoCoreSceneCharacterRegistrant] Character ID is empty on '{gameObject.name}'. Assign an ID that matches the representation asset.");
        }
#endif
    }
}