#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1ConvoInputPropertyDrawer.html")]
[CustomPropertyDrawer(typeof(IConvoInput), true)]
    public class ConvoInputPropertyDrawer : PropertyDrawer
    {
        private static readonly System.Type[] s_Types =
        {
            typeof(SingleConversationInput),
            typeof(ContainerInput)
        };
        private static readonly string[] s_Tabs = { "Single", "Container" };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Extra lines for header + toolbar
            float h = EditorGUIUtility.singleLineHeight * 2 + 4f;

            // Height of the concrete object children
            var copy = property.Copy();
            var end  = copy.GetEndProperty();
            while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, end))
            {
                if (copy.name == "managedReferenceFullTypename" || copy.name == "managedReferenceData")
                    continue;
                h += EditorGUI.GetPropertyHeight(copy, true) + 2f;
            }
            return h + 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Header
            var header = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(header, "Conversation Input", EditorStyles.boldLabel);

            // Toolbar
            var bar = new Rect(position.x, header.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
            int idx = GetTypeIndex(property);
            int newIdx = GUI.Toolbar(bar, Mathf.Max(0, idx), s_Tabs);
            if (newIdx != idx)
            {
                SetManagedReferenceType(property, s_Types[newIdx]);
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
            }

            // Body rect
            var body = new Rect(position.x, bar.yMax + 2, position.width, position.yMax - (bar.yMax + 2));

            // Draw children inline
            EditorGUI.indentLevel++;
            DrawChildrenInline(body, property);
            EditorGUI.indentLevel--;

            // Handle drag-and-drop onto the whole drawer
            HandleDragAndDrop(position, property);
        }

        private static void DrawChildrenInline(Rect rect, SerializedProperty property)
        {
            var copy = property.Copy();
            var end  = copy.GetEndProperty();
            float y = rect.y;

            while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, end))
            {
                if (copy.name == "managedReferenceFullTypename" || copy.name == "managedReferenceData")
                    continue;

                float h = EditorGUI.GetPropertyHeight(copy, true);
                var line = new Rect(rect.x, y, rect.width, h);
                EditorGUI.PropertyField(line, copy, true);
                y += h + 2f;
            }
        }

        private static int GetTypeIndex(SerializedProperty prop)
        {
            var t = GetManagedType(prop);
            for (int i = 0; i < s_Types.Length; i++) if (t == s_Types[i]) return i;
            return 0;
        }

        private static System.Type GetManagedType(SerializedProperty prop)
        {
            var full = prop.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return null;
            var parts = full.Split(' ');
            return System.Type.GetType($"{parts[1]}, {parts[0]}");
        }

        private static void SetManagedReferenceType(SerializedProperty prop, System.Type t)
        {
            prop.managedReferenceValue = System.Activator.CreateInstance(t);
        }

        private static void HandleDragAndDrop(Rect dropRect, SerializedProperty root)
        {
            var evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated)
            {
                if (CanAcceptDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                }
            }
            else if (evt.type == EventType.DragPerform)
            {
                if (!CanAcceptDrag()) return;

                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is ConvoCoreConversationData convo)
                    {
                        // Switch to Single and assign Conversation
                        SetManagedReferenceType(root, typeof(SingleConversationInput));
                        root.serializedObject.ApplyModifiedProperties();
                        root.serializedObject.Update();

                        var convProp = root.FindPropertyRelative("Conversation");
                        if (convProp != null)
                        {
                            convProp.objectReferenceValue = convo;
                            root.serializedObject.ApplyModifiedProperties();
                        }
                        Event.current.Use();
                        break;
                    }
                    if (obj is ConversationContainer container)
                    {
                        // Switch to Container and assign Container
                        SetManagedReferenceType(root, typeof(ContainerInput));
                        root.serializedObject.ApplyModifiedProperties();
                        root.serializedObject.Update();

                        var contProp = root.FindPropertyRelative("Container");
                        if (contProp != null)
                        {
                            contProp.objectReferenceValue = container;
                            root.serializedObject.ApplyModifiedProperties();
                        }
                        Event.current.Use();
                        break;
                    }
                }
            }

            static bool CanAcceptDrag()
            {
                foreach (var o in DragAndDrop.objectReferences)
                    if (o is ConvoCoreConversationData || o is ConversationContainer)
                        return true;
                return false;
            }
        }
    }
}
#endif