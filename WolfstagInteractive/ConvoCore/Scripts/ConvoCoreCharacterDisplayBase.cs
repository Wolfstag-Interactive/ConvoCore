using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    /// <summary>
    /// Generic base class for all prefab-based character displays.
    /// Applies scale, flip, and side offsets using ConvoCore's side-based layout model.
    /// </summary>
    public abstract class ConvoCoreCharacterDisplayBase : MonoBehaviour, IConvoCoreCharacterDisplay
    {
        [Header("Display Root Settings")]
        [Tooltip("The root object to scale/flip/position. Defaults to this GameObject.")]
        [SerializeField] protected Transform visualRoot;

        [SerializeField] private Vector3 leftPositionOffset = new Vector3(-3f, 0f, 0f);
        [SerializeField] private Vector3 rightPositionOffset = new Vector3(3f, 0f, 0f);
        [SerializeField] private Vector3 centerPositionOffset = Vector3.zero;
        
        [Tooltip("Should the root object be flipped horizontally for side-facing characters?")]
        [SerializeField] protected bool supportFlipX = true;

        [Tooltip("Should the root object be flipped vertically?")]
        [SerializeField] protected bool supportFlipY = false;
        
        protected virtual void Awake()
        {
            if (visualRoot == null)
                visualRoot = transform;

        }

        public virtual void ApplyDisplayOptions(DialogueLineDisplayOptions options)
        {
            // Scale
            visualRoot.localScale = options.FullBodyScale;

            // Flip X/Y
            var scale = visualRoot.localScale;
            if (supportFlipX && options.FlipFullBodyX)
                scale.x *= -1;
            if (supportFlipY && options.FlipFullBodyY)
                scale.y *= -1;

            visualRoot.localScale = scale;

            // Move to anchor point
            Vector3 offset = options.FullBodySide switch
            {
                DisplaySide.Left => leftPositionOffset,
                DisplaySide.Right => rightPositionOffset,
                DisplaySide.Center => centerPositionOffset,
                _ => Vector3.zero
            };

            visualRoot.localPosition = offset;
        }

        public abstract void BindRepresentation(PrefabCharacterRepresentationData representationAsset);
      

        public abstract void ApplyEmotion(string emotionId);
    }
}