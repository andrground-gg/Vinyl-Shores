using System;

namespace VinylShores.Rhythm
{
    public enum RhythmRating { None, Perfect, Good, Ok, Miss, EmptyPress }

    public readonly struct RhythmHitResult
    {
        public int Beat { get; }
        public int Lane { get; }
        public RhythmRating Rating { get; }
        public double ErrorSeconds { get; }

        public RhythmHitResult(int beat, int lane, RhythmRating rating, double errorSeconds = 0)
        {
            Beat = beat;
            Lane = lane;
            Rating = rating;
            ErrorSeconds = errorSeconds;
        }
    }

    /// <summary>Judges a two-lane alternating chart using calibrated song timestamps, independently of frames.</summary>
    public sealed class RhythmScoreSession
    {
        private readonly RhythmRating[] ratings;
        private readonly double secondsPerBeat;
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

        public RhythmScoreSession(int totalNotes, double secondsPerBeat, double perfectWindow = 0.05,
            double goodWindow = 0.1, double hitWindow = 0.15)
        {
            if (totalNotes <= 0 || totalNotes > int.MaxValue / 100 || !IsFinite(secondsPerBeat) || secondsPerBeat <= 0 ||
                !IsFinite(perfectWindow) || !IsFinite(goodWindow) || !IsFinite(hitWindow) ||
                perfectWindow <= 0 || goodWindow < perfectWindow || hitWindow < goodWindow)
            {
                throw new ArgumentOutOfRangeException(nameof(totalNotes), "Invalid chart or timing windows.");
            }
            ratings = new RhythmRating[totalNotes];
            this.secondsPerBeat = secondsPerBeat;
            this.perfectWindow = perfectWindow;
            this.goodWindow = goodWindow;
            this.hitWindow = hitWindow;
        }

        public bool IsResolved(int beat) => ratings[beat] != RhythmRating.None;

        public void Advance(double songTime)
        {
            if (!IsFinite(songTime)) throw new ArgumentOutOfRangeException(nameof(songTime));
            for (int beat = 0; beat < TotalNotes; beat++)
            {
                if (beat * secondsPerBeat + hitWindow + BoundaryTolerance >= songTime) break;
                if (!IsResolved(beat)) Resolve(new RhythmHitResult(beat, beat % 2, RhythmRating.Miss));
            }
        }

        public RhythmHitResult Press(int lane, double songTime)
        {
            if (lane < 0 || lane > 1) throw new ArgumentOutOfRangeException(nameof(lane));
            if (!IsFinite(songTime)) throw new ArgumentOutOfRangeException(nameof(songTime));
            Advance(songTime);
            if (songTime < -hitWindow - BoundaryTolerance ||
                songTime > (TotalNotes - 1) * secondsPerBeat + hitWindow + BoundaryTolerance || ResolvedNotes == TotalNotes)
            {
                return new RhythmHitResult(-1, lane, RhythmRating.None);
            }
            int closest = -1;
            double closestDistance = double.MaxValue;
            for (int beat = lane; beat < TotalNotes; beat += 2)
            {
                double distance = Math.Abs(songTime - beat * secondsPerBeat);
                if (!IsResolved(beat) && distance <= hitWindow + BoundaryTolerance && distance < closestDistance)
                {
                    closest = beat;
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
            var result = new RhythmHitResult(closest, lane, rating, songTime - closest * secondsPerBeat);
            Resolve(result);
            return result;
        }

        private void Resolve(RhythmHitResult result)
        {
            ratings[result.Beat] = result.Rating;
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
