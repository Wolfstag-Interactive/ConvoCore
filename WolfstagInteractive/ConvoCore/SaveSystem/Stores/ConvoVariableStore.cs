using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
    [CreateAssetMenu(fileName = "NewVariableStore", menuName = "ConvoCore/Runtime/Variable Store")]
    public class ConvoVariableStore : ScriptableObject
    {
        // Authored, serialized, editor-visible. Holds Global and Conversation scoped variables.
        // Designers pre-declare these with defaults, descriptions, and tags before the game runs.
        [SerializeField] private List<ConvoVariableEntry> _persistentEntries = new List<ConvoVariableEntry>();

        // Runtime only — never serialized, never editable in inspector.
        // Holds Session scoped variables exclusively. They only exist after runtime code sets them.
        [NonSerialized] private List<ConvoVariableEntry> _sessionEntries;

        // Key-based change listeners — runtime only, not serialized.
        [NonSerialized] private Dictionary<string, Action<ConvoCoreVariable>> _keyListeners;

        private List<ConvoVariableEntry> SessionEntries
        {
            get
            {
                if (_sessionEntries == null)
                    _sessionEntries = new List<ConvoVariableEntry>();
                return _sessionEntries;
            }
        }

        private Dictionary<string, Action<ConvoCoreVariable>> KeyListeners
        {
            get
            {
                if (_keyListeners == null)
                    _keyListeners = new Dictionary<string, Action<ConvoCoreVariable>>();
                return _keyListeners;
            }
        }

        public Action<string, string, string> OnVariableChanged;

        // ----- List Routing -----

        private List<ConvoVariableEntry> ListForScope(ConvoVariableScope scope)
        {
            return scope == ConvoVariableScope.Session ? SessionEntries : _persistentEntries;
        }

        private ConvoVariableEntry GetEntry(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            for (int i = 0; i < _persistentEntries.Count; i++)
            {
                if (_persistentEntries[i].CoreVariable != null && _persistentEntries[i].CoreVariable.Key == key)
                    return _persistentEntries[i];
            }

            for (int i = 0; i < SessionEntries.Count; i++)
            {
                if (SessionEntries[i].CoreVariable != null && SessionEntries[i].CoreVariable.Key == key)
                    return SessionEntries[i];
            }

            return null;
        }

        // ----- SetInternal -----

        private bool SetInternal(string key, Action<ConvoVariableEntry> apply,
            ConvoVariableType type, ConvoVariableScope scope)
        {
            var list = ListForScope(scope);
            ConvoVariableEntry entry = null;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].CoreVariable != null && list[i].CoreVariable.Key == key)
                {
                    entry = list[i];
                    break;
                }
            }

            if (entry == null)
            {
                entry = new ConvoVariableEntry
                {
                    CoreVariable = new ConvoCoreVariable { Key = key, Type = type },
                    Scope = scope,
                    IsReadOnly = false
                };
                list.Add(entry);
            }

            if (entry.IsReadOnly)
            {
                Debug.LogWarning($"[ConvoVariableStore] Variable '{key}' is marked read-only.");
                return false;
            }

            string oldValue = entry.CoreVariable.AsString();
            apply(entry);
            string newValue = entry.CoreVariable.AsString();

            if (oldValue != newValue)
            {
                OnVariableChanged?.Invoke(key, oldValue, newValue);
                if (KeyListeners.TryGetValue(key, out var listener))
                    listener?.Invoke(entry.CoreVariable);
            }

            return true;
        }

        // ----- Write Methods -----

        public bool SetString(string key, string value, ConvoVariableScope scope = ConvoVariableScope.Global)
            => SetInternal(key, e => e.CoreVariable.SetString(value), ConvoVariableType.String, scope);

        public bool SetInt(string key, int value, ConvoVariableScope scope = ConvoVariableScope.Global)
            => SetInternal(key, e => e.CoreVariable.SetInt(value), ConvoVariableType.Int, scope);

        public bool SetFloat(string key, float value, ConvoVariableScope scope = ConvoVariableScope.Global)
            => SetInternal(key, e => e.CoreVariable.SetFloat(value), ConvoVariableType.Float, scope);

        public bool SetBool(string key, bool value, ConvoVariableScope scope = ConvoVariableScope.Global)
            => SetInternal(key, e => e.CoreVariable.SetBool(value), ConvoVariableType.Bool, scope);

        // ----- Read Methods -----

        public bool TryGetString(string key, out string value)
        {
            var entry = GetEntry(key);
            if (entry != null && entry.CoreVariable.Type == ConvoVariableType.String)
            {
                value = entry.CoreVariable.GetString();
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetInt(string key, out int value)
        {
            var entry = GetEntry(key);
            if (entry != null && entry.CoreVariable.Type == ConvoVariableType.Int)
            {
                value = entry.CoreVariable.GetInt();
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetFloat(string key, out float value)
        {
            var entry = GetEntry(key);
            if (entry != null && entry.CoreVariable.Type == ConvoVariableType.Float)
            {
                value = entry.CoreVariable.GetFloat();
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetBool(string key, out bool value)
        {
            var entry = GetEntry(key);
            if (entry != null && entry.CoreVariable.Type == ConvoVariableType.Bool)
            {
                value = entry.CoreVariable.GetBool();
                return true;
            }
            value = default;
            return false;
        }

        public ConvoCoreVariable GetVariable(string key)
        {
            return GetEntry(key)?.CoreVariable;
        }

        public bool HasVariable(string key)
        {
            return GetEntry(key) != null;
        }

        // ----- Query Methods -----

        public IReadOnlyList<ConvoVariableEntry> GetByScope(ConvoVariableScope scope)
        {
            var list = ListForScope(scope);
            var result = new List<ConvoVariableEntry>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Scope == scope)
                    result.Add(list[i]);
            }
            return result;
        }

        public IReadOnlyList<ConvoVariableEntry> GetByTag(string tag)
        {
            var result = new List<ConvoVariableEntry>();
            if (string.IsNullOrEmpty(tag)) return result;

            CollectByTag(_persistentEntries, tag, result);
            CollectByTag(SessionEntries, tag, result);
            return result;
        }

        private static void CollectByTag(List<ConvoVariableEntry> list, string tag,
            List<ConvoVariableEntry> result)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var tags = list[i].CoreVariable?.Tags;
                if (tags == null) continue;
                for (int t = 0; t < tags.Length; t++)
                {
                    if (tags[t] == tag)
                    {
                        result.Add(list[i]);
                        break;
                    }
                }
            }
        }

        // ----- Subscription Methods -----

        public void Subscribe(string key, Action<ConvoCoreVariable> callback)
        {
            if (string.IsNullOrEmpty(key) || callback == null) return;

            if (KeyListeners.ContainsKey(key))
                KeyListeners[key] += callback;
            else
                KeyListeners[key] = callback;
        }

        public void Unsubscribe(string key, Action<ConvoCoreVariable> callback)
        {
            if (string.IsNullOrEmpty(key) || callback == null) return;

            if (KeyListeners.ContainsKey(key))
            {
                KeyListeners[key] -= callback;
                if (KeyListeners[key] == null)
                    KeyListeners.Remove(key);
            }
        }

        // ----- Snapshot Methods -----

        public List<ConvoVariableEntry> ExportByScope(ConvoVariableScope scope)
        {
            // Session variables are runtime-only — never written to a save file.
            if (scope == ConvoVariableScope.Session)
                return new List<ConvoVariableEntry>();

            var result = new List<ConvoVariableEntry>();
            for (int i = 0; i < _persistentEntries.Count; i++)
            {
                if (_persistentEntries[i].Scope == scope)
                {
                    result.Add(new ConvoVariableEntry
                    {
                        CoreVariable = _persistentEntries[i].CoreVariable.Clone(),
                        Scope = _persistentEntries[i].Scope,
                        IsReadOnly = _persistentEntries[i].IsReadOnly
                    });
                }
            }
            return result;
        }

        public void RestoreEntries(List<ConvoVariableEntry> entries)
        {
            if (entries == null) return;

            for (int i = 0; i < entries.Count; i++)
            {
                var incoming = entries[i];
                if (incoming.CoreVariable == null) continue;

                // Session scope is never saved, so it is never restored.
                if (incoming.Scope == ConvoVariableScope.Session)
                    continue;

                bool found = false;
                for (int j = 0; j < _persistentEntries.Count; j++)
                {
                    if (_persistentEntries[j].CoreVariable != null &&
                        _persistentEntries[j].CoreVariable.Key == incoming.CoreVariable.Key)
                    {
                        _persistentEntries[j].CoreVariable = incoming.CoreVariable.Clone();
                        _persistentEntries[j].Scope = incoming.Scope;
                        _persistentEntries[j].IsReadOnly = incoming.IsReadOnly;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _persistentEntries.Add(new ConvoVariableEntry
                    {
                        CoreVariable = incoming.CoreVariable.Clone(),
                        Scope = incoming.Scope,
                        IsReadOnly = incoming.IsReadOnly
                    });
                }
            }
        }

        public void ClearByScope(ConvoVariableScope scope)
        {
            if (scope == ConvoVariableScope.Session)
                SessionEntries.Clear();
            else
                _persistentEntries.RemoveAll(e => e.Scope == scope);
        }

        // ----- Internal Access -----

        public List<ConvoVariableEntry> GetRawEntries()
        {
            return _persistentEntries;
        }

        // ----- Editor-Only Access -----

#if UNITY_EDITOR
        public IReadOnlyList<ConvoVariableEntry> GetSessionEntries()
        {
            return SessionEntries;
        }
#endif
    }
}