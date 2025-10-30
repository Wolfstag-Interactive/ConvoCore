using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(fileName = "HistoryRendererProfile", menuName = "ConvoCore/Dialogue/History Renderer Profile", order = 50)]
    public class ConvoCoreHistoryRendererProfile : ScriptableObject
    {
        [SerializeField] private string rendererName = "Rich";

        public string RendererName => rendererName;

        public void UpdateFromDiscovered(string newRendererName)
        {
            rendererName = newRendererName;
            name = $"{newRendererName}RendererProfile";
        }
    }
}