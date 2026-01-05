#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore.Editor
{
    public static class ConvoCoreYamlWriteback
    {
        public static bool TryGetWritableYamlAssetPath(ConvoCoreConversationData data, out string assetPath)
        {
            assetPath = null;
            if (data == null) return false;

            // Prefer explicit linked source path if present
            if (!string.IsNullOrEmpty(data.SourceYamlAssetPath))
            {
                assetPath = data.SourceYamlAssetPath;
                return IsProjectTextFile(assetPath);
            }

            // Fallback: if ConversationYaml is an asset file, we can write it
            if (data.ConversationYaml != null)
            {
                var p = AssetDatabase.GetAssetPath(data.ConversationYaml);
                if (IsProjectTextFile(p))
                {
                    assetPath = p;
                    return true;
                }
            }

            return false;
        }

        public static bool TryWriteYamlToAssetPath(string assetPath, string yamlText)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (yamlText == null) yamlText = "";

            var fullPath = Path.GetFullPath(assetPath);
            File.WriteAllText(fullPath, yamlText);
            AssetDatabase.ImportAsset(assetPath);
            return true;
        }

        private static bool IsProjectTextFile(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/")) return false;

            var ext = Path.GetExtension(assetPath).ToLowerInvariant();
            return ext == ".yml" || ext == ".yaml" || ext == ".txt";
        }
    }
}
#endif