using System;
using System.Collections.Generic;
using UnityEngine;

namespace VinylShores.Rhythm
{
    [CreateAssetMenu(fileName = "RhythmPattern", menuName = "Vinyl Shores/Rhythm Pattern")]
    public sealed class RhythmPattern : ScriptableObject
    {
        [SerializeField, Min(0.25f)] private double lengthBeats = 4;
        [SerializeField] private List<RhythmNote> notes = new List<RhythmNote>();

        public bool TryAppend(List<RhythmNote> destination, double startBeat, bool mirror, out string error)
        {
            error = null;
            if (double.IsNaN(lengthBeats) || double.IsInfinity(lengthBeats) || lengthBeats <= 0 ||
                notes == null || notes.Count == 0)
            {
                error = $"Pattern {name} needs a positive length and notes.";
                return false;
            }
            foreach (var note in notes)
            {
                if (double.IsNaN(note.Beat) || double.IsInfinity(note.Beat) || note.Beat < 0 ||
                    note.Beat >= lengthBeats || (int)note.Lane < 0 || (int)note.Lane > 1)
                {
                    error = $"Pattern {name}: notes must fit inside Length Beats and use Left/Right.";
                    return false;
                }
            }
            foreach (var note in notes)
                destination.Add(new RhythmNote(startBeat + note.Beat,
                    mirror ? (RhythmLane)(1 - (int)note.Lane) : note.Lane));
            return true;
        }
    }

    [Serializable]
    public struct RhythmPatternPlacement
    {
        [SerializeField] private RhythmPattern pattern;
        [SerializeField, Min(0)] private double startBeat;
        [SerializeField] private bool mirror;

        public bool TryAppend(List<RhythmNote> destination, out string error)
        {
            error = null;
            if (pattern == null || double.IsNaN(startBeat) || double.IsInfinity(startBeat) || startBeat < 0)
            {
                error = "Each pattern placement needs a pattern and a non-negative finite Start Beat.";
                return false;
            }
            return pattern.TryAppend(destination, startBeat, mirror, out error);
        }
    }
}
