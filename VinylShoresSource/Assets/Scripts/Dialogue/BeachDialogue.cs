using System;
using UnityEngine;
using VinylShores.Progress;
using VinylShores.Rhythm;

namespace VinylShores.Dialogue
{
    [RequireComponent(typeof(DialogueInteractable))]
    public sealed class BeachDialogue : MonoBehaviour, IDialogueSource
    {
        [Serializable]
        private struct GuideHint
        {
            public string RecordId;
            public DialogueLine[] First;
            public DialogueLine[] Repeat;
        }

        [Header("Old Spin")]
        [SerializeField] private GuideHint[] guideHints;
        [SerializeField] private DialogueLine[] beachComplete;

        [Header("Guardian")]
        [SerializeField] private string requiredRecord;
        [SerializeField] private DialogueLine[] locked;
        [SerializeField] private DialogueLine[] firstInvitation;
        [SerializeField] private DialogueLine[] repeatInvitation;
        [SerializeField] private DialogueLine[] replayInvitation;
        [SerializeField] private DialogueLine[] failure;
        [SerializeField] private DialogueLine[] success;

        private DialogueInteractable dialogue;
        private BeachRhythmChallenge challenge;
        private string lastHint;
        private bool invited;

        private void Awake()
        {
            dialogue = GetComponent<DialogueInteractable>();
            challenge = GetComponent<BeachRhythmChallenge>();
        }

        private void OnEnable()
        {
            if (challenge == null) return;
            challenge.onChallengeFailed.AddListener(ShowFailure);
            challenge.onRecordGranted.AddListener(ShowSuccess);
        }

        private void OnDisable()
        {
            if (challenge == null) return;
            challenge.onChallengeFailed.RemoveListener(ShowFailure);
            challenge.onRecordGranted.RemoveListener(ShowSuccess);
        }

        public DialogueLine[] GetLines(out bool notifyFinished)
        {
            notifyFinished = false;
            if (challenge == null) return GetGuideLines();

            if (!string.IsNullOrEmpty(requiredRecord) && !RecordProgress.HasRecord(requiredRecord))
                return locked;

            notifyFinished = true;
            if (RecordProgress.HasRecord(challenge.RecordId)) return replayInvitation;
            if (invited) return repeatInvitation;

            invited = true;
            return firstInvitation;
        }

        private DialogueLine[] GetGuideLines()
        {
            if (!RecordProgress.TryGetNextMissingRecord(IslandArea.Beach, out var next))
                return beachComplete;

            foreach (var hint in guideHints)
            {
                if (hint.RecordId != next.Id) continue;
                bool repeated = lastHint == next.Id;
                lastHint = next.Id;
                return repeated ? hint.Repeat : hint.First;
            }

            Debug.LogError($"Old Spin has no hint for {next.Id}.", this);
            return Array.Empty<DialogueLine>();
        }

        private void ShowFailure()
        {
            if (!RecordProgress.HasRecord(challenge.RecordId))
                dialogue.TryOpen(failure, notifyFinished: false);
        }

        private void ShowSuccess() => dialogue.TryOpen(success, notifyFinished: false);
    }
}
