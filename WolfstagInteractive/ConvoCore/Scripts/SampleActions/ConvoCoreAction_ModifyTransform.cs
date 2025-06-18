using UnityEngine;
using System.Collections;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(menuName = "ConvoCore/Actions/ModifyTransform")] [System.Serializable]
    public class ConvoCoreAction_ModifyTransform : BaseAction
    {
        public string TransformName;
        public Vector3 NewPosition;
        public Vector3 NewRotation;
        public Vector3 NewScale;
        public override IEnumerator DoAction()
        {
            Transform transform = GameObject.Find(TransformName).transform;
            if (transform == null)
            {
                Debug.LogError("Transform not found");
                yield break;
            }
            transform.SetPositionAndRotation(NewPosition, Quaternion.Euler(NewRotation));
            transform.localScale = NewScale;
            yield return null; 
        }
    }
}