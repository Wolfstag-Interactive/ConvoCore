using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WolfstagInteractive.ConvoCore;

namespace WolfstagInteractive.ConvoCore
{
    [CreateAssetMenu(menuName = "ConvoCore/Actions/Action Group")][ System.Serializable]
    
    public class ConvoCoreAction_ActionGroup : BaseAction
    {
        /// <summary>
        /// Add commonly executed actions together for easy reuse, executes each action in the list one after the other.
        /// </summary>
        public List<BaseAction> ActionGroup = new List<BaseAction>();
        
        public override IEnumerator DoAction()
        {
            foreach (var t in ActionGroup)
            {
                yield return t.DoAction();
            }
        }
    }
}