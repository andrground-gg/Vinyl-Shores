using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VinylShores.Rhythm
{
    /// <summary>Positions reusable UI notes from absolute song time, without hit judgement.</summary>
    public sealed class RhythmLaneView : MonoBehaviour
    {
        [SerializeField] private RectTransform noteTemplate;
        [SerializeField] private RectTransform noteContainer;
        [SerializeField] private RectTransform hitTarget;
        [SerializeField] private UnityEngine.UI.Image hitPulse;
        [SerializeField] private TMP_Text judgementText;
        [SerializeField] private CanvasGroup judgementBurst;
        [SerializeField] private int laneIndex;
        [SerializeField] private float spawnX = 500f;
        [SerializeField] private float exitX = -500f;

        private readonly List<RectTransform> notes = new List<RectTransform>();
        private UnityEngine.UI.Image targetImage;
        private Color targetColor;
        private Vector3 targetScale;
        private Vector2 judgementPosition;
        private double pressStartDsp = double.NegativeInfinity;
        private double pulseStartDsp = double.NegativeInfinity;
        private double judgementStartDsp = double.NegativeInfinity;
        private Color reactionColor;
        private bool failedReaction;
        private bool failedJudgement;
        private const double PressDuration = 0.2;
        private const double PulseDuration = 0.24;
        private const double JudgementDuration = 0.7;
        public int VisibleNoteCount { get; private set; }
        public int LaneIndex => laneIndex;
        public double HitX => noteContainer.InverseTransformPoint(hitTarget.position).x;

        public bool IsConfigured => noteTemplate != null && noteContainer != null && hitTarget != null &&
            hitPulse != null && judgementText != null;

        public void Initialize()
        {
            if (!IsConfigured) throw new InvalidOperationException("Rhythm lane references are missing.");
            targetImage = hitTarget.GetComponent<UnityEngine.UI.Image>();
            if (targetImage != null) targetColor = targetImage.color;
            targetScale = hitTarget.localScale;
            judgementPosition = judgementText.rectTransform.anchoredPosition;
            ResetFeedback();
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

        public void Render(double songTime, RhythmChart chart, double leadSeconds,
            RhythmScoreSession session = null)
        {
            int visible = 0;
            for (int i = 0; i < chart.NoteCount; i++)
            {
                var chartNote = chart.GetNote(i);
                if (chartNote.Lane != laneIndex || (session != null && session.IsResolved(i))) continue;
                double remaining = chartNote.TimeSeconds - songTime;
                if (remaining > leadSeconds) continue;
                double x = GetNoteX(chartNote.TimeSeconds, songTime, leadSeconds);
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
            ResetFeedback();
        }

        public void Press()
        {
            pressStartDsp = AudioSettings.dspTime;
            BeginPulse(new Color(1, 0.945f, 0.835f), false);
        }

        public void ShowJudgement(RhythmHitResult result, Color color)
        {
            bool failed = result.Rating == RhythmRating.Miss || result.Rating == RhythmRating.EmptyPress;
            string label = result.Rating == RhythmRating.EmptyPress ? "EXTRA" : result.Rating.ToString().ToUpperInvariant();
            failedJudgement = failed;
            judgementText.text = label;
            judgementText.color = color;
            judgementText.alpha = 1;
            judgementStartDsp = AudioSettings.dspTime;
            BeginPulse(color, failed);
        }

        private void BeginPulse(Color color, bool failed)
        {
            reactionColor = color;
            failedReaction = failed;
            pulseStartDsp = AudioSettings.dspTime;
        }

        private void LateUpdate()
        {
            if (targetImage == null || hitPulse == null || judgementText == null) return;
            double now = AudioSettings.dspTime;
            float press = (float)Math.Clamp((now - pressStartDsp) / PressDuration, 0, 1);
            float scale = press < 0.25f ? Mathf.Lerp(0.86f, 1.14f, press / 0.25f) :
                Mathf.Lerp(1.14f, 1, (press - 0.25f) / 0.75f);
            hitTarget.localScale = targetScale * scale;

            float pulse = (float)Math.Clamp((now - pulseStartDsp) / PulseDuration, 0, 1);
            float intensity = (1 - pulse) * (1 - pulse);
            Color pulseColor = reactionColor;
            pulseColor.a = intensity * (failedReaction ? 0.5f : 0.75f);
            hitPulse.color = pulseColor;
            hitPulse.rectTransform.localScale = pulse < 1 ? Vector3.one * Mathf.Lerp(0.8f, 1.28f, pulse) : Vector3.one;
            targetImage.color = Color.Lerp(targetColor, failedReaction ? reactionColor : Color.white, intensity);

            float age = (float)(now - judgementStartDsp);
            float fade = 1 - Mathf.Clamp01((age - 0.38f) / 0.24f);
            judgementText.alpha = fade;
            var textRect = judgementText.rectTransform;
            if (age < JudgementDuration)
            {
                float p = Mathf.Clamp01(age / 0.28f);
                if (failedJudgement)
                {
                    float drop = 1 - Mathf.Pow(1 - p, 3);
                    textRect.localScale = Vector3.one * Mathf.Lerp(1.08f, 0.92f, drop);
                    textRect.anchoredPosition = judgementPosition + new Vector2(
                        Mathf.Sin(age * 35) * 2 * (1 - p), -9 * drop);
                    textRect.localRotation = Quaternion.Euler(0, 0, -2 * drop);
                }
                else
                {
                    float pop = Mathf.Sin(p * Mathf.PI);
                    float enter = Mathf.Lerp(0.88f, 1, Mathf.Clamp01(age / 0.045f));
                    textRect.localScale = Vector3.one * (enter + 0.16f * pop);
                    textRect.anchoredPosition = judgementPosition + Vector2.up * (7 * Mathf.Sin(p * Mathf.PI));
                    textRect.localRotation = Quaternion.Euler(0, 0, -1.5f * Mathf.Sin(p * Mathf.PI));
                }
            }
            else
            {
                judgementText.text = "";
                textRect.anchoredPosition = judgementPosition;
                textRect.localScale = Vector3.one;
                textRect.localRotation = Quaternion.identity;
            }
            if (judgementBurst != null)
            {
                float p = Mathf.Clamp01(age / 0.36f);
                judgementBurst.gameObject.SetActive(!failedJudgement && p < 1);
                judgementBurst.alpha = 1 - p;
                judgementBurst.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, p);
            }
        }

        private void ResetFeedback()
        {
            pressStartDsp = pulseStartDsp = judgementStartDsp = double.NegativeInfinity;
            if (targetImage != null) targetImage.color = targetColor;
            if (targetImage != null) hitTarget.localScale = targetScale;
            if (hitPulse != null)
            {
                var color = hitPulse.color;
                color.a = 0;
                hitPulse.color = color;
                hitPulse.rectTransform.localScale = Vector3.one;
            }
            if (judgementBurst != null) judgementBurst.gameObject.SetActive(false);
            if (judgementText != null)
            {
                judgementText.text = "";
                judgementText.alpha = 0;
                judgementText.rectTransform.localScale = Vector3.one;
                judgementText.rectTransform.localRotation = Quaternion.identity;
                judgementText.rectTransform.anchoredPosition = judgementPosition;
            }
        }
    }
}
