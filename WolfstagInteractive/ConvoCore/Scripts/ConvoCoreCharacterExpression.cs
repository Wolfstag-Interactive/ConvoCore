using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreCharacterExpression.html")]
[CreateAssetMenu(fileName = "NewCharacterExpression", menuName = "ConvoCore/Character/Expression")]
    public class ConvoCoreCharacterExpression : ScriptableObject
    {
        public string expressionName;
        public Sprite CharacterExpressionSprite;

        [Header("Override Expression Sprites per Representation")]
        public List<RepresentationExpressionOverride> RepresentationOverrides;

        // Method to fetch expression sprite by representation ID (or default if none exists)
        public Sprite GetExpressionSpriteForRepresentation(string representationId)
        {
            if (string.IsNullOrEmpty(representationId))
                return CharacterExpressionSprite;

            // Check if there is a specific override for this representation
            var overrideSprite = RepresentationOverrides.FirstOrDefault(r => 
                r.RepresentationID == representationId);
            return overrideSprite?.ExpressionSprite ?? CharacterExpressionSprite;
        }

    }

    [Serializable]
    public class RepresentationExpressionOverride
    {
        public string RepresentationID; // The ID of the alternative representation
        public Sprite ExpressionSprite; // Custom sprite for the expression in this representation
    }
}