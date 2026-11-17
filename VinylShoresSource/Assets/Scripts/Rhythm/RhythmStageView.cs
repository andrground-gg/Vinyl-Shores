using System;
using System.Collections.Generic;
using UnityEngine;

namespace VinylShores.Rhythm
{
    [Serializable]
    public struct RhythmLocationTheme
    {
        public RhythmLocation Location;
        public Texture2D Backdrop;
        public Sprite Ground;
        public Color Accent;
        public Color Title;
    }

    public sealed class RhythmStageView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.RawImage backdrop;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private UnityEngine.UI.Image ground;
        [SerializeField] private RhythmLocationTheme[] themes;
        [SerializeField] private TMPro.TMP_Text trackTitle;
        [SerializeField] private TMPro.TMP_Text comboLabel;
        [SerializeField] private RhythmComboView combo;
        [SerializeField] private UnityEngine.UI.Image progressFill;
        [SerializeField] private UnityEngine.UI.Image progressTrack;
        [SerializeField] private RhythmActorView player;
        [SerializeField] private RhythmActorView opponent;
        private readonly Queue<float> recent = new Queue<float>();
        private float sum;
        private float baseEnergy = 0.5f;
        private double lowUntilBeat = double.NegativeInfinity;
        private double musicStart;
        private double firstBeatOffset;
        private double secondsPerBeat = 0.6;
        private double delay;
        private bool running;
        public float PlayerEnergy { get; private set; } = 0.5f;
        public float OpponentEnergy => 1 - PlayerEnergy;
        public RhythmLocation CurrentLocation { get; private set; }
        public bool IsConfigured
        {
            get
            {
                if (backdrop == null || viewport == null || ground == null || player == null || opponent == null ||
                    !player.IsConfigured || !opponent.IsConfigured || themes == null) return false;
                for (int location = 0; location < 3; location++)
                {
                    bool found = false;
                    foreach (var theme in themes) found |= (int)theme.Location == location && theme.Backdrop != null && theme.Ground != null;
                    if (!found) return false;
                }
                return true;
            }
        }

        public void ApplyChart(RhythmChart chart)
        {
            ResetView();
            CurrentLocation = chart.Location;
            foreach (var theme in themes)
            {
                if (theme.Location != chart.Location) continue;
                backdrop.texture = theme.Backdrop;
                ground.sprite = theme.Ground;
                if (trackTitle != null) trackTitle.color = theme.Title;
                if (comboLabel != null) comboLabel.color = theme.Accent;
                if (combo != null) combo.SetTheme(theme.Accent);
                if (progressFill != null) progressFill.color = Color.Lerp(theme.Accent, Color.white, 0.55f);
                if (progressTrack != null) progressTrack.color = new Color(0.03f, 0.08f, 0.12f, 0.85f);
                break;
            }
            FitBackdrop();
        }

        public void Begin(double startDsp, RhythmChart chart, float visualDelayMilliseconds)
        {
            musicStart = startDsp;
            firstBeatOffset = chart.FirstBeatOffsetSeconds;
            secondsPerBeat = chart.SecondsPerBeat;
            SetCalibration(visualDelayMilliseconds);
            running = true;
            player.Begin(BeatNow);
            opponent.Begin(BeatNow);
        }

        public void SetCalibration(float milliseconds) => delay = milliseconds / 1000.0;
        private double BeatNow => (AudioSettings.dspTime - musicStart - delay - firstBeatOffset) / secondsPerBeat;

        public void Judge(RhythmHitResult result, int combo)
        {
            if (!running) return;
            float quality = result.Rating == RhythmRating.Perfect ? 1 : result.Rating == RhythmRating.Good ? 0.7f :
                result.Rating == RhythmRating.Ok ? 0.4f : 0;
            recent.Enqueue(quality);
            sum += quality;
            if (recent.Count > 8) sum -= recent.Dequeue();
            baseEnergy = Mathf.Clamp01(0.65f * sum / recent.Count + 0.35f * Mathf.Clamp01(combo / 20f));
            bool failed = result.Rating == RhythmRating.Miss || result.Rating == RhythmRating.EmptyPress;
            if (failed)
            {
                lowUntilBeat = BeatNow + 2;
                player.ReactSad(BeatNow);
            }
            else if (combo >= 10 && combo % 10 == 0) opponent.ReactSad(BeatNow);
        }

        public void Finish()
        {
            running = false;
            if (player != null) player.Settle();
            if (opponent != null) opponent.Settle();
        }

        public void ResetView()
        {
            running = false;
            recent.Clear();
            sum = 0;
            baseEnergy = PlayerEnergy = 0.5f;
            lowUntilBeat = double.NegativeInfinity;
            if (player != null) player.ResetPose();
            if (opponent != null) opponent.ResetPose();
        }

        private void LateUpdate()
        {
            if (!IsConfigured) return;
            FitBackdrop();
            double beat = BeatNow;
            PlayerEnergy = running && beat < lowUntilBeat ? Math.Min(baseEnergy, 0.2f) : baseEnergy;
            player.Tick(beat, PlayerEnergy);
            opponent.Tick(beat, OpponentEnergy);
        }

        private void FitBackdrop()
        {
            if (backdrop == null || backdrop.texture == null || viewport == null) return;
            float aspect = (float)backdrop.texture.width / backdrop.texture.height;
            float width = Mathf.Max(viewport.rect.width, viewport.rect.height * aspect);
            backdrop.rectTransform.sizeDelta = new Vector2(width, width / aspect);
        }
    }
}
