using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "HistoryRendererProfile", menuName = "ConvoCore/Dialogue/History Renderer Profile", order = 50)]
    public class ConvoCoreHistoryRendererProfile : ScriptableObject
    {
        [SerializeField] private string rendererName = "Rich";
        [SerializeField] private Color defaultSpeakerColor = Color.white;
        [SerializeField] private bool isDefault = false;

        public string RendererName => rendererName;
        public Color DefaultSpeakerColor => defaultSpeakerColor;
        public bool IsDefault => isDefault;

        public void UpdateFromDiscovered(string newRendererName)
        {
            rendererName = newRendererName;
            name = $"{newRendererName}RendererProfile";
        }
    }
}