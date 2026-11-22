# Beach Record Loop

One complete record loop on Beach: guardian dialogue -> rhythm challenge ->
result / retry -> return to the same Beach position -> record granted once.
Builds on PR #3 (Beach integration) and PR #7 (guardians open the rhythm
challenge). Owner: Aitor.

## What changed

| File | Change |
| --- | --- |
| `Assets/Scripts/Progress/RecordProgress.cs` | New. Minimal in-memory progress: `HasRecord`, `GetBestStars`, `Submit`, `CollectedCount`, `TotalBestStars`, `Changed` event. Resets when Play Mode starts. No save file. |
| `Assets/Scripts/Rhythm/BeachRhythmChallenge.cs` | `BeachRhythmChallenge` gets a `recordId`, a `State` and three UnityEvents. `BeachRhythmSession` receives the challenge id and a return callback, and keeps the best attempt of the visit. |
| `Assets/Scripts/Rhythm/RhythmPrototypeController.cs` | One line: when a song finishes it calls `BeachRhythmSession.ReportResult(percentage)`. |
| `Assets/Scenes/Beach_Level/Beach.unity` | `Beach_Guardian` has `recordId = beach_1`. `Kid2` and `Gate_Guardian` keep an empty id (practice only, grant nothing) until records 2 and 3 are wired. |

## Challenge interface

```text
DialogueInteractable.onDialogueFinished
  -> BeachRhythmChallenge.StartChallenge()
  -> BeachRhythmSession.StartChallenge(scene, sprite, recordId, callback)   // Beach roots hidden, rhythm scene loaded additively
  -> each finished song: BeachRhythmSession.ReportResult(percentage)       // Restart = retry, best attempt kept
  -> "Return to beach": rhythm scene unloaded, Beach roots restored
  -> callback(BeachRhythmOutcome { Attempts, BestPercentage, Stars })
  -> BeachRhythmChallenge: < 50% -> nothing; >= 50% -> RecordProgress.Submit(recordId, stars)
```

Rules implemented:

- Below 50% grants nothing (`onChallengeFailed`).
- The first passing result grants the record (`onRecordGranted`).
- Later passing results never grant it again; they only raise the best stars
  (`onReplayPassed`).
- If the player restarts several times before returning, the best attempt of
  that visit counts. A worse retry cannot remove a pass.
- Returning without finishing a song (audio error) changes nothing.
- Stars never gate anything.

The three UnityEvents are the hook for the guardian's success / failure /
replay dialogue variants (Beach dialogue script, Phase B). Nothing is wired to
them yet. Every result is logged to the Console with the prefix
`[Beach record loop]`, which is how the tests below are verified.

## Manual test (Unity 6000.3.12f1)

Open `Assets/Scenes/Beach_Level/Beach.unity`, keep the Console visible and
enter Play Mode. `Beach_Guardian` is in the northwest. Rhythm keys: Left/Right
arrows. Talk: E.

| # | Steps | Expected |
| --- | --- | --- |
| 1 | Walk, then talk to `Beach_Guardian` until the dialogue closes. | Rhythm scene opens with the guardian as opponent. No Console errors. |
| 2 | Fail on purpose (press nothing). Click **Return to beach**. | Same position, player can move, camera follows, music resumes, prompt appears near NPCs. Log: `failed ... Nothing granted. State: Failed`. |
| 3 | Talk to the guardian again, play and pass (>= 50%). Return. | Log: `Record 'beach_1' GRANTED`. `Records: 1`. |
| 4 | Talk again, pass again. Return. | Log: `already owned, not granted again`. `Records: 1`. Best stars only rise. |
| 5 | Talk again, fail. Return. | Log: `failed`, `State: Completed`. Record kept. |
| 6 | Talk again. In the rhythm scene: pass, click **Restart**, fail, then return. | Counted as a pass (best attempt). Not granted twice. |
| 7 | Repeat the full loop 3+ times. | No duplicate player, camera, dialogue UI or EventSystem in the Hierarchy. Movement never stuck. |
| 8 | Talk to `Kid2` or `Gate_Guardian`, pass, return. | Log: `Practice only: no record id`. `Records` unchanged. |
| 9 | Exit and re-enter Play Mode. | Progress starts empty again (`Records: 1` after the first pass). |
| 10 | Pause | Not available yet; re-test when the pause menu card is merged. |

## Known limits

- Progress is not saved between sessions (out of scope for the slice).
- Guardian dialogue does not change after a result yet; use the UnityEvents.
- All three guardians open the same Beach song.
- No way to leave a song while it is playing until pause exists.
