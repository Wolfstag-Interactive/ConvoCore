using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WolfstagInteractive.ConvoCore
{
    public interface ICharacterRepresentation
    {
        // Called once to initialize the representation.
        void Initialize();

        // Called to update the character's appearance (for example, changing an emotion).
        void SetEmotion(string emotionID);
    
        void Show();
        void Hide();
        void Dispose();
    }

    public abstract class CharacterRepresentationBase : ScriptableObject, ICharacterRepresentation
    {
        public abstract void Initialize();
        public abstract void SetEmotion(string emotionID);
        public abstract void Show();
        public abstract void Hide();
        public abstract void Dispose();
    }

}