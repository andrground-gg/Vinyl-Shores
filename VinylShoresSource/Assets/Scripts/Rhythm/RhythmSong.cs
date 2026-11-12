using System;
using System.Collections.Generic;
using UnityEngine;

namespace VinylShores.Rhythm
{
    public enum RhythmLane { Left, Right }
    public enum RhythmLocation { Beach, Forest, Mountains }

    [Serializable]
    public struct RhythmNote
    {
        [Tooltip("Zero-based beat position. Fractions are supported: 2.5 is halfway between beats 2 and 3.")]
        [SerializeField, Min(0)] private double beat;
        [SerializeField] private RhythmLane lane;

        public double Beat => beat;
        public RhythmLane Lane => lane;

        public RhythmNote(double beat, RhythmLane lane)
        {
            this.beat = beat;
            this.lane = lane;
        }
    }

    public readonly struct RhythmChartNote
    {
        public double TimeSeconds { get; }
        public int Lane { get; }

        public RhythmChartNote(double timeSeconds, int lane)
        {
            TimeSeconds = timeSeconds;
            Lane = lane;
        }
    }

    /// <summary>Immutable timing snapshot shared by scoring and presentation during one attempt.</summary>
    public sealed class RhythmChart
    {
        private readonly RhythmChartNote[] notes;
        public AudioClip Track { get; }
        public string TrackTitle { get; }
        public double Bpm { get; }
        public double FirstBeatOffsetSeconds { get; }
        public double SecondsPerBeat => 60.0 / Bpm;
        public double DurationSeconds { get; }
        public RhythmLocation Location { get; }
        public int NoteCount => notes.Length;
        public RhythmChartNote GetNote(int index) => notes[index];

        internal RhythmChart(AudioClip track, double bpm, double firstBeatOffsetSeconds, RhythmChartNote[] notes, RhythmLocation location, string trackTitle)
        {
            Track = track;
            TrackTitle = trackTitle;
            Bpm = bpm;
            FirstBeatOffsetSeconds = firstBeatOffsetSeconds;
            DurationSeconds = (double)track.samples / track.frequency;
            this.notes = notes;
            Location = location;
        }
    }

    [CreateAssetMenu(fileName = "RhythmSong", menuName = "Vinyl Shores/Rhythm Song")]
    public sealed class RhythmSong : ScriptableObject
    {
        [SerializeField] private AudioClip track;
        [SerializeField] private string trackTitle;
        [SerializeField] private RhythmLocation location;
        [SerializeField, Min(1)] private double bpm = 100;
        [Tooltip("Time of beat zero from the start of the audio file, in seconds. Not device calibration.")]
        [SerializeField, Min(0)] private double firstBeatOffsetSeconds;
        [Tooltip("Extra individual notes, combined with pattern placements.")]
        [SerializeField] private List<RhythmNote> notes = new List<RhythmNote>();
        [SerializeField] private List<RhythmPatternPlacement> patterns = new List<RhythmPatternPlacement>();

        public bool TryCreateChart(out RhythmChart chart, out string error)
        {
            chart = null;
            error = null;
            if (track == null || track.frequency <= 0 || track.samples <= 0)
                error = "Assign an audio track to the song asset.";
            else if (!IsFinite(bpm) || bpm <= 0)
                error = "Song BPM must be a positive, finite number.";
            else if (!IsFinite(firstBeatOffsetSeconds) || firstBeatOffsetSeconds < 0)
                error = "First beat offset must be a finite number of seconds, at least zero.";
            else if ((int)location < 0 || (int)location > 2)
                error = "Choose Beach, Forest, or Mountains for the song location.";

            if (error != null) return false;

            var expanded = notes != null ? new List<RhythmNote>(notes) : new List<RhythmNote>();
            if (patterns != null)
            {
                foreach (var placement in patterns)
                    if (!placement.TryAppend(expanded, out error)) return false;
            }
            if (expanded.Count == 0 || expanded.Count > int.MaxValue / 100)
            {
                error = "The song needs notes or pattern placements.";
                return false;
            }

            double duration = (double)track.samples / track.frequency;
            var timedNotes = new RhythmChartNote[expanded.Count];
            for (int i = 0; i < expanded.Count; i++)
            {
                var note = expanded[i];
                double time = firstBeatOffsetSeconds + note.Beat * (60.0 / bpm);
                if (!IsFinite(note.Beat) || note.Beat < 0 || (int)note.Lane < 0 || (int)note.Lane > 1 ||
                    !IsFinite(time) || time < 0 || time >= duration)
                {
                    error = $"Note {i}: use a non-negative beat, Left/Right lane, and a hit time before the track ends.";
                    return false;
                }
                timedNotes[i] = new RhythmChartNote(time, (int)note.Lane);
            }
            Array.Sort(timedNotes, (a, b) =>
            {
                int order = a.TimeSeconds.CompareTo(b.TimeSeconds);
                return order != 0 ? order : a.Lane.CompareTo(b.Lane);
            });
            for (int i = 1; i < timedNotes.Length; i++)
            {
                if (timedNotes[i].Lane == timedNotes[i - 1].Lane &&
                    Math.Abs(timedNotes[i].TimeSeconds - timedNotes[i - 1].TimeSeconds) < 0.000000001)
                {
                    error = $"Duplicate note on {(RhythmLane)timedNotes[i].Lane} at {timedNotes[i].TimeSeconds:F3} seconds.";
                    return false;
                }
            }
            chart = new RhythmChart(track, bpm, firstBeatOffsetSeconds, timedNotes, location, string.IsNullOrWhiteSpace(trackTitle) ? track.name : trackTitle.Trim());
            return true;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
