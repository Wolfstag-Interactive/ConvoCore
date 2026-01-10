using System;
using System.Collections.Generic;
using UnityEngine;
namespace WolfstagInteractive.ConvoCore
{
[HelpURL("https://docs.wolfstaginteractive.com/convocore/api/classWolfstagInteractive_1_1ConvoCore_1_1ConvoCoreConversationData.html")]
    public partial class ConvoCoreConversationData
    {
        [Serializable]
        public struct CharacterRepresentationData
        {
            [Tooltip("The ID of the selected character profile (for secondary/tertiary characters).")]
            public string SelectedCharacterID; 
            public string SelectedRepresentationName;
            public CharacterRepresentationBase SelectedRepresentation;

            // This drawer shows DisplayName but stores GUID from the representation asset.
            [ExpressionIDSelector(nameof(SelectedRepresentation))]
            public string SelectedExpressionId;
           
            [Header("Per-Line Display Overrides")]
            [Tooltip("Display options specific to this dialogue line. These override the default expression settings.")]
            public DialogueLineDisplayOptions LineSpecificDisplayOptions;
        }
    }

    public partial class ConvoCoreConversationData
    {
        [Serializable]
        public enum DialogueLineProgressionMethod
        {
            UserInput,
            Timed
        }
    }

    public partial class ConvoCoreConversationData
    {
        [Serializable]
        public struct LocalizedDialogue
        {
            public string Language;
            public string Text;
        }
    }
    public partial class ConvoCoreConversationData
    {
        [Serializable]
        public enum LineContinuationMode
        {
            Continue,
            EndConversation,
            ContainerBranch
        }

        [Serializable]
        public struct LineContinuation
        {
            public LineContinuationMode Mode;
            public ConversationContainer TargetContainer;
            public string TargetAliasOrName;
            public bool PushReturnPoint;
        }

    }
    public partial class ConvoCoreConversationData
    {
        [Serializable]
        public class DialogueLineInfo
        {
            public string ConversationID;            // Key or ConversationID in the YAML
            public string LineID; // Stable Line ID
            public int ConversationLineIndex; // Line index within the conversation
            public string characterID; //ID of the character speaking the line
            [Tooltip("Ordered list of visible character representations for this line. Index 0 is the speaker.")]
            public List<CharacterRepresentationData> CharacterRepresentations = new();


            public List<LocalizedDialogue> LocalizedDialogues; // Localized dialogues per language
            public AudioClip clip; // Audio associated with the line
            public List<BaseDialogueLineAction> ActionsBeforeDialogueLine; // Actions before the dialogue line
            public List<BaseDialogueLineAction> ActionsAfterDialogueLine; // Actions after the dialogue line

            public DialogueLineProgressionMethod
                UserInputMethod; // Whether to wait for user input before continuing to the next line

            public float TimeBeforeNextLine; // Time in seconds to wait before continuing to the next line
            
            public LineContinuation LineContinuationSettings;
            
            // Add a constructor to ensure initialization
            public DialogueLineInfo(string conversationID)
            {
                ConversationID = conversationID;
                ConversationLineIndex = 0;
                LineID = null;
                characterID = "";
                LocalizedDialogues = new List<LocalizedDialogue>();
                clip = null;
                ActionsBeforeDialogueLine = new List<BaseDialogueLineAction>();
                ActionsAfterDialogueLine = new List<BaseDialogueLineAction>();
                UserInputMethod = DialogueLineProgressionMethod.UserInput;
                TimeBeforeNextLine = 0f;
                LineContinuationSettings = new LineContinuation
                {
                    Mode = LineContinuationMode.Continue, 
                    TargetAliasOrName= null,
                    TargetContainer =  null,
                    PushReturnPoint = false
                };
            }
            public void EnsureCharacterRepresentationListInitialized()
            {
                CharacterRepresentations ??= new List<CharacterRepresentationData>();
                if (CharacterRepresentations.Count == 0)
                {
                    CharacterRepresentations.Add(new CharacterRepresentationData());
                }            
            }
            private static bool HasAnySelection(CharacterRepresentationData data)
            {
                return !string.IsNullOrEmpty(data.SelectedCharacterID)
                       || !string.IsNullOrEmpty(data.SelectedRepresentationName)
                       || data.SelectedRepresentation != null
                       || !string.IsNullOrEmpty(data.SelectedExpressionId);
            }
        }
    }
}