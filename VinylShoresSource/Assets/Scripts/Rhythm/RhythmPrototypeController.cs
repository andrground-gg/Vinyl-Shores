using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace VinylShores.Rhythm
{
    public enum RhythmPlaybackState { Ready, Loading, CountIn, Playing, Finished, Error }

    /// <summary>Standalone two-lane rhythm prototype with DSP timing. Does not award records.</summary>
    public sealed class RhythmPrototypeController : MonoBehaviour
    {
        [Header("Track")]
        [SerializeField] private RhythmSong song;
        [SerializeField] private RhythmSong beachSong;
        [SerializeField] private AudioSource countInSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Min(0.1f)] private double leadSeconds = 2.4;

        [Header("Local display calibration")]
        [Tooltip("Positive values make notes and count-in digits appear later. Does not change audio timing.")]
        [SerializeField, Range(-500, 500)] private float visualDelayMilliseconds;

        [Header("Hit windows (milliseconds)")]
        [SerializeField, Min(1)] private double perfectWindowMilliseconds = 50;
        [SerializeField, Min(1)] private double goodWindowMilliseconds = 100;
        [SerializeField, Min(1)] private double hitWindowMilliseconds = 150;

        [Header("Scene references")]
        [SerializeField] private RhythmLaneView[] lanes;
        [SerializeField] private GameObject readyPanel;
        [SerializeField] private TMP_Text readyTitle;
        [SerializeField] private TMP_Text trackTitle;
        [SerializeField] private TMP_Text readyMessage;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private TMP_Text startButtonText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text elapsedText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text playbackTimeText;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private RectTransform progressTrack;
        [SerializeField] private RhythmStageView stageView;
        [SerializeField] private RhythmComboView comboView;
        [SerializeField] private RhythmResultView resultView;

        private double countStartDsp;
        private double musicStartDsp;
        private double loadStartedDsp;
        private bool configured;
        private bool previousListenerPause;
        private const int CountInBeats = 4;
        private const double PreparationSeconds = 0.2;
        private const string CalibrationPreference = "VinylShores.RhythmPrototype.VisualDelayMs";
        private const string BeachSongCalibrationPreference = "VinylShores.BeachSong1.VisualDelayMs.v2";
        private const float BeachSongDefaultDelayMilliseconds = 110f;
        private float defaultVisualDelayMilliseconds;
        private string calibrationPreferenceKey;
        private InputAction leftAction;
        private InputAction rightAction;
        private RhythmChart chart;
        private AudioClip countInSample;
        private AudioClip countInClick;
        private AudioSource[] countSources;
        private double firstBeatDsp;
        public RhythmScoreSession Scoring { get; private set; }
        public RhythmChart Chart => chart;

        public RhythmPlaybackState State { get; private set; } = RhythmPlaybackState.Ready;
        public bool IsPaused { get; private set; }
        private bool IsPlayingAttempt => State == RhythmPlaybackState.CountIn || State == RhythmPlaybackState.Playing;
        public double SecondsPerBeat => chart != null ? chart.SecondsPerBeat : 0;
        public int TotalNotes => chart != null ? chart.NoteCount : 0;
        public double SongTime => State == RhythmPlaybackState.Finished ? DurationSeconds :
            State == RhythmPlaybackState.CountIn || State == RhythmPlaybackState.Playing ?
            AudioSettings.dspTime - musicStartDsp : 0;
        public double MusicStartDsp => musicStartDsp;
        public double CountStartDsp => countStartDsp;
        public double FirstBeatDsp => firstBeatDsp;
        public double DurationSeconds => chart != null ? chart.DurationSeconds : 0;
        public float VisualDelayMilliseconds => visualDelayMilliseconds;
        public double VisualSongTime => State == RhythmPlaybackState.CountIn || State == RhythmPlaybackState.Playing ?
            SongTime - visualDelayMilliseconds / 1000.0 : SongTime;

        private void Awake()
        {
            configured = countInSource != null && countInSource.clip != null && musicSource != null && countInSource != musicSource &&
                leadSeconds > 0 && lanes != null && lanes.Length == 2 &&
                readyPanel != null && readyTitle != null && readyMessage != null && startButton != null &&
                startButtonText != null && countText != null && elapsedText != null && durationText != null &&
                progressFill != null && progressTrack != null && stageView != null && stageView.IsConfigured &&
                comboView != null && comboView.IsConfigured && resultView != null && resultView.IsConfigured &&
                perfectWindowMilliseconds > 0 && goodWindowMilliseconds >= perfectWindowMilliseconds &&
                hitWindowMilliseconds >= goodWindowMilliseconds;
            if (configured)
            {
                for (int i = 0; i < lanes.Length; i++)
                    configured &= lanes[i] != null && lanes[i].IsConfigured && lanes[i].LaneIndex == i;
            }
            if (!configured)
            {
                Debug.LogError("Rhythm prototype has missing or invalid Inspector references.", this);
                enabled = false;
                return;
            }
            countInSample = countInSource.clip;
            ConfigureSource(musicSource);
            countSources = new AudioSource[CountInBeats];
            countSources[0] = countInSource;
            for (int i = 1; i < CountInBeats; i++)
            {
                var beatObject = new GameObject("CountInBeat" + (i + 1));
                beatObject.transform.SetParent(transform, false);
                countSources[i] = beatObject.AddComponent<AudioSource>();
                countSources[i].outputAudioMixerGroup = countInSource.outputAudioMixerGroup;
                countSources[i].volume = countInSource.volume;
            }
            foreach (var source in countSources) ConfigureSource(source);
            foreach (var lane in lanes) lane.Initialize();
            bool beachChallenge = BeachRhythmSession.IsActive;
            calibrationPreferenceKey = beachChallenge ? BeachSongCalibrationPreference : CalibrationPreference;
            defaultVisualDelayMilliseconds = beachChallenge ? BeachSongDefaultDelayMilliseconds : visualDelayMilliseconds;
            SetVisualDelay(PlayerPrefs.GetFloat(calibrationPreferenceKey, defaultVisualDelayMilliseconds));
            leftAction = new InputAction("RhythmLeft", InputActionType.Button, "<Keyboard>/leftArrow");
            rightAction = new InputAction("RhythmRight", InputActionType.Button, "<Keyboard>/rightArrow");
            leftAction.performed += OnLeftPressed;
            rightAction.performed += OnRightPressed;
            startButton.onClick.AddListener(BeginPlayback);
            countText.text = "";
            UpdateProgress(0);
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.pitch = 1;
            source.Stop();
        }

        public void BeginPlayback()
        {
            if (!configured || !isActiveAndEnabled || State == RhythmPlaybackState.Loading ||
                (IsPlayingAttempt && !IsPaused)) return;
            // Cancel the old scheduled audio before releasing the DSP clock on restart.
            StopPlayback();
            ResumePlayback();
            if (!PrepareSong()) return;
            State = RhythmPlaybackState.Loading;
            readyPanel.SetActive(false);
            startButton.interactable = false;
            countText.text = "Loading audio...";
            UpdateProgress(0);
            loadStartedDsp = AudioSettings.dspTime;
            if (!RequestAudioData(chart.Track) || !RequestAudioData(countInSample)) FailLoading();
            else if (AudioLoaded()) SchedulePlayback();
        }

        private bool PrepareSong()
        {
            RhythmSong selectedSong = BeachRhythmSession.IsActive ? beachSong : song;
            if (selectedSong == null)
            {
                FailLoading("Assign a Rhythm Song asset on RhythmPlayback.");
                return false;
            }
            if (!selectedSong.TryCreateChart(out var preparedChart, out string error))
            {
                FailLoading(error);
                return false;
            }
            chart = preparedChart;
            musicSource.clip = chart.Track;
            stageView.ApplyChart(chart);
            if (trackTitle != null) trackTitle.text = chart.TrackTitle;
            ResetScoring();
            return true;
        }

        private static bool RequestAudioData(AudioClip clip)
        {
            return clip.loadState == AudioDataLoadState.Loaded || clip.loadState == AudioDataLoadState.Loading || clip.LoadAudioData();
        }

        private bool AudioLoaded() => chart.Track.loadState == AudioDataLoadState.Loaded &&
            countInSample.loadState == AudioDataLoadState.Loaded;

        private bool PrepareCountInClick()
        {
            if (countInClick != null) return true;
            // Reuse only the first metronome click; the original OGG is never edited.
            int frames = Math.Min(countInSample.samples, Math.Max(1, (int)(countInSample.frequency * 0.12)));
            var samples = new float[frames * countInSample.channels];
            if (!countInSample.GetData(samples, 0)) return false;
            countInClick = AudioClip.Create("Rhythm count-in click", frames, countInSample.channels, countInSample.frequency, false);
            countInClick.SetData(samples, 0);
            return true;
        }

        private void SchedulePlayback()
        {
            if (!PrepareCountInClick())
            {
                FailLoading("Could not read the count-in metronome. Its audio data must be decompressed on load.");
                return;
            }
            double countDuration = CountInBeats * SecondsPerBeat;
            double firstNoteAfterBeatZero = chart.GetNote(0).TimeSeconds - chart.FirstBeatOffsetSeconds;
            double preRoll = Math.Max(Math.Max(countDuration, chart.FirstBeatOffsetSeconds), leadSeconds - firstNoteAfterBeatZero);
            firstBeatDsp = AudioSettings.dspTime + PreparationSeconds + preRoll + Math.Max(0, -visualDelayMilliseconds / 1000.0);
            countStartDsp = firstBeatDsp - countDuration;
            musicStartDsp = firstBeatDsp - chart.FirstBeatOffsetSeconds;
            for (int i = 0; i < CountInBeats; i++)
            {
                double start = countStartDsp + i * SecondsPerBeat;
                countSources[i].clip = countInClick;
                countSources[i].PlayScheduled(start);
                countSources[i].SetScheduledEndTime(start + Math.Min((double)countInClick.samples / countInClick.frequency, SecondsPerBeat));
            }
            musicSource.PlayScheduled(musicStartDsp);
            musicSource.SetScheduledEndTime(musicStartDsp + DurationSeconds);
            State = RhythmPlaybackState.CountIn;
            stageView.Begin(musicStartDsp, chart, visualDelayMilliseconds);
            countText.text = "Get ready";
        }

        private void Update()
        {
            if (!configured) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && IsPlayingAttempt)
            {
                if (IsPaused) ResumePlayback();
                else PausePlayback();
                return;
            }
            if (IsPaused) return;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame) BeginPlayback();
                if (keyboard.minusKey.wasPressedThisFrame) SaveVisualDelay(visualDelayMilliseconds - 10);
                if (keyboard.equalsKey.wasPressedThisFrame) SaveVisualDelay(visualDelayMilliseconds + 10);
                if (keyboard.backspaceKey.wasPressedThisFrame) SaveVisualDelay(defaultVisualDelayMilliseconds);
            }
            if (State == RhythmPlaybackState.Loading)
            {
                if (AudioLoaded()) SchedulePlayback();
                else if (chart.Track.loadState == AudioDataLoadState.Failed || countInSample.loadState == AudioDataLoadState.Failed ||
                    AudioSettings.dspTime - loadStartedDsp > 10) FailLoading();
                return;
            }
            if (State != RhythmPlaybackState.CountIn && State != RhythmPlaybackState.Playing) return;
            double now = AudioSettings.dspTime;
            double audioTime = now - musicStartDsp;
            double time = audioTime - visualDelayMilliseconds / 1000.0;
            Scoring.Advance(time);
            // Let both the scheduled audio and the delayed display finish before resetting.
            if (audioTime >= DurationSeconds && time >= Math.Max(DurationSeconds, Scoring.LastHitWindowEnd + 0.000001))
            {
                StopPlayback();
                State = RhythmPlaybackState.Finished;
                UpdateProgress(DurationSeconds);
                BeachRhythmSession.ReportResult(Scoring.ScorePercentage);
                ShowReady(RhythmResultView.StarsForPercentage(Scoring.ScorePercentage) > 0 ? "Challenge passed" : "Try that beat again",
                    $"Perfect: {Scoring.Perfects}   Good: {Scoring.Goods}   OK: {Scoring.Oks}\n" +
                    $"Miss: {Scoring.Misses}   Extra presses: {Scoring.ExtraPresses}\n" +
                    $"Best combo: {Scoring.BestCombo}", "Restart");
                resultView.Show(Scoring);
                return;
            }
            State = now < firstBeatDsp ? RhythmPlaybackState.CountIn : RhythmPlaybackState.Playing;
            double timeFromFirstBeat = time - chart.FirstBeatOffsetSeconds;
            double countElapsed = timeFromFirstBeat + CountInBeats * SecondsPerBeat;
            countText.text = countElapsed < 0 ? "Get ready" : timeFromFirstBeat < 0 ?
                (Math.Min(CountInBeats - 1, (int)(countElapsed / SecondsPerBeat)) + 1).ToString() : "";
            foreach (var lane in lanes) lane.Render(time, chart, leadSeconds, Scoring);
            UpdateProgress(Math.Max(0, time));
        }

        private void ResetScoring()
        {
            if (Scoring != null) Scoring.Judged -= OnJudged;
            Scoring = new RhythmScoreSession(chart, perfectWindowMilliseconds / 1000,
                goodWindowMilliseconds / 1000, hitWindowMilliseconds / 1000);
            Scoring.Judged += OnJudged;
            comboView.ResetView();
            resultView.ResetView();
        }

        private void OnLeftPressed(InputAction.CallbackContext context) => JudgeInput(0, context.time);
        private void OnRightPressed(InputAction.CallbackContext context) => JudgeInput(1, context.time);

        private void JudgeInput(int lane, double inputEventTime)
        {
            if (!configured || IsPaused || !IsPlayingAttempt) return;
            lanes[lane].Press();
            // Input events carry timestamps; subtract their age instead of judging at this frame's arrival time.
            double eventDspTime = AudioSettings.dspTime - (InputState.currentTime - inputEventTime);
            double time = eventDspTime - musicStartDsp - visualDelayMilliseconds / 1000.0;
            Scoring.Press(lane, time);
        }

        private void OnJudged(RhythmHitResult result)
        {
            bool missed = result.Rating == RhythmRating.Miss || result.Rating == RhythmRating.EmptyPress;
            Color color = missed ? new Color(0.98f, 0.35f, 0.32f) : result.Rating == RhythmRating.Perfect ?
                new Color(0.96f, 0.78f, 0.49f) : new Color(0.65f, 0.9f, 0.8f);
            lanes[result.Lane].ShowJudgement(result, color);
            comboView.SetCombo(Scoring.Combo);
            stageView.Judge(result, Scoring.Combo);
        }

        private void PausePlayback()
        {
            previousListenerPause = AudioListener.pause;
            // The listener also freezes DSP time, keeping scheduled counts, notes and music in sync.
            AudioListener.pause = true;
            IsPaused = true;
            resultView.ShowPause(ResumePlayback, BeachRhythmSession.IsActive ? ExitPausedChallenge : null);
            readyPanel.SetActive(true);
            resultView.SelectResumeButton();
        }

        private void ResumePlayback()
        {
            if (!IsPaused) return;
            readyPanel.SetActive(false);
            resultView.ResetView();
            IsPaused = false;
            AudioListener.pause = previousListenerPause;
        }

        private void ExitPausedChallenge()
        {
            if (!IsPaused || !BeachRhythmSession.IsActive) return;
            // No result is submitted for the interrupted attempt. Completed retries still count.
            enabled = false;
            BeachRhythmSession.ReturnToBeach();
        }

        public void SetVisualDelay(float milliseconds)
        {
            if (float.IsNaN(milliseconds) || float.IsInfinity(milliseconds))
                throw new ArgumentOutOfRangeException(nameof(milliseconds));
            visualDelayMilliseconds = Mathf.Clamp(milliseconds, -500, 500);
            if (stageView != null) stageView.SetCalibration(visualDelayMilliseconds);
        }

        private void SaveVisualDelay(float milliseconds)
        {
            SetVisualDelay(milliseconds);
            PlayerPrefs.SetFloat(calibrationPreferenceKey, visualDelayMilliseconds);
            PlayerPrefs.Save();
        }

        private void UpdateProgress(double time)
        {
            if (playbackTimeText != null)
                playbackTimeText.text = FormatTime(time) + " / " + FormatTime(DurationSeconds);
            elapsedText.text = FormatTime(time);
            durationText.text = FormatTime(DurationSeconds);
            progressFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                progressTrack.rect.width * (float)Math.Clamp(time / Math.Max(DurationSeconds, 0.001), 0, 1));
        }

        private static string FormatTime(double seconds)
        {
            int whole = (int)Math.Max(0, seconds);
            return (whole / 60) + ":" + (whole % 60).ToString("00");
        }

        private void ShowReady(string title, string message, string buttonLabel)
        {
            readyTitle.text = title;
            readyMessage.text = message;
            startButtonText.text = buttonLabel;
            startButton.interactable = true;
            readyPanel.SetActive(true);
        }

        private void FailLoading(string message = "Could not load the audio. Please try again.")
        {
            StopPlayback();
            if (resultView != null) resultView.ResetView();
            if (comboView != null) comboView.ResetView();
            State = RhythmPlaybackState.Error;
            ShowReady("Song unavailable", message, "Try again");
            resultView.ShowReturnButton(false);
        }

        private void StopPlayback()
        {
            if (countSources != null) foreach (var source in countSources) if (source != null) source.Stop();
            if (musicSource != null) musicSource.Stop();
            if (lanes != null) foreach (var lane in lanes) if (lane != null) lane.Clear();
            if (countText != null) countText.text = "";
            if (stageView != null) stageView.Finish();
        }

        private void OnDisable()
        {
            leftAction?.Disable();
            rightAction?.Disable();
            StopPlayback();
            ResumePlayback();
            if (stageView != null) stageView.ResetView();
            if (comboView != null) comboView.ResetView();
            if (resultView != null) resultView.ResetView();
            State = RhythmPlaybackState.Ready;
        }

        private void OnEnable()
        {
            if (!configured) return;
            leftAction.Enable();
            rightAction.Enable();
            if (BeachRhythmSession.IsActive)
                stageView.SetOpponentSprite(BeachRhythmSession.OpponentSprite);
            State = RhythmPlaybackState.Ready;
            readyPanel.SetActive(false);
            BeginPlayback();
        }

        private void OnDestroy()
        {
            leftAction?.Dispose();
            rightAction?.Dispose();
            if (countInClick != null) Destroy(countInClick);
            if (Scoring != null) Scoring.Judged -= OnJudged;
            if (startButton != null) startButton.onClick.RemoveListener(BeginPlayback);
        }
    }
}
