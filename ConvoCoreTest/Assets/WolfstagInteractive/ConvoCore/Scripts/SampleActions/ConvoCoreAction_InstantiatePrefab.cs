using System.Collections;
using UnityEngine;
using WolfstagInteractive.ConvoCore;
[CreateAssetMenu(menuName = "ConvoCore/Actions/InstantiatePrefab")][ System.Serializable]
public class ConvoCoreAction_InstantiatePrefab : BaseAction
{
    public GameObject Prefab;
    public Vector3 Position;
    public Vector3 Rotation;

    public override IEnumerator DoAction()
    {
        Instantiate(Prefab, Position, Quaternion.Euler(Rotation));
        return base.DoAction();
    }
}