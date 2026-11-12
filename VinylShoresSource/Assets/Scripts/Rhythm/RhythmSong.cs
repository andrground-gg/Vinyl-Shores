using System;
using System.Collections.Generic;
using UnityEngine;

namespace VinylShores.Rhythm
{
    public enum RhythmLane { Left, Right }

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
        public double Bpm { get; }
        public double FirstBeatOffsetSeconds { get; }
        public double SecondsPerBeat => 60.0 / Bpm;
        public double DurationSeconds { get; }
        public int NoteCount => notes.Length;
        public RhythmChartNote GetNote(int index) => notes[index];

        internal RhythmChart(AudioClip track, double bpm, double firstBeatOffsetSeconds, RhythmChartNote[] notes)
        {
            Track = track;
            Bpm = bpm;
            FirstBeatOffsetSeconds = firstBeatOffsetSeconds;
            DurationSeconds = (double)track.samples / track.frequency;
            this.notes = notes;
        }
    }

    [CreateAssetMenu(fileName = "RhythmSong", menuName = "Vinyl Shores/Rhythm Song")]
    public sealed class RhythmSong : ScriptableObject
    {
        [SerializeField] private AudioClip track;
        [SerializeField, Min(1)] private double bpm = 100;
        [Tooltip("Time of beat zero from the start of the audio file, in seconds. Not device calibration.")]
        [SerializeField, Min(0)] private double firstBeatOffsetSeconds;
        [SerializeField] private List<RhythmNote> notes = new List<RhythmNote>();

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
            else if (notes == null || notes.Count == 0 || notes.Count > int.MaxValue / 100)
                error = "The song needs a non-empty list of notes.";
            if (error != null) return false;

            double duration = (double)track.samples / track.frequency;
            var timedNotes = new RhythmChartNote[notes.Count];
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
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
            chart = new RhythmChart(track, bpm, firstBeatOffsetSeconds, timedNotes);
            return true;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
