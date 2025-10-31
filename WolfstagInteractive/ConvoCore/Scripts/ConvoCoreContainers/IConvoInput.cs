using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public interface IConvoInput
    {
        void Play(MonoBehaviour host, IConvoCoreRunner runner);
    }
    [System.Serializable]
    public sealed class SingleConversationInput : IConvoInput
    {
        public ConvoCoreConversationData Conversation;

        public void Play(MonoBehaviour host, IConvoCoreRunner runner)
        {
            if (Conversation == null)
            {
                Debug.LogWarning("[ConvoCore] SingleConversationInput: Conversation is null.");
                return;
            }
            runner.PlayConversation(Conversation);
        }
    }

    
    [System.Serializable]
    public sealed class ContainerInput : IConvoInput
    {
        public ConversationContainer Container;
        public string StartAlias;
        public bool? LoopOverride; // null = use container’s own Loop

        public void Play(MonoBehaviour host, IConvoCoreRunner runner)
        {
            if (Container == null) { Debug.LogWarning("[ConvoCore] No container assigned."); return; }
            host.StartCoroutine(
                ConversationContainerRuntime.Play(Container, runner, StartAlias, LoopOverride, hubSelector: null));
        }
    }
    

}