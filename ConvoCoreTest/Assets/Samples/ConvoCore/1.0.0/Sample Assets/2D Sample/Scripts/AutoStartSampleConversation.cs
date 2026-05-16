using UnityEngine;
[HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1AutoStartSampleConversation.html")]
[RequireComponent(typeof(WolfstagInteractive.ConvoCore.ConvoCore))]
public class AutoStartSampleConversation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<WolfstagInteractive.ConvoCore.ConvoCore>().PlayConversation();
    }
}