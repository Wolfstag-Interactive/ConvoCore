using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Presence type for characters that are fully managed by the developer.
    /// ConvoCore never spawns, parents, or destroys anything.
    ///
    /// The character is resolved via <see cref="ConvoCoreSceneCharacterRegistry"/> using
    /// <see cref="CharacterPresenceContext.CharacterId"/>. If no registrant is found for that ID
    /// (or the ID is empty) this presence returns null.
    ///
    /// <see cref="OnConversationEnd"/> is a no-op. The developer is fully responsible for
    /// character lifecycle.
    ///
    /// Use case: characters already placed in the world by the developer, with no ConvoCore lifecycle involvement.
    /// </summary>
    [CreateAssetMenu(fileName = "ExternalPresence", menuName = "ConvoCore/Presence/External Presence")]
    public class ExternalPresence : ConvoCoreCharacterPresence
    {
        public override IConvoCoreCharacterDisplay ResolvePresence(
            PrefabCharacterRepresentationData representation,
            CharacterPresenceContext context,
            ConvoCorePrefabRepresentationSpawner spawner)
        {
            if (!spawner.TryGetSceneResident(context.CharacterId, out var display))
            {
                Debug.LogWarning($"[ExternalPresence] Scene-resident character '{context.CharacterId}' " +
                                 $"not found in registry. Is a ConvoCoreSceneCharacterRegistrant present in the scene?");
                return null;
            }
            return display;
        }
    }
}
