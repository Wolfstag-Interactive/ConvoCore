#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1EmotionIdSelectorDrawer.html")]
[CustomPropertyDrawer(typeof(EmotionIDSelectorAttribute))]
    public class EmotionIdSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (EmotionIDSelectorAttribute)attribute;

            // Find the representation property relative to this field
            var repPropPath = property.propertyPath.Replace(property.name, attr.RepresentationPropertyName);
            var repProp = property.serializedObject.FindProperty(repPropPath);

            var rep = repProp?.objectReferenceValue as CharacterRepresentationBase;
            if (rep == null)
            {
                EditorGUI.HelpBox(position, "Assign a Representation to select an Emotion.", MessageType.Info);
                return;
            }

            string[] names;
            string[] ids;

            if (rep is PrefabCharacterRepresentationData prefabRep)
            {
                var catalog = prefabRep.GetEmotionCatalog();
                names = catalog.Select(c => c.name).ToArray();
                ids   = catalog.Select(c => c.id).ToArray();
            }
            else if (rep is SpriteCharacterRepresentationData spriteRep)
            {
                var catalog = spriteRep.GetEmotionCatalog();
                names = catalog.Select(c => c.name).ToArray();
                ids   = catalog.Select(c => c.id).ToArray();
            }
            else
            {
                EditorGUI.HelpBox(position, "Representation does not expose a GUID catalog.", MessageType.Warning);
                return;
            }

            if (ids.Length == 0)
            {
                EditorGUI.Popup(position, label.text, -1, new[] { "(No Emotions)" });
                return;
            }

            var currentId = property.stringValue;
            var idx = Mathf.Max(0, System.Array.IndexOf(ids, currentId));
            var newIdx = EditorGUI.Popup(position, label.text, idx, names);

            if (newIdx != idx && newIdx >= 0 && newIdx < ids.Length)
            {
                property.stringValue = ids[newIdx];
            }
        }
    }
}
#endif