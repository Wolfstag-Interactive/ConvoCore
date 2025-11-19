using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCorePrefabRepresentationSpawner.html")]
    public class ConvoCorePrefabRepresentationSpawner : MonoBehaviour
    {
        [SerializeField] private Transform leftParent;
        [SerializeField] private Transform rightParent;
        [SerializeField] private Transform centerParent;

        private readonly Dictionary<DisplaySlot, GameObject> _activeInstances = new();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();

        public GameObject SpawnCharacter(
            PrefabCharacterRepresentationData representation,
            string emotionID,
            DialogueLineDisplayOptions displayOptions,
            DisplaySlot slot)
        {
            // Despawn existing in that slot
            if (_activeInstances.TryGetValue(slot, out var previous))
            {
                if (_instanceToPrefab.TryGetValue(previous, out var oldPrefab))
                {
                    if (previous.TryGetComponent<IConvoCoreFadeOut>(out var fadeOut))
                    {
                        fadeOut.FadeOutAndRelease(() =>
                        {
                            ConvoCorePrefabPool.Instance.Release(oldPrefab, previous);
                        });
                    }
                    else
                    {
                        ConvoCorePrefabPool.Instance.Release(oldPrefab, previous);
                    }
                }

                _activeInstances.Remove(slot);
                _instanceToPrefab.Remove(previous);
            }

            var instance = ConvoCorePrefabPool.Instance.Spawn(representation.CharacterPrefab, GetParentTransform(slot));
            instance.name = $"{representation.CharacterPrefab.name}_{slot}_{System.Guid.NewGuid().ToString("N").Substring(0, 6)}";

            var display = instance.GetComponentInChildren<IConvoCoreCharacterDisplay>();
            if (display == null)
            {
                Debug.LogWarning($"[{nameof(ConvoCorePrefabRepresentationSpawner)}] Prefab '{representation.CharacterPrefab.name}' has no IConvoCoreCharacterDisplay.");
            }
            else
            {
                display.BindRepresentation(representation);  
                display.ApplyEmotion(emotionID);
                display.ApplyDisplayOptions(displayOptions);
            }

            if (instance.TryGetComponent<IConvoCoreFadeIn>(out var fadeIn))
                fadeIn.FadeIn();

            _activeInstances[slot] = instance;
            _instanceToPrefab[instance] = representation.CharacterPrefab;
            return instance;
        }
        


        public void DespawnAll()
        {
            foreach (var instance in _activeInstances.Values)
            {
                DespawnInstance(instance);
            }

            _activeInstances.Clear();
        }

        private void DespawnInstance(GameObject instance)
        {
            if (instance == null) return;

            if (_instanceToPrefab.TryGetValue(instance, out var prefab))
            {
                var fade = instance.GetComponentInChildren<IConvoCoreFadeOut>();
                if (fade != null)
                {
                    fade.FadeOutAndRelease(() =>
                    {
                        ConvoCorePrefabPool.Instance.Release(prefab, instance);
                        _instanceToPrefab.Remove(instance);
                    });
                }
                else
                {
                    ConvoCorePrefabPool.Instance.Release(prefab, instance);
                    _instanceToPrefab.Remove(instance);
                }
            }
            else
            {
                Destroy(instance); // Fallback if we somehow lost the prefab ref
            }
        }

        private Transform GetParentTransform(DisplaySlot slot)
        {
            return slot switch
            {
                DisplaySlot.Left => leftParent,
                DisplaySlot.Right => rightParent,
                DisplaySlot.Center => centerParent,
                _ => transform
            };
        }
    }

    public enum DisplaySlot
    {
        Left,
        Right,
        Center
    }

    public interface IConvoCoreFadeOut
    {
        /// <summary>
        /// Called when a character is despawned from a slot and should fade out
        /// </summary>
        /// <param name="onComplete"></param>
        void FadeOutAndRelease(System.Action onComplete);
    }
    public interface IConvoCoreFadeIn
    {
        /// <summary>
        /// Called when a character is spawned into a slot and should fade in.
        /// </summary>
        /// <param name="onComplete">Optional callback invoked after fade-in completes.</param>
        void FadeIn(System.Action onComplete = null);
    }
}