using UnityEngine;
using System.Collections;

/// <summary>
/// Base Action serves as the base for all derivative classes,
/// allowing them to all automatically share the values and functions within this script
/// Derivative classes must inherenit from BaseAction in order to work and should not derive from
/// monobehavior or scriptable object on their own
///
/// IMPORTANT!!!!!!!!!!
/// Make sure To Copy the below commented out lines over the class name into the derivative class and remove the slashes on the beginning of each line
/// </summary>

// [CreateAssetMenu(fileName = "CustomActionTransform", menuName = "ConvoCore/CustomAction")] //creates button in unity to make copy of object make sure to change menu and file name to match new action or else it wont show up in editor
//[System.Serializable] // required to make sure values in the inspector are saved

namespace WolfstagInteractive.ConvoCore
{
    [HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1BaseAction.html")]
    [System.Serializable]
    public class BaseDialogueLineAction : ScriptableObject
    {
        [Tooltip("If true, this action will only execute once per conversation for a given line, even if the player reverses and replays that line.")]
        public bool RunOnlyOncePerConversation = false;
        /// <summary>
        /// Override this function in your custom line actions to perform custom logic that should occur when a dialogue line is presented to the user
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator ExecuteLineAction()
        {
            //logic called before base.DoAction will execute before the timer
            yield return new WaitForSecondsRealtime(0); //wait for the time listed in wait timer
            //logic called after base.DoAction will execute after the timer
        }
        /// <summary>
        /// Override this function in your custom line action to perform custom logic that should occur when the user goes back to a previous line.
        /// Use this function to reverse custom logic from execute line action
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator ExecuteOnReversedLineAction()
        {
            yield return new WaitForSecondsRealtime(0);
        }
    }
}