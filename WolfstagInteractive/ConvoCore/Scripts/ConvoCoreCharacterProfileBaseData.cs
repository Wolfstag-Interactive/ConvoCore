using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    [UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreCharacterProfileBaseData.html")]
[CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "ConvoCore/CharacterProfile")]
    public class ConvoCoreCharacterProfileBaseData : ScriptableObject
    {
        // Basic character information
        public bool IsPlayerCharacter;
        public string CharacterName;
        public string PlayerPlaceholder;
        public string CharacterID;
        public Color CharacterNameColor = Color.grey;
        public string CharacterDescription;

        [Tooltip("Each element maps a unique representation identifier (e.g., 'happy', 'angry') to a representation asset.")]
        public List<RepresentationPair> Representations = new List<RepresentationPair>();

        /// <summary>
        /// Returns the representation matching the provided representationId.
        /// If no specific representation ID is provided or found, the first representation in the list is returned.
        /// </summary>
        /// <param name="representationId">The identifier for the desired representation (e.g., 'happy', 'angry').</param>
        /// <returns>The matching representation or the first representation if none is found.</returns>
        public CharacterRepresentationBase GetRepresentation(string representationId)
        {
            // If no specific ID is provided, or if not found, return the first representation.
            if (string.IsNullOrEmpty(representationId) || Representations.All(r => r.CharacterRepresentationName != representationId))
            {
                if (Representations.Count > 0)
                {
                    return Representations[0].CharacterRepresentationType;
                }

                Debug.LogError($"No representations are defined for character '{CharacterName}'.");
                return null;
            }

            // Find the specified representation.
            var pair = Representations.FirstOrDefault(rep => rep.CharacterRepresentationName == representationId);
            return pair?.CharacterRepresentationType;
        }
    }

    [System.Serializable]
    public class RepresentationPair
    {
        [Tooltip("A unique identifier for this representation (e.g., 'default', 'happy', 'angry').")]
        public string CharacterRepresentationName;

        [Tooltip("The representation asset implementing the representation system (e.g., sprite or prefab based).")]
        public CharacterRepresentationBase CharacterRepresentationType;
    }
}