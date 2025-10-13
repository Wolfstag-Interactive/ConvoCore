#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace WolfstagInteractive.ConvoCore.Editor
{
    [CustomEditor(typeof(ConvoCoreSettings))]
    public class ConvoCoreSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _sourceOrderProp;
        private SerializedProperty _resourcesRootProp;
        private SerializedProperty _addressablesEnabledProp;
        private SerializedProperty _addressablesKeyTemplateProp;
        private SerializedProperty _supportedLanguagesProp;
        private SerializedProperty _currentLanguageProp;
        private SerializedProperty _verboseLogsProp;
        private SerializedProperty _historyRendererProfilesProp;

        private void OnEnable()
        {
            _sourceOrderProp = serializedObject.FindProperty("SourceOrder");
            _resourcesRootProp = serializedObject.FindProperty("resourcesRoot");
            _addressablesEnabledProp = serializedObject.FindProperty("AddressablesEnabled");
            _addressablesKeyTemplateProp = serializedObject.FindProperty("AddressablesKeyTemplate");
            _supportedLanguagesProp = serializedObject.FindProperty("SupportedLanguages");
            _currentLanguageProp = serializedObject.FindProperty("CurrentLanguage");
            _verboseLogsProp = serializedObject.FindProperty("VerboseLogs");

            // new
            _historyRendererProfilesProp = serializedObject.FindProperty("historyRendererProfiles");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var settings = (ConvoCoreSettings)target;

            // YAML Source Order
            EditorGUILayout.LabelField("YAML Source Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_sourceOrderProp, true);
            EditorGUILayout.Space();

            // Resources
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_resourcesRootProp);
            EditorGUILayout.Space();

            // Addressables
            EditorGUILayout.LabelField("Addressables (Optional)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_addressablesEnabledProp);
            if (_addressablesEnabledProp.boolValue)
                EditorGUILayout.PropertyField(_addressablesKeyTemplateProp);
            EditorGUILayout.Space();

            // Language Settings
            EditorGUILayout.LabelField("Language Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_supportedLanguagesProp, new GUIContent("Supported Languages"), true);

            // Current Language Dropdown
            if (settings.SupportedLanguages != null && settings.SupportedLanguages.Count > 0)
            {
                int currentIndex = settings.SupportedLanguages.IndexOf(settings.CurrentLanguage);
                if (currentIndex < 0) currentIndex = 0;

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Current Language", currentIndex, settings.SupportedLanguages.ToArray());
                if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < settings.SupportedLanguages.Count)
                {
                    _currentLanguageProp.stringValue = settings.SupportedLanguages[newIndex];
                    ConvoCoreLanguageManager.Instance?.SetLanguage(settings.SupportedLanguages[newIndex]);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Add at least one supported language!", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_verboseLogsProp);
            EditorGUILayout.Space();

           
            EditorGUILayout.LabelField("Dialogue History Renderers", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_historyRendererProfilesProp, new GUIContent("Renderer Profiles"), true);

            if (GUILayout.Button("Auto-Populate Renderer Profiles"))
            {
                PopulateRendererProfiles(settings);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Renderer Profiles define which dialogue history renderers are available. " +
                "Click 'Auto-Populate' to discover and generate profiles for all IConvoCoreHistoryRenderer implementations in your project.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------------
        // Helper: Auto-populate renderer profiles
        // ------------------------------------------------------------------
        private void PopulateRendererProfiles(ConvoCoreSettings settings)
        {
            ConvoCoreHistoryRendererRegistry.DiscoverRenderers();
            var names = ConvoCoreHistoryRendererRegistry.GetRendererNames();

            if (names == null || names.Length == 0)
            {
                Debug.LogWarning("[ConvoCoreSettings] No IConvoCoreHistoryRenderer implementations found.");
                return;
            }

            string folder = "Assets/ConvoCore/Generated/RendererProfiles";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            int created = 0;

            foreach (var name in names)
            {
                // check if profile already exists
                if (settings.HistoryRendererProfiles != null &&
                    settings.HistoryRendererProfiles.Any(p => p != null && p.RendererName == name))
                    continue;

                var profile = ScriptableObject.CreateInstance<ConvoCoreHistoryRendererProfile>();
                profile.UpdateFromDiscovered(name);

                string assetPath = Path.Combine(folder, $"{name}RendererProfile.asset");
                AssetDatabase.CreateAsset(profile, assetPath);
                settings.AddRendererProfile(profile);
                created++;
            }

            settings.CleanRendererProfiles();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ConvoCoreSettings] Added {created} new renderer profile(s) to settings.");
        }
    }
}
#endif