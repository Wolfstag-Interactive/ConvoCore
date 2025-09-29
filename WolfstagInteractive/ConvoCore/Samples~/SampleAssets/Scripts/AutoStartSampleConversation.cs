using UnityEngine;
using WolfstagInteractive.ConvoCore;
[RequireComponent(typeof(ConvoCore))]
public class AutoStartSampleConversation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<ConvoCore>().StartConversation();
    }
}