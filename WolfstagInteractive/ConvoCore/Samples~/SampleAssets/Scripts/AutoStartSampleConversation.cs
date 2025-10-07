using UnityEngine;
using WolfstagInteractive.ConvoCore;
[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/classWolfstagInteractive_1_1ConvoCore_1_1AutoStartSampleConversation.html")]
[RequireComponent(typeof(ConvoCore))]
public class AutoStartSampleConversation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<ConvoCore>().StartConversation();
    }
}