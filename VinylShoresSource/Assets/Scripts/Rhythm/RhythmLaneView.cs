using System;
using System.Collections.Generic;
using UnityEngine;

namespace VinylShores.Rhythm
{
    /// <summary>Positions reusable UI notes from absolute song time, without hit judgement.</summary>
    public sealed class RhythmLaneView : MonoBehaviour
    {
        [SerializeField] private RectTransform noteTemplate;
        [SerializeField] private RectTransform noteContainer;
        [SerializeField] private RectTransform hitTarget;
        [SerializeField] private int laneIndex;
        [SerializeField] private float spawnX = 500f;
        [SerializeField] private float exitX = -500f;

        private readonly List<RectTransform> notes = new List<RectTransform>();
        private UnityEngine.UI.Image targetImage;
        private Color targetColor;
        private double flashEndDsp;
        public int VisibleNoteCount { get; private set; }
        public int LaneIndex => laneIndex;
        public double HitX => noteContainer.InverseTransformPoint(hitTarget.position).x;

        public bool IsConfigured => noteTemplate != null && noteContainer != null && hitTarget != null;

        public void Initialize()
        {
            if (!IsConfigured) throw new InvalidOperationException("Rhythm lane references are missing.");
            targetImage = hitTarget.GetComponent<UnityEngine.UI.Image>();
            if (targetImage != null) targetColor = targetImage.color;
            noteTemplate.gameObject.SetActive(false);
            if (notes.Count > 0) return;
            for (int i = 0; i < 4; i++)
            {
                var note = Instantiate(noteTemplate, noteContainer);
                note.name = "PlaybackNote_" + i;
                note.gameObject.SetActive(false);
                notes.Add(note);
            }
        }

        public double GetNoteX(double noteTime, double songTime, double leadSeconds)
        {
            return HitX + (spawnX - HitX) * (noteTime - songTime) / leadSeconds;
        }

        public void Render(double songTime, double secondsPerBeat, int totalNotes, double leadSeconds,
            RhythmScoreSession session = null)
        {
            if (targetImage != null && AudioSettings.dspTime >= flashEndDsp) targetImage.color = targetColor;
            int visible = 0;
            for (int beat = laneIndex; beat < totalNotes; beat += 2)
            {
                if (session != null && session.IsResolved(beat)) continue;
                double remaining = beat * secondsPerBeat - songTime;
                if (remaining > leadSeconds) continue;
                double x = GetNoteX(beat * secondsPerBeat, songTime, leadSeconds);
                if (x < exitX) continue;
                if (visible == notes.Count)
                {
                    var extra = Instantiate(noteTemplate, noteContainer);
                    extra.name = "PlaybackNote_" + notes.Count;
                    notes.Add(extra);
                }
                var note = notes[visible++];
                note.anchoredPosition = new Vector2((float)x, 0);
                note.gameObject.SetActive(true);
            }
            for (int i = visible; i < notes.Count; i++) notes[i].gameObject.SetActive(false);
            VisibleNoteCount = visible;
        }

        public void Clear()
        {
            foreach (var note in notes) if (note != null) note.gameObject.SetActive(false);
            VisibleNoteCount = 0;
            if (targetImage != null) targetImage.color = targetColor;
            flashEndDsp = 0;
        }

        public void Flash(Color color)
        {
            if (targetImage != null) targetImage.color = color;
            flashEndDsp = AudioSettings.dspTime + 0.12;
        }
    }
}
