using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public interface IConvoCoreCharacterDisplay
    {
        /// <summary> Inject the representation asset (emotion catalog, single source of truth). </summary>
        void BindRepresentation(PrefabCharacterRepresentationData representationAsset);

        /// <summary> Apply emotion by GUID. </summary>
        void ApplyEmotion(string emotionId);

        /// <summary> Apply per-line display overrides (scale/flip/position/etc.). </summary>
        void ApplyDisplayOptions(DialogueLineDisplayOptions options);
    }
}