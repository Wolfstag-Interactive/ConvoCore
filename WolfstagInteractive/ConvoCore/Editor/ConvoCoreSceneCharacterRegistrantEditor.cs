#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1ConvoCoreSceneCharacterRegistrantEditor.html")]
[CustomEditor(typeof(ConvoCoreSceneCharacterRegistrant))]
    public class ConvoCoreSceneCharacterRegistrantEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var registrant = (ConvoCoreSceneCharacterRegistrant)target;

            // Check whether a registry exists in the scene. Use FindObjectsByType when available
            // (Unity 2023+), otherwise fall back to the deprecated FindObjectOfType.
            bool hasRegistry;
#if UNITY_2023_1_OR_NEWER
            hasRegistry = FindObjectsByType<ConvoCoreSceneCharacterRegistry>(FindObjectsSortMode.None).Length > 0;
#else
            hasRegistry = FindObjectOfType<ConvoCoreSceneCharacterRegistry>() != null;
#endif

            if (!hasRegistry)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(
                    "No ConvoCoreSceneCharacterRegistry found in the scene. " +
                    "Characters will not be registered at runtime. Add a registry or assign one directly.",
                    MessageType.Warning);

                if (GUILayout.Button("Add Registry to Scene"))
                {
                    var go = new GameObject("ConvoCoreSceneCharacterRegistry");
                    go.AddComponent<ConvoCoreSceneCharacterRegistry>();
                    Undo.RegisterCreatedObjectUndo(go, "Add ConvoCoreSceneCharacterRegistry");
                    Selection.activeGameObject = go;
                }
            }
        }
    }
}
#endif
