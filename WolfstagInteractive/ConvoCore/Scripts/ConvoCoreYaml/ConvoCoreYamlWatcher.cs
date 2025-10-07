using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreYamlWatcher.html")]
    public class ConvoCoreYamlWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            // Find all ConversationData assets
            var guids = AssetDatabase.FindAssets("t:WolfstagInteractive.ConvoCore.ConvoCoreConversationData");
            if (guids == null || guids.Length == 0) return;

            var importedSet = new HashSet<string>(imported);

            // Map old -> new paths for moved/renamed assets
            var movedMap = new Dictionary<string, string>();
            if (moved != null && movedFrom != null)
            {
                int len = Mathf.Min(moved.Length, movedFrom.Length);
                for (int i = 0; i < len; i++)
                {
                    var from = movedFrom[i];
                    var to = moved[i];
                    if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
                        movedMap[from] = to;
                }
            }

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase
                    .LoadAssetAtPath<WolfstagInteractive.ConvoCore.ConvoCoreConversationData>(assetPath);
                if (data == null) continue;

                // Read the stored source link (hidden field)
                var so = new SerializedObject(data);
                var srcPathProp = so.FindProperty("SourceYamlAssetPath");
                if (srcPathProp == null) continue;

                var linkedPath = srcPathProp.stringValue;
                if (string.IsNullOrEmpty(linkedPath)) continue;

                // 1) If the linked source .yaml was moved/renamed, update the stored path
                if (movedMap.TryGetValue(linkedPath, out var newPath))
                {
                    srcPathProp.stringValue = newPath;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssets();

                    Debug.Log(
                        $"ConvoCore: Updated SourceYamlAssetPath after move/rename:\n  {linkedPath} -> {newPath}\n  Asset: {assetPath}");

                    // Optional: if the moved file was also reimported this frame, you can re-embed now
                    if (importedSet.Contains(newPath))
                    {
                        if (TryEmbedFromPath(data, newPath))
                        {
                            so.Update();
                            so.ApplyModifiedPropertiesWithoutUndo();
                            EditorUtility.SetDirty(data);
                            AssetDatabase.SaveAssets();
                            Debug.Log(
                                $"ConvoCore: Auto-synced embedded YAML after move from '{newPath}' into '{assetPath}'.");
                        }
                    }

                    // Use updated path for subsequent checks
                    linkedPath = newPath;
                }

                // 2) If the (possibly updated) linked source .yaml was reimported, refresh the embedded copy
                if (importedSet.Contains(linkedPath))
                {
                    if (TryEmbedFromPath(data, linkedPath))
                    {
                        so.Update();
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(data);
                        AssetDatabase.SaveAssets();
                        Debug.Log($"ConvoCore: Auto-synced embedded YAML from '{linkedPath}' into '{assetPath}'.");
                    }
                }
            }
        }

        // Helper: embed text as sub-asset only if it actually changed (prevents churn)
        private static bool TryEmbedFromPath(WolfstagInteractive.ConvoCore.ConvoCoreConversationData data,
            string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath)) return false;

            // Load source text (from TextAsset if possible, else from disk)
            string srcText = null;
            var srcObj = AssetDatabase.LoadAssetAtPath<Object>(sourcePath);
            if (srcObj is TextAsset ta) srcText = ta.text;
            else srcText = File.ReadAllText(sourcePath);
            if (srcText == null) return false;

            // Determine current embedded text (if any)
            string currentText = null;

            if (data.ConversationYaml is TextAsset currentTA)
            {
                currentText = currentTA.text;
            }
            else
            {
                // Fallback: look for a sub-asset named "EmbeddedYaml"
                var convPath = AssetDatabase.GetAssetPath(data);
                if (!string.IsNullOrEmpty(convPath))
                {
                    var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(convPath);
                    if (reps != null)
                    {
                        foreach (var rep in reps)
                        {
                            if (rep is TextAsset repTa && repTa.name == "EmbeddedYaml")
                            {
                                currentText = repTa.text;
                                break;
                            }
                        }
                    }
                }
            }

            // 2) Stop re-embedding when nothing changed (prevents churn)
            if (currentText != null && currentText == srcText)
            {
                // No change, skip re-embed
                return false;
            }

            // Replace the embedded sub-asset with the new text
            // Remove existing embedded (if any)
            if (data.ConversationYaml != null)
            {
                Object.DestroyImmediate(data.ConversationYaml, true);
                data.ConversationYaml = null;
                AssetDatabase.SaveAssets();
            }

            // Also clean up any stray "EmbeddedYaml" representations
            var convAssetPath = AssetDatabase.GetAssetPath(data);
            if (!string.IsNullOrEmpty(convAssetPath))
            {
                var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(convAssetPath);
                if (reps != null)
                {
                    foreach (var rep in reps)
                    {
                        if (rep is TextAsset repTa && repTa.name == "EmbeddedYaml")
                        {
                            Object.DestroyImmediate(repTa, true);
                        }
                    }

                    AssetDatabase.SaveAssets();
                }
            }

            // Create & add the new embedded TextAsset
            var embedded = new TextAsset(srcText) { name = "EmbeddedYaml" };
            AssetDatabase.AddObjectToAsset(embedded, data);
            data.ConversationYaml = embedded;

            // Optional: auto-fill FilePath for persistent/Addressables fallbacks if empty
            if (string.IsNullOrEmpty(data.FilePath))
            {
                var baseName = Path.GetFileNameWithoutExtension(sourcePath);
                data.FilePath = $"ConvoCore/Dialogue/{baseName}";
            }

            return true;
        }
    }
}