using System;
using UnityEngine;

namespace VinylShores.Progress
{
    /// <summary>
    /// Global access to the current play session's progress.
    /// One RecordProgressData instance lives here for the whole session, so it survives
    /// loading the rhythm challenge and returning to Beach. It is a static class, not a
    /// GameObject singleton: nothing is added to scenes and nothing can be duplicated.
    /// Nothing is saved to disk; a new session starts each time Play Mode starts.
    /// </summary>
    public static class RecordProgress
    {
        public static RecordProgressData Session { get; private set; } = new RecordProgressData();

        public static event Action<RecordSubmission> Changed
        {
            add => Session.Changed += value;
            remove => Session.Changed -= value;
        }

        public static int CollectedCount => Session.CollectedCount;
        public static int TotalBestStars => Session.TotalBestStars;
        public static int MaxTotalStars => Session.MaxTotalStars;

        public static bool HasRecord(string recordId) => Session.HasRecord(recordId);
        public static int GetBestStars(string recordId) => Session.GetBestStars(recordId);
        public static int GetCollectedCount(IslandArea area) => Session.GetCollectedCount(area);
        public static bool IsAreaComplete(IslandArea area) => Session.IsAreaComplete(area);
        public static bool TryGetNextMissingRecord(IslandArea area, out RecordDefinition record) =>
            Session.TryGetNextMissingRecord(area, out record);
        public static RecordSubmission ReportChallengeResult(string recordId, int stars) =>
            Session.ReportChallengeResult(recordId, stars);

        // A fresh session (and no stale listeners) every time Play Mode starts,
        // even when domain reload is disabled in the project settings.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void StartNewSession() => Session = new RecordProgressData();
    }
}
