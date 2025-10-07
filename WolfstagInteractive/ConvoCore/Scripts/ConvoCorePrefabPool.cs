using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCorePrefabPool.html")]
    public class ConvoCorePrefabPool : MonoBehaviour
    {
        public static ConvoCorePrefabPool Instance { get; private set; }

        private readonly Dictionary<GameObject, Stack<GameObject>> _pool = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            if (!_pool.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                _pool[prefab] = stack;
            }

            GameObject instance;
            if (stack.Count > 0)
            {
                instance = stack.Pop();
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(prefab);
            }

            if (parent != null)
                instance.transform.SetParent(parent, worldPositionStays: false);

            return instance;
        }

        public void Clear()
        {
            foreach (var kvp in _pool)
            {
                foreach (var go in kvp.Value)
                {
                    if (go != null)
                        Destroy(go);
                }
            }

            _pool.Clear();
        }

        public void Preload(GameObject prefab, int count)
        {
            if (!_pool.TryGetValue(prefab, out var stack))
                _pool[prefab] = stack = new Stack<GameObject>();

            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(prefab);
                instance.SetActive(false);
                stack.Push(instance);
            }
        }


        public void Release(GameObject prefab, GameObject instance)
        {
            if (instance == null) return;

            if (!_pool.TryGetValue(prefab, out var stack))
                _pool[prefab] = stack = new Stack<GameObject>();

            if (stack.Contains(instance))
            {
                Debug.LogWarning($"[PrefabPool] Attempted to release {instance.name} twice.");
                return;
            }

            instance.SetActive(false);
            stack.Push(instance);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            foreach (var kvp in _pool)
            {
                Debug.Log($"[PrefabPool] Prefab: {kvp.Key.name} | Pooled: {kvp.Value.Count}");
            }
        }
#endif
    }
}