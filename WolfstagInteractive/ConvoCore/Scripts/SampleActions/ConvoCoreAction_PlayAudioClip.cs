using UnityEngine;
using System.Collections;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(menuName = "ConvoCore/Actions/PlayAudioClip")] [System.Serializable]
    public class ConvoCoreAction_PlayAudioClip : BaseAction
    {
        public AudioClip AudioClip;
        public Vector3 Position;
        [Range(0,1)]
        public float Volume = 1f;

        public override IEnumerator DoAction()
        {
            AudioSource.PlayClipAtPoint(AudioClip, Position,Volume);
            yield return new WaitForSeconds(AudioClip.length);
        }

    }
}