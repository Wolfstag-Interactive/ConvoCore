using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Input strategy interface. Implement to define how and which conversation(s) a
    /// <see cref="ConvoCore"/> runner should play. The two built-in implementations are
    /// <see cref="SingleConversationInput"/> for a single fixed conversation, and
    /// <see cref="ContainerInput"/> for container-driven selection.
    /// </summary>
    public interface IConvoInput
    {
        void Play(MonoBehaviour host, IConvoCoreRunner runner);
    }

    /// <summary>
    /// Plays a single <see cref="ConvoCoreConversationData"/> asset directly.
    /// The simplest input strategy — suitable for NPCs with one fixed conversation.
    /// </summary>
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

    
    /// <summary>
    /// Plays one or more conversations from a <see cref="ConversationContainer"/> asset.
    /// Use this strategy when you want to pick a conversation by alias, play a playlist,
    /// or use random or weighted selection.
    /// </summary>
    [System.Serializable]
    public sealed class ContainerInput : IConvoInput
    {
        public ConversationContainer Container;
        public string StartAlias;
        public bool? LoopOverride; // null = use container’s own Loop

        public void Play(MonoBehaviour host, IConvoCoreRunner runner)
        {
            if (Container == null)
            {
                Debug.LogWarning("[ConvoCore] No container assigned."); return;
            }
            host.StartCoroutine(
                ConversationContainerRuntime.Play(Container, runner, StartAlias, LoopOverride, hubSelector: null));
        }
    }
    

}