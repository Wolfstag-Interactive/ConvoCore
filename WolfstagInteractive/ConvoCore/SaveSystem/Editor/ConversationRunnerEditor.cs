#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using WolfstagInteractive.ConvoCore.SaveSystem;
using YamlDotNet.Serialization;

namespace WolfstagInteractive.ConvoCore.SaveSystem.Editor
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1SaveSystem_1_1Editor_1_1ConversationRunnerEditor.html")]
[CustomEditor(typeof(ConvoCoreConversationSaveManager))]
    public class ConversationRunnerEditor : UnityEditor.Editor
    {
        private SerializedProperty _saveManagerProp;
        private SerializedProperty _conversationIdProp;
        private SerializedProperty _autoCommitOnEndProp;
        private SerializedProperty _autoCommitOnStartProp;
        private SerializedProperty _autoCommitOnLineCompleteProp;
        private SerializedProperty _autoCommitOnChoiceMadeProp;
        private SerializedProperty _autoRestoreOnAwakeProp;
        private SerializedProperty _autoRestoreOnStartProp;
        private SerializedProperty _restoreBehaviorProp;

        private void OnEnable()
        {
            _saveManagerProp = serializedObject.FindProperty("SaveManager");
            _conversationIdProp = serializedObject.FindProperty("_conversationId");
            _autoCommitOnEndProp = serializedObject.FindProperty("_autoCommitOnEnd");
            _autoCommitOnStartProp = serializedObject.FindProperty("_autoCommitOnStart");
            _autoCommitOnLineCompleteProp = serializedObject.FindProperty("_autoCommitOnLineComplete");
            _autoCommitOnChoiceMadeProp = serializedObject.FindProperty("_autoCommitOnChoiceMade");
            _autoRestoreOnAwakeProp = serializedObject.FindProperty("_autoRestoreOnAwake");
            _autoRestoreOnStartProp = serializedObject.FindProperty("_autoRestoreOnStart");
            _restoreBehaviorProp = serializedObject.FindProperty("_restoreBehavior");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var runner = (ConvoCoreConversationSaveManager)target;

            // References
            EditorGUILayout.PropertyField(_saveManagerProp);
            EditorGUILayout.PropertyField(_conversationIdProp);

            // Validation warnings
            if (string.IsNullOrEmpty(_conversationIdProp.stringValue))
            {
                EditorGUILayout.HelpBox("Conversation ID is empty. This runner will not save or restore state.", MessageType.Warning);
            }
            else
            {
                // Check for duplicates in scene
                var allRunners = FindObjectsOfType<ConvoCoreConversationSaveManager>();
                int count = 0;
                for (int i = 0; i < allRunners.Length; i++)
                {
                    if (allRunners[i] != runner && allRunners[i].ConversationId == _conversationIdProp.stringValue)
                        count++;
                }
                if (count > 0)
                {
                    EditorGUILayout.HelpBox($"Conversation ID '{_conversationIdProp.stringValue}' is used by another runner in this scene. Snapshots will conflict.", MessageType.Warning);
                }
            }

            if (_saveManagerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Save Manager is not assigned. Saving and restoring will be skipped.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Auto-Commit
            EditorGUILayout.LabelField("Auto-Commit", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoCommitOnStartProp, new GUIContent("On Start"));
            EditorGUILayout.PropertyField(_autoCommitOnEndProp, new GUIContent("On End"));
            EditorGUILayout.PropertyField(_autoCommitOnLineCompleteProp, new GUIContent("On Line Complete"));
            EditorGUILayout.PropertyField(_autoCommitOnChoiceMadeProp, new GUIContent("On Choice Made"));

            EditorGUILayout.Space();

            // Auto-Restore
            EditorGUILayout.LabelField("Auto-Restore", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoRestoreOnAwakeProp, new GUIContent("On Awake"));
            EditorGUILayout.PropertyField(_autoRestoreOnStartProp, new GUIContent("On Start"));
            EditorGUILayout.PropertyField(_restoreBehaviorProp, new GUIContent("Restore Behavior"));

            // Play Mode section
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Play Mode State", EditorStyles.boldLabel);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Active Line ID", runner.ActiveLineId ?? "(none)");
                EditorGUILayout.Toggle("Is Complete", runner.IsComplete);
                EditorGUILayout.IntField("Visited Lines", runner.VisitedLinesCount);
                EditorGUILayout.Toggle("Is Dirty", runner.IsDirty);
                EditorGUILayout.TextField("Last Committed", runner.LastCommitTime != default
                    ? runner.LastCommitTime.ToString("HH:mm:ss")
                    : "(never)");
                EditorGUILayout.IntField("Conversation Variables", runner.ConversationVariablesCount);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();

                if (GUILayout.Button("Preview Snapshot"))
                {
                    var snapshot = runner.GetConversationSnapshot();
                    var serializer = new SerializerBuilder().Build();
                    var yaml = serializer.Serialize(snapshot);
                    Debug.Log($"[ConversationRunner] Snapshot Preview:\n{yaml}");
                }

                if (GUILayout.Button("Force Commit"))
                {
                    runner.CommitSnapshot();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif