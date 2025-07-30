using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public partial class ConvoCoreConversationData
    {
        [Serializable]
        public struct CharacterRepresentationData
        {
            [Tooltip("The ID of the selected character profile (for secondary/tertiary characters).")]
            public string SelectedCharacterID; 
            public string SelectedRepresentationName;
            public CharacterRepresentationBase SelectedRepresentation;
            [Tooltip("Specify the emotion for this dialogue line by name.")]
            public string SelectedRepresentationEmotion; // Name of the emotion selected via dropdown

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
        public class DialogueLineInfo
        {
            public string ConversationID; // Key or ConversationID in the YAML
            public int ConversationLineIndex; // Line index within the conversation
            public string characterID; //ID of the character speaking the line

            [Tooltip("Primary character representation (usually the speaker).")]
            public CharacterRepresentationData PrimaryCharacterRepresentation = new CharacterRepresentationData();

            [Tooltip("Optional representation for a secondary character.")]
            public CharacterRepresentationData SecondaryCharacterRepresentation = new CharacterRepresentationData();

            [Tooltip("Optional representation for a tertiary character.")]
            public CharacterRepresentationData TertiaryCharacterRepresentation = new CharacterRepresentationData();

            public List<LocalizedDialogue> LocalizedDialogues; // Localized dialogues per language
            public AudioClip clip; // Audio associated with the line
            public List<BaseAction> ActionsBeforeDialogueLine; // Actions before the dialogue line
            public List<BaseAction> ActionsAfterDialogueLine; // Actions after the dialogue line

            public DialogueLineProgressionMethod
                UserInputMethod; // Whether to wait for user input before continuing to the next line

            public float TimeBeforeNextLine; // Time in seconds to wait before continuing to the next line

            // Add a constructor to ensure initialization
            public DialogueLineInfo(string conversationID)
            {
                ConversationID = conversationID;
                ConversationLineIndex = 0;
                characterID = "";
                PrimaryCharacterRepresentation = new CharacterRepresentationData();
                SecondaryCharacterRepresentation = new CharacterRepresentationData();
                TertiaryCharacterRepresentation = new CharacterRepresentationData();
                LocalizedDialogues = new List<LocalizedDialogue>();
                clip = null;
                ActionsBeforeDialogueLine = new List<BaseAction>();
                ActionsAfterDialogueLine = new List<BaseAction>();
                UserInputMethod = DialogueLineProgressionMethod.UserInput;
                TimeBeforeNextLine = 0f;
            }
        }
    }
}