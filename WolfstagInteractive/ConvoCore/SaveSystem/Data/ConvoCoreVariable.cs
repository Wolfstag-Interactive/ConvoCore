using System;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
    /// <summary>
    /// A strongly-typed named variable that can hold a <c>string</c>, <c>int</c>, <c>float</c>,
    /// or <c>bool</c> value. Variables are stored in a <see cref="ConvoVariableStore"/> and
    /// persisted to save data by <see cref="ConvoCoreSaveManager"/>.
    /// </summary>
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1SaveSystem_1_1ConvoVariable.html")]
[Serializable]
    public class ConvoCoreVariable
    {
        public string Key;
        public ConvoVariableType Type;
        public string Description;
        public string[] Tags;

        [SerializeField] private string _stringValue;
        [SerializeField] private int _intValue;
        [SerializeField] private float _floatValue;
        [SerializeField] private bool _boolValue;

        public string GetString() => _stringValue;
        public int GetInt() => _intValue;
        public float GetFloat() => _floatValue;
        public bool GetBool() => _boolValue;

        public ConvoCoreVariable SetString(string value)
        {
            _stringValue = value;
            return this;
        }

        public ConvoCoreVariable SetInt(int value)
        {
            _intValue = value;
            return this;
        }

        public ConvoCoreVariable SetFloat(float value)
        {
            _floatValue = value;
            return this;
        }

        public ConvoCoreVariable SetBool(bool value)
        {
            _boolValue = value;
            return this;
        }

        public bool TryGetValue<T>(out T result)
        {
            switch (Type)
            {
                case ConvoVariableType.String when typeof(T) == typeof(string):
                    result = (T)(object)_stringValue;
                    return true;
                case ConvoVariableType.Int when typeof(T) == typeof(int):
                    result = (T)(object)_intValue;
                    return true;
                case ConvoVariableType.Float when typeof(T) == typeof(float):
                    result = (T)(object)_floatValue;
                    return true;
                case ConvoVariableType.Bool when typeof(T) == typeof(bool):
                    result = (T)(object)_boolValue;
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        public string AsString()
        {
            switch (Type)
            {
                case ConvoVariableType.String:
                    return _stringValue ?? string.Empty;
                case ConvoVariableType.Int:
                    return _intValue.ToString();
                case ConvoVariableType.Float:
                    return _floatValue.ToString();
                case ConvoVariableType.Bool:
                    return _boolValue.ToString();
                default:
                    return string.Empty;
            }
        }

        public ConvoCoreVariable Clone()
        {
            return new ConvoCoreVariable
            {
                Key = Key,
                Type = Type,
                Description = Description,
                Tags = Tags != null ? (string[])Tags.Clone() : null,
                _stringValue = _stringValue,
                _intValue = _intValue,
                _floatValue = _floatValue,
                _boolValue = _boolValue
            };
        }
    }
}