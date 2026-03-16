using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Presence type for characters that are fully managed by the developer.
    /// ConvoCore never spawns, parents, or destroys anything.
    ///
    /// <list type="bullet">
    ///   <item>Scene-resident characters: resolved via <see cref="ConvoCoreSceneCharacterRegistry"/> on the spawner.</item>
    ///   <item>Spawn-from-prefab characters: returns null (the developer is responsible for placing and managing these).</item>
    /// </list>
    ///
    /// <see cref="OnConversationEnd"/> is a no-op. The developer is fully responsible for
    /// character lifecycle in both source modes.
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
            if (representation.SourceMode == CharacterSourceMode.SceneResident)
            {
                if (!spawner.TryGetSceneResident(representation.SceneCharacterId, out var display))
                {
                    Debug.LogWarning($"[ExternalPresence] Scene-resident character '{representation.SceneCharacterId}' " +
                                     $"not found in registry. Is a ConvoCoreSceneCharacterRegistrant in the scene?");
                    return null;
                }
                return display;
            }

            // SpawnFromPrefab: this presence does not spawn. Return null.
            return null;
        }
    }
}
