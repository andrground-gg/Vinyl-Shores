using System;

namespace VinylShores.Rhythm
{
    public enum RhythmRating { None, Perfect, Good, Ok, Miss, EmptyPress }

    public readonly struct RhythmHitResult
    {
        public int NoteIndex { get; }
        public int Lane { get; }
        public RhythmRating Rating { get; }
        public double ErrorSeconds { get; }

        public RhythmHitResult(int noteIndex, int lane, RhythmRating rating, double errorSeconds = 0)
        {
            NoteIndex = noteIndex;
            Lane = lane;
            Rating = rating;
            ErrorSeconds = errorSeconds;
        }
    }

    /// <summary>Judges the song's explicit note list using calibrated timestamps, independently of frames.</summary>
    public sealed class RhythmScoreSession
    {
        private readonly RhythmRating[] ratings;
        private readonly RhythmChart chart;
        private readonly double perfectWindow;
        private readonly double goodWindow;
        private readonly double hitWindow;
        private int earnedPoints;
        private const double BoundaryTolerance = 0.000000001;

        public event Action<RhythmHitResult> Judged;
        public int TotalNotes => ratings.Length;
        public int MaximumScore => TotalNotes * 100;
        public int Score => earnedPoints;
        public double ScorePercentage => 100.0 * Score / MaximumScore;
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Perfects { get; private set; }
        public int Goods { get; private set; }
        public int Oks { get; private set; }
        public int Misses { get; private set; }
        public int ExtraPresses { get; private set; }
        public int ResolvedNotes { get; private set; }
        public double LastHitWindowEnd => chart.GetNote(TotalNotes - 1).TimeSeconds + hitWindow;

        public RhythmScoreSession(RhythmChart chart, double perfectWindow = 0.05,
            double goodWindow = 0.1, double hitWindow = 0.15)
        {
            if (chart == null || chart.NoteCount <= 0 ||
                !IsFinite(perfectWindow) || !IsFinite(goodWindow) || !IsFinite(hitWindow) ||
                perfectWindow <= 0 || goodWindow < perfectWindow || hitWindow < goodWindow)
            {
                throw new ArgumentOutOfRangeException(nameof(chart), "Invalid chart or timing windows.");
            }
            ratings = new RhythmRating[chart.NoteCount];
            this.chart = chart;
            this.perfectWindow = perfectWindow;
            this.goodWindow = goodWindow;
            this.hitWindow = hitWindow;
        }

        public bool IsResolved(int noteIndex) => ratings[noteIndex] != RhythmRating.None;

        public void Advance(double songTime)
        {
            if (!IsFinite(songTime)) throw new ArgumentOutOfRangeException(nameof(songTime));
            for (int i = 0; i < TotalNotes; i++)
            {
                var note = chart.GetNote(i);
                if (note.TimeSeconds + hitWindow + BoundaryTolerance >= songTime) break;
                if (!IsResolved(i)) Resolve(new RhythmHitResult(i, note.Lane, RhythmRating.Miss));
            }
        }

        public RhythmHitResult Press(int lane, double songTime)
        {
            if (lane < 0 || lane > 1) throw new ArgumentOutOfRangeException(nameof(lane));
            if (!IsFinite(songTime)) throw new ArgumentOutOfRangeException(nameof(songTime));
            Advance(songTime);
            if (songTime < chart.GetNote(0).TimeSeconds - hitWindow - BoundaryTolerance ||
                songTime > LastHitWindowEnd + BoundaryTolerance || ResolvedNotes == TotalNotes)
            {
                return new RhythmHitResult(-1, lane, RhythmRating.None);
            }
            int closest = -1;
            double closestDistance = double.MaxValue;
            for (int i = 0; i < TotalNotes; i++)
            {
                var note = chart.GetNote(i);
                double distance = Math.Abs(songTime - note.TimeSeconds);
                if (note.Lane == lane && !IsResolved(i) && distance <= hitWindow + BoundaryTolerance && distance < closestDistance)
                {
                    closest = i;
                    closestDistance = distance;
                }
            }
            if (closest < 0)
            {
                ExtraPresses++;
                Combo = 0;
                var empty = new RhythmHitResult(-1, lane, RhythmRating.EmptyPress);
                Judged?.Invoke(empty);
                return empty;
            }
            var rating = closestDistance <= perfectWindow + BoundaryTolerance ? RhythmRating.Perfect :
                closestDistance <= goodWindow + BoundaryTolerance ? RhythmRating.Good : RhythmRating.Ok;
            var result = new RhythmHitResult(closest, lane, rating, songTime - chart.GetNote(closest).TimeSeconds);
            Resolve(result);
            return result;
        }

        private void Resolve(RhythmHitResult result)
        {
            ratings[result.NoteIndex] = result.Rating;
            ResolvedNotes++;
            switch (result.Rating)
            {
                case RhythmRating.Perfect: Perfects++; earnedPoints += 100; break;
                case RhythmRating.Good: Goods++; earnedPoints += 70; break;
                case RhythmRating.Ok: Oks++; earnedPoints += 40; break;
                case RhythmRating.Miss: Misses++; Combo = 0; break;
            }
            if (result.Rating != RhythmRating.Miss)
            {
                Combo++;
                BestCombo = Math.Max(BestCombo, Combo);
            }
            Judged?.Invoke(result);
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
