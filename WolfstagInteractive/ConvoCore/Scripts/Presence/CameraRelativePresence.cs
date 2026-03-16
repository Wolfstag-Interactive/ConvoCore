using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Presence type that places characters at authored offsets relative to the active camera.
    ///
    /// Two positioning modes:
    /// <list type="bullet">
    ///   <item><b>Once</b> — position is calculated once on <see cref="ResolvePresence"/> and not updated.</item>
    ///   <item><b>Continuous</b> — a <see cref="ConvoCoreCameraRelativePosition"/> component is attached to the
    ///       spawned character and updates its world position every frame.</item>
    /// </list>
    ///
    /// Use case: first-person games, VR, or any setup where character position should be
    /// relative to the player's view.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraRelativePresence", menuName = "ConvoCore/Presence/Camera Relative Presence")]
    public class CameraRelativePresence : ConvoCoreCharacterPresence
    {
        public enum PositioningMode { Once, Continuous }

        [System.Serializable]
        public class CameraSlotEntry
        {
            [Tooltip("Forward distance from the camera.")]
            public float ForwardDistance = 2f;

            [Tooltip("Lateral offset (positive = right of camera).")]
            public float LateralOffset = 0f;

            [Tooltip("Vertical offset from camera position.")]
            public float Height = -0.5f;
        }

        [SerializeField] private List<CameraSlotEntry> _slots = new();

        [Tooltip("Once: position set at spawn only. Continuous: updated every frame via a MonoBehaviour.")]
        [SerializeField] private PositioningMode _mode = PositioningMode.Once;

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
                Debug.LogWarning($"[CameraRelativePresence] No slot defined for character index {context.CharacterIndex}.");
                return null;
            }

            var slot = _slots[context.CharacterIndex];
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[CameraRelativePresence] No Camera.main found in the scene.");
                return null;
            }

            var display = spawner.SpawnAndBind(representation, null);
            if (display == null) return null;

            var displayMono = display as MonoBehaviour;
            if (displayMono == null) return display;

            if (_mode == PositioningMode.Continuous)
            {
                var follow = displayMono.gameObject.AddComponent<ConvoCoreCameraRelativePosition>();
                follow.Initialize(slot.ForwardDistance, slot.LateralOffset, slot.Height);
            }
            else
            {
                displayMono.transform.position = ComputePosition(cam, slot);
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

        private static Vector3 ComputePosition(Camera cam, CameraSlotEntry slot)
        {
            return cam.transform.position
                + cam.transform.forward * slot.ForwardDistance
                + cam.transform.right * slot.LateralOffset
                + Vector3.up * slot.Height;
        }
    }

    /// <summary>
    /// Added at runtime to spawned characters by <see cref="CameraRelativePresence"/>
    /// when using <see cref="CameraRelativePresence.PositioningMode.Continuous"/> mode.
    /// Repositions the character relative to Camera.main every frame.
    /// </summary>
    public class ConvoCoreCameraRelativePosition : MonoBehaviour
    {
        private float _forward;
        private float _lateral;
        private float _height;

        public void Initialize(float forward, float lateral, float height)
        {
            _forward = forward;
            _lateral = lateral;
            _height = height;
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            transform.position = cam.transform.position
                + cam.transform.forward * _forward
                + cam.transform.right * _lateral
                + Vector3.up * _height;
        }
    }
}
