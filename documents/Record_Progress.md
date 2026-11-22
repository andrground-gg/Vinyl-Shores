# Record Progress

Session progress for the nine records: which records are collected and the best
stars of each one. Data and a small API only. No scene transitions, gate
visuals, save/load or songs. Owner: Aitor.

## Files

| File | Responsibility |
| --- | --- |
| `Assets/Scripts/Progress/RecordCatalog.cs` | `IslandArea` (Beach, Forest, Mountain), `RecordDefinition` and the nine stable ids. |
| `Assets/Scripts/Progress/RecordProgressData.cs` | The progress rules. Plain C# object; can be created in isolation for tests. |
| `Assets/Scripts/Progress/RecordProgress.cs` | Static access to the session instance (`RecordProgress.Session`). |
| `Assets/Editor/RecordProgressChecks.cs` | Editor menu with automated checks using controlled results. |

## Record ids

Ids are stable strings. They never depend on NPC or scene object names.

| Area | Records (in collection order) |
| --- | --- |
| Beach | `beach_1`, `beach_2`, `beach_3` |
| Forest | `forest_1`, `forest_2`, `forest_3` |
| Mountain | `mountain_1`, `mountain_2`, `mountain_3` |

The final boss uses `RecordCatalog.FinalBossChallengeId` (`mountain_boss`). It
is **not** a record: reporting it to the progress is rejected and changes
nothing. Its stars are never part of the total.

A guardian chooses its record through the `recordId` field of
`BeachRhythmChallenge` in the Inspector (`Beach_Guardian` = `beach_1`).
Unknown ids are rejected with a Console error, so typos are visible.

## API

All calls go through `RecordProgress` (namespace `VinylShores.Progress`).

| Call | Returns |
| --- | --- |
| `HasRecord(id)` | Whether the record is collected. |
| `GetBestStars(id)` | Best stars of that record, 0-3 (0 = never passed). |
| `ReportChallengeResult(id, stars)` | Reports one finished challenge. Returns a `RecordSubmission` (`Accepted`, `Passed`, `GrantedNow`, `ImprovedBest`, `BestStars`). |
| `GetCollectedCount(area)` | Records collected in an area, 0-3. |
| `IsAreaComplete(area)` | True when the area's three records are collected. |
| `TryGetNextMissingRecord(area, out record)` | First record of the area, in order, that is still missing. False when complete. |
| `CollectedCount` | Records collected on the island, 0-9. |
| `TotalBestStars` / `MaxTotalStars` | Sum of best stars of the nine records / 27. For the final character's dialogue. |
| `Changed` event | Raised when a record is granted or its best stars improve. |

Example (Old Spin's next hint, gate check):

```csharp
using VinylShores.Progress;

if (RecordProgress.TryGetNextMissingRecord(IslandArea.Beach, out RecordDefinition next))
    Debug.Log("Next hint for " + next.Id);   // beach_1, then beach_2, then beach_3
bool gateOpen = RecordProgress.IsAreaComplete(IslandArea.Beach);
```

## Rules (from the GDD)

- **Scoring stays in the rhythm system.** The caller converts the percentage to
  stars with `RhythmResultView.StarsForPercentage` and reports 0-3 stars.
- **0 stars (below 50%) = failed:** nothing is granted and nothing changes.
- **1-3 stars = passed:** the record is granted the first time only.
- **Replays** never duplicate the record. A better replay raises the best
  stars; a worse or failed replay keeps them.
- **Collected records and best stars are tracked separately.**
- **Stars never unlock anything.** An area is complete with its three records,
  whatever their stars.
- **Final boss** is a separate challenge, not record ten.

## Session lifetime

`RecordProgress` is a static class holding one `RecordProgressData`. It is not
a GameObject, so it is never duplicated by scene loads and needs no setup in a
scene. The same instance survives opening the rhythm challenge and returning to
Beach. A new, empty session starts each time Play Mode starts (also with domain
reload disabled). Nothing is written to disk; persistence can be a later task.

Existing contracts are unchanged: `IPlayerMovementLock`, `DialogueUI` and
`onDialogueFinished` are not modified. No production dialogue reads the
progress yet, so nothing shows a fake completion.

## Checks (Unity 6000.3.12f1)

### Automated (controlled results)

Menu **Tools > Vinyl Shores > Run Record Progress Checks** (outside Play Mode).
The Console must show `[Record progress checks] ALL ... CHECKS PASSED.`

It covers: catalog (9 unique ids, 3 per area, boss not a record), fail, first
success, repeated success, worse and better replay, failed replay after owning,
next missing record, three-record area completion with 1 star each, other areas
unaffected, rejected ids (boss, NPC name, empty), star clamping, totals and
reset. The checks use their own `RecordProgressData` and never touch the
session.

### In game (session survives the rhythm scene)

1. Open `Beach.unity`, enter Play Mode, talk to `Beach_Guardian`, pass, return.
   Console: `Record 'beach_1' GRANTED ... Beach: 1/3 ... Records: 1/9`.
2. Talk to `Beach_Guardian` again, pass, return.
   Console: `already owned, not granted again ... Records: 1/9`.
   The record survived going into the rhythm challenge and back.
3. Exit and enter Play Mode again: the first pass is `GRANTED` again (new session).
