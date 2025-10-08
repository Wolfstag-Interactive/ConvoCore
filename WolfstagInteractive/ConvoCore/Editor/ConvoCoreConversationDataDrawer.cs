using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreConversationDataDrawer.html")]
    [CustomPropertyDrawer(typeof(ConvoCoreConversationData), true)]
    public class ConvoCoreConversationDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var so = property.serializedObject;
            so.Update();

            // Draw everything normally except DialogueLines
            var iterator = property.Copy();
            var end = iterator.GetEndProperty();

            bool expanded = EditorGUILayout.Foldout(true, label, true);
            if (expanded)
            {
                EditorGUI.indentLevel++;
                while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
                {
                    if (iterator.name == "DialogueLines")
                    {
                        // Integrate paging here
                        PagedListUtility.DrawPagedList(iterator, 20);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
                EditorGUI.indentLevel--;
            }

            so.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }
    }
}