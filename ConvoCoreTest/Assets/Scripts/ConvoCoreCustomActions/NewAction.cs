using UnityEngine;
using System.Collections;
using WolfstagInteractive.ConvoCore;

[CreateAssetMenu(menuName = "ConvoCore/Actions/NewAction")]
public class NewAction : BaseAction
{

        public override IEnumerator DoAction()
        {
            //add action logic here
            yield return null; 
            //alternatively you can use yield return new WaitForSecondsRealtime(amount); to wait for a certain amount of time before or after continuing
        }
        
}