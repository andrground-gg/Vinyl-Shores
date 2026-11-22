using System;
using System.Collections.Generic;
using UnityEngine;

namespace VinylShores.Progress
{
    /// <summary>What happened when a challenge result was submitted.</summary>
    public readonly struct RecordSubmission
    {
        public string RecordId { get; }
        public int Stars { get; }
        public bool Passed => Stars > 0;
        /// <summary>True only the first time this record is earned.</summary>
        public bool GrantedNow { get; }
        /// <summary>True when this attempt beat the previous best stars.</summary>
        public bool ImprovedBest { get; }
        public int BestStars { get; }

        public RecordSubmission(string recordId, int stars, bool grantedNow, bool improvedBest, int bestStars)
        {
            RecordId = recordId;
            Stars = stars;
            GrantedNow = grantedNow;
            ImprovedBest = improvedBest;
            BestStars = bestStars;
        }
    }

    /// <summary>
    /// Minimal in-memory record progress for the playable slice.
    /// A record is granted once, on the first passing result (1-3 stars).
    /// Replays only improve the best stars. Stars never gate progression.
    /// Nothing is saved to disk; state resets when Play Mode starts.
    /// </summary>
    public static class RecordProgress
    {
        public const int MaxStars = 3;

        private static readonly HashSet<string> collected = new HashSet<string>();
        private static readonly Dictionary<string, int> bestStars = new Dictionary<string, int>();

        /// <summary>Raised after a record is granted or its best stars improve.</summary>
        public static event Action<RecordSubmission> Changed;

        public static int CollectedCount => collected.Count;

        public static int TotalBestStars
        {
            get
            {
                int total = 0;
                foreach (int stars in bestStars.Values) total += stars;
                return total;
            }
        }

        public static bool HasRecord(string recordId) =>
            !string.IsNullOrEmpty(recordId) && collected.Contains(recordId);

        public static int GetBestStars(string recordId) =>
            !string.IsNullOrEmpty(recordId) && bestStars.TryGetValue(recordId, out int stars) ? stars : 0;

        /// <summary>
        /// Submits one challenge result. Stars of 0 mean the attempt failed (below 50%)
        /// and change nothing. Passing results grant the record once and keep the best stars.
        /// </summary>
        public static RecordSubmission Submit(string recordId, int stars)
        {
            if (string.IsNullOrEmpty(recordId)) throw new ArgumentException("Record id is empty.", nameof(recordId));
            stars = Mathf.Clamp(stars, 0, MaxStars);
            int previousBest = GetBestStars(recordId);
            if (stars == 0) return new RecordSubmission(recordId, 0, false, false, previousBest);

            bool grantedNow = collected.Add(recordId);
            bool improved = stars > previousBest;
            if (improved) bestStars[recordId] = stars;
            var submission = new RecordSubmission(recordId, stars, grantedNow, improved, Math.Max(stars, previousBest));
            if (grantedNow || improved) Changed?.Invoke(submission);
            return submission;
        }

        /// <summary>Clears all progress. Used at Play Mode start and by tests.</summary>
        public static void ResetAll()
        {
            collected.Clear();
            bestStars.Clear();
        }

        // Static fields survive between Play Mode sessions when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            ResetAll();
            Changed = null;
        }
    }
}
