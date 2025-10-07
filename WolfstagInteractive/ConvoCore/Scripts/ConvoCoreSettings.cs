using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public enum TextSourceKind
    {
        AssignedTextAsset,
        Persistent,
        Addressables,
        Resources
    }

    [CreateAssetMenu(fileName = "ConvoCoreSettings", menuName = "ConvoCore/Settings")]
    public sealed class ConvoCoreSettings : ScriptableObject
    {
        [Header("Order the sources to try (first hit wins)")]
        public TextSourceKind[] SourceOrder = new[]
        {
            TextSourceKind.AssignedTextAsset,
            TextSourceKind.Persistent,
            TextSourceKind.Addressables,
            TextSourceKind.Resources
        };

        [Header("Resources")] public string resourcesRoot = "ConvoCore/Dialogue"; // only used if FilePath given

        [Header("Addressables (optional)")] public bool AddressablesEnabled = false; // flip on when project uses it
        public string AddressablesKeyTemplate = "{filePath}.yml"; // maps FilePath -> key

        [Header("Debug")] public bool VerboseLogs = false;
    }
}