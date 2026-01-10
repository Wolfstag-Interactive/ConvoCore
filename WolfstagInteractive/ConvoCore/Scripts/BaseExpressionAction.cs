using UnityEngine;
using WolfstagInteractive.ConvoCore;

[UnityEngine.HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1BaseExpressionAction.html")]

public abstract class BaseExpressionAction : ScriptableObject
{
    /// <summary>
    /// Executes this action 
    /// </summary>
    /// <param name="context"></param>
    public abstract void ExecuteAction(ExpressionActionContext context);
}
/// <summary>
/// Context passed to BaseExpressionAction when a character expression is applied.
/// </summary>
public struct ExpressionActionContext
{
    // Dialogue side
    public ConvoCore Runtime;
    public ConvoCoreConversationData Conversation;
    public int LineIndex;

    // Representation side
    public CharacterRepresentationBase Representation;
    public IConvoCoreCharacterDisplay Display;

    // Expression info
    public string ExpressionId;
}