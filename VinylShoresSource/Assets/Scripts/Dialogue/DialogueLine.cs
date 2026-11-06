using System;
using UnityEngine;

namespace VinylShores.Dialogue
{
    /// <summary>
    /// One line of dialogue. Kept serializable so lines can be authored directly
    /// on a <see cref="DialogueInteractable"/> in the inspector.
    /// </summary>
    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker;

        [TextArea(2, 6)]
        public string text;
    }
}
