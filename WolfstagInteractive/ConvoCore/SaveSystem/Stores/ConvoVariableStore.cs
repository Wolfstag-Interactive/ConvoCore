using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1SaveSystem_1_1ConvoVariableStore.html")]
[CreateAssetMenu(fileName = "NewVariableStore", menuName = "ConvoCore/Runtime/Variable Store")]
    public class ConvoVariableStore : ScriptableObject
    {
        [SerializeField] private List<ConvoVariableEntry> _entries = new List<ConvoVariableEntry>();

        [NonSerialized] private Dictionary<string, Action<ConvoCoreVariable>> _keyListeners;

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

        // ----- Write Methods -----

        public bool SetString(string key, string value, ConvoVariableScope scope = ConvoVariableScope.Global)
        {
            var entry = FindOrCreateEntry(key, ConvoVariableType.String, scope);
            if (entry.IsReadOnly)
            {
                Debug.LogWarning($"[ConvoVariableStore] Variable '{key}' is read-only.");
                return false;
            }

            var oldValue = entry.CoreVariable.AsString();
            entry.CoreVariable.SetString(value);
            var newValue = entry.CoreVariable.AsString();

            if (oldValue != newValue)
                NotifyChanged(key, oldValue, newValue, entry.CoreVariable);

            return true;
        }

        public bool SetInt(string key, int value, ConvoVariableScope scope = ConvoVariableScope.Global)
        {
            var entry = FindOrCreateEntry(key, ConvoVariableType.Int, scope);
            if (entry.IsReadOnly)
            {
                Debug.LogWarning($"[ConvoVariableStore] Variable '{key}' is read-only.");
                return false;
            }

            var oldValue = entry.CoreVariable.AsString();
            entry.CoreVariable.SetInt(value);
            var newValue = entry.CoreVariable.AsString();

            if (oldValue != newValue)
                NotifyChanged(key, oldValue, newValue, entry.CoreVariable);

            return true;
        }

        public bool SetFloat(string key, float value, ConvoVariableScope scope = ConvoVariableScope.Global)
        {
            var entry = FindOrCreateEntry(key, ConvoVariableType.Float, scope);
            if (entry.IsReadOnly)
            {
                Debug.LogWarning($"[ConvoVariableStore] Variable '{key}' is read-only.");
                return false;
            }

            var oldValue = entry.CoreVariable.AsString();
            entry.CoreVariable.SetFloat(value);
            var newValue = entry.CoreVariable.AsString();

            if (oldValue != newValue)
                NotifyChanged(key, oldValue, newValue, entry.CoreVariable);

            return true;
        }

        public bool SetBool(string key, bool value, ConvoVariableScope scope = ConvoVariableScope.Global)
        {
            var entry = FindOrCreateEntry(key, ConvoVariableType.Bool, scope);
            if (entry.IsReadOnly)
            {
                Debug.LogWarning($"[ConvoVariableStore] Variable '{key}' is read-only.");
                return false;
            }

            var oldValue = entry.CoreVariable.AsString();
            entry.CoreVariable.SetBool(value);
            var newValue = entry.CoreVariable.AsString();

            if (oldValue != newValue)
                NotifyChanged(key, oldValue, newValue, entry.CoreVariable);

            return true;
        }

        // ----- Read Methods -----

        public bool TryGetString(string key, out string value)
        {
            var variable = GetVariable(key);
            if (variable != null && variable.Type == ConvoVariableType.String)
            {
                value = variable.GetString();
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetInt(string key, out int value)
        {
            var variable = GetVariable(key);
            if (variable != null && variable.Type == ConvoVariableType.Int)
            {
                value = variable.GetInt();
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetFloat(string key, out float value)
        {
            var variable = GetVariable(key);
            if (variable != null && variable.Type == ConvoVariableType.Float)
            {
                value = variable.GetFloat();
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetBool(string key, out bool value)
        {
            var variable = GetVariable(key);
            if (variable != null && variable.Type == ConvoVariableType.Bool)
            {
                value = variable.GetBool();
                return true;
            }
            value = default;
            return false;
        }

        public ConvoCoreVariable GetVariable(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].CoreVariable != null && _entries[i].CoreVariable.Key == key)
                    return _entries[i].CoreVariable;
            }
            return null;
        }

        public bool HasVariable(string key)
        {
            return GetVariable(key) != null;
        }

        // ----- Query Methods -----

        public IReadOnlyList<ConvoVariableEntry> GetByScope(ConvoVariableScope scope)
        {
            var result = new List<ConvoVariableEntry>();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Scope == scope)
                    result.Add(_entries[i]);
            }
            return result;
        }

        public IReadOnlyList<ConvoVariableEntry> GetByTag(string tag)
        {
            var result = new List<ConvoVariableEntry>();
            if (string.IsNullOrEmpty(tag)) return result;

            for (int i = 0; i < _entries.Count; i++)
            {
                var tags = _entries[i].CoreVariable?.Tags;
                if (tags == null) continue;
                for (int t = 0; t < tags.Length; t++)
                {
                    if (tags[t] == tag)
                    {
                        result.Add(_entries[i]);
                        break;
                    }
                }
            }
            return result;
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
            var result = new List<ConvoVariableEntry>();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Scope == scope)
                {
                    result.Add(new ConvoVariableEntry
                    {
                        CoreVariable = _entries[i].CoreVariable.Clone(),
                        Scope = _entries[i].Scope,
                        IsReadOnly = _entries[i].IsReadOnly
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

                bool found = false;
                for (int j = 0; j < _entries.Count; j++)
                {
                    if (_entries[j].CoreVariable != null && _entries[j].CoreVariable.Key == incoming.CoreVariable.Key)
                    {
                        _entries[j].CoreVariable = incoming.CoreVariable.Clone();
                        _entries[j].Scope = incoming.Scope;
                        _entries[j].IsReadOnly = incoming.IsReadOnly;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _entries.Add(new ConvoVariableEntry
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
            _entries.RemoveAll(e => e.Scope == scope);
        }

        // ----- Internal Access -----

        internal List<ConvoVariableEntry> GetRawEntries()
        {
            return _entries;
        }

        // ----- Private Helpers -----

        private ConvoVariableEntry FindOrCreateEntry(string key, ConvoVariableType type, ConvoVariableScope scope)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].CoreVariable != null && _entries[i].CoreVariable.Key == key)
                    return _entries[i];
            }

            var entry = new ConvoVariableEntry
            {
                CoreVariable = new ConvoCoreVariable { Key = key, Type = type },
                Scope = scope,
                IsReadOnly = false
            };
            _entries.Add(entry);
            return entry;
        }

        private void NotifyChanged(string key, string oldValue, string newValue, ConvoCoreVariable coreVariable)
        {
            OnVariableChanged?.Invoke(key, oldValue, newValue);

            if (KeyListeners.TryGetValue(key, out var listener))
                listener?.Invoke(coreVariable);
        }
    }
}