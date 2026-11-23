using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VinylShores.Progress;

/// <summary>
/// Editor menu that checks the record progress rules with controlled results.
/// Uses its own RecordProgressData, so it never touches the play session.
/// Run: Tools > Vinyl Shores > Run Record Progress Checks (works outside Play Mode).
/// </summary>
public static class RecordProgressChecks
{
    private static int passed;
    private static readonly List<string> failures = new List<string>();

    [MenuItem("Tools/Vinyl Shores/Run Record Progress Checks")]
    public static void Run()
    {
        passed = 0;
        failures.Clear();

        CheckCatalog();
        CheckFail();
        CheckFirstSuccess();
        CheckRepeatedSuccess();
        CheckWorseAndBetterReplay();
        CheckFailAfterOwned();
        CheckAreaCompletion();
        CheckRejectedIds();
        CheckStarClamping();
        CheckTotals();

        if (failures.Count == 0)
        {
            Debug.Log($"[Record progress checks] ALL {passed} CHECKS PASSED.");
        }
        else
        {
            foreach (string failure in failures) Debug.LogError("[Record progress checks] FAILED: " + failure);
            Debug.LogError($"[Record progress checks] {failures.Count} failed, {passed} passed.");
        }
    }

    private static void CheckCatalog()
    {
        Expect(RecordCatalog.Count == 9, "catalog has 9 records");
        var ids = new HashSet<string>();
        foreach (var record in RecordCatalog.All) ids.Add(record.Id);
        Expect(ids.Count == 9, "record ids are unique");
        foreach (IslandArea area in Enum.GetValues(typeof(IslandArea)))
        {
            int count = 0;
            foreach (var _ in RecordCatalog.InArea(area)) count++;
            Expect(count == RecordCatalog.RecordsPerArea, $"{area} has 3 records");
        }
        Expect(!RecordCatalog.IsRecord(RecordCatalog.FinalBossChallengeId), "final boss is not a record");
    }

    private static void CheckFail()
    {
        var data = new RecordProgressData();
        int events = 0;
        data.Changed += _ => events++;
        var result = data.ReportChallengeResult("beach_1", 0);
        Expect(result.Accepted && !result.Passed && !result.GrantedNow, "fail (<50%, 0 stars) is accepted but not passed");
        Expect(!data.HasRecord("beach_1"), "fail grants nothing");
        Expect(data.GetBestStars("beach_1") == 0 && data.CollectedCount == 0, "fail leaves stars and count at 0");
        Expect(events == 0, "fail raises no Changed event");
    }

    private static void CheckFirstSuccess()
    {
        var data = new RecordProgressData();
        int events = 0;
        data.Changed += _ => events++;
        var result = data.ReportChallengeResult("beach_1", 2);
        Expect(result.GrantedNow && result.Passed, "first success grants the record");
        Expect(data.HasRecord("beach_1") && data.CollectedCount == 1, "record is collected once");
        Expect(data.GetBestStars("beach_1") == 2, "best stars stored on first success");
        Expect(events == 1, "first success raises one Changed event");
    }

    private static void CheckRepeatedSuccess()
    {
        var data = new RecordProgressData();
        data.ReportChallengeResult("beach_1", 2);
        int events = 0;
        data.Changed += _ => events++;
        var result = data.ReportChallengeResult("beach_1", 2);
        Expect(!result.GrantedNow && !result.ImprovedBest, "repeated equal success grants nothing new");
        Expect(data.CollectedCount == 1, "repeated success never duplicates the record");
        Expect(events == 0, "repeated equal success raises no event");
    }

    private static void CheckWorseAndBetterReplay()
    {
        var data = new RecordProgressData();
        data.ReportChallengeResult("beach_1", 2);
        var worse = data.ReportChallengeResult("beach_1", 1);
        Expect(!worse.ImprovedBest && worse.BestStars == 2, "worse replay keeps best stars");
        Expect(data.GetBestStars("beach_1") == 2, "worse replay does not lower stars");
        var better = data.ReportChallengeResult("beach_1", 3);
        Expect(better.ImprovedBest && !better.GrantedNow && better.BestStars == 3, "better replay improves stars only");
        Expect(data.CollectedCount == 1, "replays keep exactly one record");
    }

    private static void CheckFailAfterOwned()
    {
        var data = new RecordProgressData();
        data.ReportChallengeResult("beach_1", 3);
        data.ReportChallengeResult("beach_1", 0);
        Expect(data.HasRecord("beach_1") && data.GetBestStars("beach_1") == 3, "failed replay keeps record and stars");
    }

    private static void CheckAreaCompletion()
    {
        var data = new RecordProgressData();
        Expect(data.TryGetNextMissingRecord(IslandArea.Beach, out var next) && next.Id == "beach_1", "next missing starts at beach_1");
        data.ReportChallengeResult("beach_1", 1);
        Expect(data.TryGetNextMissingRecord(IslandArea.Beach, out next) && next.Id == "beach_2", "next missing moves to beach_2");
        data.ReportChallengeResult("beach_2", 1);
        Expect(!data.IsAreaComplete(IslandArea.Beach) && data.GetCollectedCount(IslandArea.Beach) == 2, "2/3 records is not complete");
        data.ReportChallengeResult("beach_3", 1);
        Expect(data.IsAreaComplete(IslandArea.Beach), "three records complete the Beach (1 star each is enough)");
        Expect(data.GetCollectedCount(IslandArea.Beach) == 3, "Beach count is 3");
        Expect(!data.TryGetNextMissingRecord(IslandArea.Beach, out _), "complete area has no next missing record");
        Expect(!data.IsAreaComplete(IslandArea.Forest) && data.GetCollectedCount(IslandArea.Forest) == 0, "other areas unaffected");
        Expect(data.TryGetNextMissingRecord(IslandArea.Forest, out next) && next.Id == "forest_1", "Forest next missing is forest_1");
    }

    private static void CheckRejectedIds()
    {
        var data = new RecordProgressData(logRejectedInput: false);
        Expect(!data.ReportChallengeResult(RecordCatalog.FinalBossChallengeId, 3).Accepted, "final boss result is rejected as a record");
        Expect(!data.ReportChallengeResult("Beach_Guardian", 3).Accepted, "NPC names are not record ids");
        Expect(!data.ReportChallengeResult("", 3).Accepted, "empty id is rejected");
        Expect(!data.ReportChallengeResult(null, 3).Accepted, "null id is rejected");
        Expect(data.CollectedCount == 0 && data.TotalBestStars == 0, "rejected ids change nothing");
    }

    private static void CheckStarClamping()
    {
        var data = new RecordProgressData(logRejectedInput: false);
        data.ReportChallengeResult("forest_1", 7);
        Expect(data.GetBestStars("forest_1") == 3, "stars above 3 are clamped to 3");
    }

    private static void CheckTotals()
    {
        var data = new RecordProgressData();
        data.ReportChallengeResult("beach_1", 3);
        data.ReportChallengeResult("beach_2", 2);
        data.ReportChallengeResult("forest_1", 1);
        data.ReportChallengeResult("forest_1", 0);
        Expect(data.TotalBestStars == 6 && data.MaxTotalStars == 27, "total best stars is 6 out of 27");
        Expect(data.CollectedCount == 3, "three records collected across areas");
        data.Reset();
        Expect(data.CollectedCount == 0 && data.TotalBestStars == 0, "reset clears progress");
    }

    private static void Expect(bool condition, string description)
    {
        if (condition) passed++;
        else failures.Add(description);
    }
}
