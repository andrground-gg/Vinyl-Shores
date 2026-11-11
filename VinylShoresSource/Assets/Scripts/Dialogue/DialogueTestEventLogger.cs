using UnityEngine;

namespace VinylShores.Dialogue
{
    /// <summary>
    /// Test helper for the dialogue prototype. Logs when an NPC's dialogue
    /// finishes so the test scene can verify that only the owning NPC raises
    /// the event. Safe to remove once the integration tests are in place.
    /// </summary>
    public sealed class DialogueTestEventLogger : MonoBehaviour
    {
        public void LogFinished()
        {
            Debug.Log(name + " dialogue finished.", this);
        }
    }
}
