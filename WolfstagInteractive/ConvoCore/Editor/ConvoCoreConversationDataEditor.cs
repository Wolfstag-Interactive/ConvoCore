using System.IO;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1Editor_1_1ConvoCoreConversationDataEditor.html")]
[CustomEditor(typeof(ConvoCoreConversationData))]
    public class ConvoCoreConversationDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var convo = (ConvoCoreConversationData)target;

            // ── Presentation Mode ─────────────────────────────────────────────
            EditorGUILayout.LabelField("Presentation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultPresentationMode"),
                new GUIContent("Default Presentation Mode",
                    "Default mode applied to new lines during YAML sync. Does not retroactively change existing lines."));

            if (convo.DefaultPresentationMode == ConversationPresentationMode.AudioOnly &&
                convo.AudioManifest == null)
            {
                EditorGUILayout.HelpBox(
                    "Presentation mode is AudioOnly but no Audio Manifest is assigned. " +
                    "Lines will advance immediately with no audio or text output.",
                    MessageType.Warning);
            }

            if (GUILayout.Button("Apply Default Mode to All Lines"))
                ApplyDefaultModeToAllLines(convo);

            EditorGUILayout.Space(6f);

            // ── Audio Manifest ────────────────────────────────────────────────
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioManifest"),
                new GUIContent("Audio Manifest",
                    "Optional. Assign to enable voice clip playback for this conversation."));

            if (convo.AudioManifest == null)
            {
                if (GUILayout.Button("Create Audio Manifest"))
                    CreateAudioManifest(convo);
            }

            EditorGUILayout.Space(6f);

            // ── Default Inspector ─────────────────────────────────────────────
            DrawPropertiesExcluding(serializedObject,
                "m_Script",
                "DefaultPresentationMode",
                "AudioManifest");

            serializedObject.ApplyModifiedProperties();
        }

        private static void ApplyDefaultModeToAllLines(ConvoCoreConversationData convo)
        {
            if (convo.DialogueLines == null || convo.DialogueLines.Count == 0)
            {
                EditorUtility.DisplayDialog("Apply Mode", "No dialogue lines found.", "OK");
                return;
            }

            Undo.RecordObject(convo, "Apply Default Presentation Mode to All Lines");
            foreach (var line in convo.DialogueLines)
                if (line != null)
                    line.PresentationMode = convo.DefaultPresentationMode;
            EditorUtility.SetDirty(convo);
            Debug.Log($"[ConvoCore] Applied '{convo.DefaultPresentationMode}' to {convo.DialogueLines.Count} line(s) on '{convo.name}'.");
        }

        private static void CreateAudioManifest(ConvoCoreConversationData convo)
        {
            string convoPath = AssetDatabase.GetAssetPath(convo);
            string folder = Path.GetDirectoryName(convoPath)?.Replace('\\', '/') ?? "Assets";
            string assetName = convo.name + "_AudioManifest";
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{assetName}.asset");

            var manifest = CreateInstance<ConvoCoreAudioManifest>();
            manifest.Mode               = AudioManifestMode.ConversationDriven;
            manifest.SourceConversation = convo;

            AssetDatabase.CreateAsset(manifest, assetPath);

            // Sync rows from the conversation
            SyncManifestRows(manifest, convo);

            // Assign back to conversation
            Undo.RecordObject(convo, "Create Audio Manifest");
            convo.AudioManifest = manifest;
            EditorUtility.SetDirty(convo);

            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(manifest);
            Debug.Log($"[ConvoCore] Created audio manifest at '{assetPath}'.");
        }

        private static void SyncManifestRows(ConvoCoreAudioManifest manifest, ConvoCoreConversationData convo)
        {
            if (convo.DialogueLines == null) return;

            manifest.Entries = new System.Collections.Generic.List<ConvoCoreAudioManifest.AudioEntry>();

            foreach (var line in convo.DialogueLines)
            {
                if (line == null) continue;

                if (line.LocalizedDialogues != null && line.LocalizedDialogues.Count > 0)
                {
                    foreach (var ld in line.LocalizedDialogues)
                    {
                        manifest.Entries.Add(new ConvoCoreAudioManifest.AudioEntry
                        {
                            LineID      = line.LineID,
                            CharacterID = line.characterID,
                            Language    = ld.Language ?? ""
                        });
                    }
                }
                else
                {
                    manifest.Entries.Add(new ConvoCoreAudioManifest.AudioEntry
                    {
                        LineID      = line.LineID,
                        CharacterID = line.characterID,
                        Language    = ""
                    });
                }
            }

            EditorUtility.SetDirty(manifest);
        }
    }
}
