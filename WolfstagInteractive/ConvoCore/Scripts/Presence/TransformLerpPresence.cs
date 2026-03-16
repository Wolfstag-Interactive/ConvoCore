using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Presence type for scene-resident characters that move to an authored offset position
    /// when a conversation begins and return to their original position when it ends.
    ///
    /// Characters are resolved via <see cref="ConvoCoreSceneCharacterRegistry"/>.
    /// Each slot entry specifies a world-space target position and optionally a duration.
    /// When duration is zero the move is instant; otherwise a <see cref="ConvoCoreTransformLerp"/>
    /// MonoBehaviour is added to the character's GameObject to run a coroutine.
    ///
    /// Use case: NPC turns to face the player, walks to a conversation spot, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "TransformLerpPresence", menuName = "ConvoCore/Presence/Transform Lerp Presence")]
    public class TransformLerpPresence : ConvoCoreCharacterPresence
    {
        [System.Serializable]
        public class LerpSlotEntry
        {
            [Tooltip("Scene character ID to move (registered via ConvoCoreSceneCharacterRegistrant).")]
            public string SceneCharacterId;

            [Tooltip("Target world position for conversation start.")]
            public Vector3 TargetPosition;

            [Tooltip("Target world rotation (Euler) for conversation start.")]
            public Vector3 TargetEulerRotation;

            [Tooltip("Move duration in seconds. 0 = instant.")]
            public float Duration = 0.4f;
        }

        [SerializeField] private List<LerpSlotEntry> _slots = new();

        // Runtime state
        [System.NonSerialized] private List<(Transform t, Vector3 origPos, Quaternion origRot)> _originals = new();
        [System.NonSerialized] private Dictionary<string, IConvoCoreCharacterDisplay> _cachedDisplays = new();

        public override void OnConversationBegin()
        {
            _originals.Clear();
            _cachedDisplays.Clear();
        }

        public override IConvoCoreCharacterDisplay ResolvePresence(
            PrefabCharacterRepresentationData representation,
            CharacterPresenceContext context,
            ConvoCorePrefabRepresentationSpawner spawner)
        {
            if (_cachedDisplays.TryGetValue(representation.name, out var cached))
                return cached;

            // Find the matching slot by scene ID.
            LerpSlotEntry slot = null;
            if (representation.SourceMode == CharacterSourceMode.SceneResident)
                slot = _slots.Find(s => s.SceneCharacterId == representation.SceneCharacterId);

            if (slot == null && context.CharacterIndex < _slots.Count)
                slot = _slots[context.CharacterIndex];

            if (slot == null)
            {
                Debug.LogWarning($"[TransformLerpPresence] No slot found for character index {context.CharacterIndex} or ID '{representation.SceneCharacterId}'.");
                return null;
            }

            if (!spawner.TryGetSceneResident(slot.SceneCharacterId, out var display))
            {
                Debug.LogWarning($"[TransformLerpPresence] Scene character '{slot.SceneCharacterId}' not found in registry.");
                return null;
            }

            var mono = display as MonoBehaviour;
            if (mono == null)
            {
                Debug.LogWarning($"[TransformLerpPresence] Display for '{slot.SceneCharacterId}' is not a MonoBehaviour.");
                return null;
            }

            var t = mono.transform;
            _originals.Add((t, t.position, t.rotation));

            if (slot.Duration <= 0f)
            {
                t.position = slot.TargetPosition;
                t.rotation = Quaternion.Euler(slot.TargetEulerRotation);
            }
            else
            {
                var lerp = mono.gameObject.AddComponent<ConvoCoreTransformLerp>();
                lerp.MoveTo(slot.TargetPosition, Quaternion.Euler(slot.TargetEulerRotation), slot.Duration);
            }

            _cachedDisplays[representation.name] = display;
            return display;
        }

        public override void OnConversationEnd()
        {
            foreach (var (t, origPos, origRot) in _originals)
            {
                if (t == null) continue;

                var lerp = t.GetComponent<ConvoCoreTransformLerp>();
                if (lerp != null)
                {
                    lerp.MoveTo(origPos, origRot, lerp.Duration);
                }
                else
                {
                    t.position = origPos;
                    t.rotation = origRot;
                }
            }

            _originals.Clear();
            _cachedDisplays.Clear();
        }
    }

    /// <summary>
    /// Added at runtime by <see cref="TransformLerpPresence"/> to smoothly move a character's
    /// Transform to a target position and rotation over a given duration.
    /// Self-destructs when the move completes.
    /// </summary>
    public class ConvoCoreTransformLerp : MonoBehaviour
    {
        public float Duration { get; private set; }

        private Coroutine _routine;

        public void MoveTo(Vector3 targetPos, Quaternion targetRot, float duration)
        {
            Duration = duration;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(LerpRoutine(targetPos, targetRot, duration));
        }

        private IEnumerator LerpRoutine(Vector3 targetPos, Quaternion targetRot, float duration)
        {
            var startPos = transform.position;
            var startRot = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            _routine = null;
            Destroy(this);
        }
    }
}
