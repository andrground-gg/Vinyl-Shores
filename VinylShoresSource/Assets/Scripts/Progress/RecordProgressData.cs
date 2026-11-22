using System;
using System.Collections.Generic;
using UnityEngine;

namespace VinylShores.Progress
{
    /// <summary>What happened when a challenge result was reported.</summary>
    public readonly struct RecordSubmission
    {
        public string RecordId { get; }
        /// <summary>False when the id is not one of the nine records (nothing changed).</summary>
        public bool Accepted { get; }
        public int Stars { get; }
        public bool Passed => Stars > 0;
        /// <summary>True only the first time this record is earned.</summary>
        public bool GrantedNow { get; }
        /// <summary>True when this attempt beat the previous best stars.</summary>
        public bool ImprovedBest { get; }
        public int BestStars { get; }

        public RecordSubmission(string recordId, bool accepted, int stars, bool grantedNow, bool improvedBest, int bestStars)
        {
            RecordId = recordId;
            Accepted = accepted;
            Stars = stars;
            GrantedNow = grantedNow;
            ImprovedBest = improvedBest;
            BestStars = bestStars;
        }
    }

    /// <summary>
    /// Session progress for the nine records. Plain C# object: no scene, no GameObject.
    /// Collected records and best stars are tracked separately.
    /// Scoring stays in the rhythm system: callers report 0-3 stars (0 = below 50%, failed).
    /// </summary>
    public sealed class RecordProgressData
    {
        public const int MaxStarsPerRecord = 3;

        private readonly bool logRejectedInput;
        private readonly HashSet<string> collected = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> bestStars = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Raised after a record is granted or its best stars improve.</summary>
        public event Action<RecordSubmission> Changed;

        /// <param name="logRejectedInput">Log unknown ids and out-of-range stars. Automated checks pass false.</param>
        public RecordProgressData(bool logRejectedInput = true) => this.logRejectedInput = logRejectedInput;

        public int CollectedCount => collected.Count;
        public int MaxTotalStars => RecordCatalog.Count * MaxStarsPerRecord;

        /// <summary>Sum of the best stars of the nine records (0-27). The boss is never included.</summary>
        public int TotalBestStars
        {
            get
            {
                int total = 0;
                foreach (int stars in bestStars.Values) total += stars;
                return total;
            }
        }

        public bool HasRecord(string recordId) => recordId != null && collected.Contains(recordId);

        public int GetBestStars(string recordId) =>
            recordId != null && bestStars.TryGetValue(recordId, out int stars) ? stars : 0;

        public int GetCollectedCount(IslandArea area)
        {
            int count = 0;
            foreach (var record in RecordCatalog.InArea(area))
                if (collected.Contains(record.Id)) count++;
            return count;
        }

        /// <summary>An area is complete when its three records are collected. Stars are irrelevant.</summary>
        public bool IsAreaComplete(IslandArea area) => GetCollectedCount(area) >= RecordCatalog.RecordsPerArea;

        /// <summary>First record of the area, in order, that is not collected yet. False when the area is complete.</summary>
        public bool TryGetNextMissingRecord(IslandArea area, out RecordDefinition record)
        {
            foreach (var candidate in RecordCatalog.InArea(area))
            {
                if (!collected.Contains(candidate.Id))
                {
                    record = candidate;
                    return true;
                }
            }
            record = default;
            return false;
        }

        /// <summary>
        /// Reports one finished challenge. 0 stars = failed: nothing changes.
        /// 1-3 stars = passed: the record is granted once, best stars can only rise.
        /// Ids outside the catalog (including the final boss) are rejected.
        /// </summary>
        public RecordSubmission ReportChallengeResult(string recordId, int stars)
        {
            if (!RecordCatalog.IsRecord(recordId))
            {
                if (logRejectedInput) Debug.LogError($"[RecordProgress] '{recordId}' is not a record id. Nothing was granted.");
                return new RecordSubmission(recordId, false, 0, false, false, 0);
            }
            if (stars < 0 || stars > MaxStarsPerRecord)
            {
                if (logRejectedInput) Debug.LogWarning($"[RecordProgress] Stars {stars} for '{recordId}' are outside 0-{MaxStarsPerRecord}; clamped.");
                stars = Mathf.Clamp(stars, 0, MaxStarsPerRecord);
            }

            int previousBest = GetBestStars(recordId);
            if (stars == 0) return new RecordSubmission(recordId, true, 0, false, false, previousBest);

            bool grantedNow = collected.Add(recordId);
            bool improved = stars > previousBest;
            if (improved) bestStars[recordId] = stars;
            var submission = new RecordSubmission(recordId, true, stars, grantedNow, improved, Math.Max(stars, previousBest));
            if (grantedNow || improved) Changed?.Invoke(submission);
            return submission;
        }

        /// <summary>Clears all progress. Listeners stay subscribed.</summary>
        public void Reset()
        {
            collected.Clear();
            bestStars.Clear();
        }
    }
}
