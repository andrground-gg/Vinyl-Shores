# Beach Record Loop

One complete record loop on Beach: guardian dialogue -> rhythm challenge ->
result / retry -> return to the same Beach position -> record granted once.
Builds on PR #3 (Beach integration) and PR #7 (guardians open the rhythm
challenge). Owner: Aitor.

## What changed

| File | Change |
| --- | --- |
| `Assets/Scripts/Progress/RecordProgress.cs` | Session progress. Full API in [Record_Progress.md](Record_Progress.md). |
| `Assets/Scripts/Rhythm/BeachRhythmChallenge.cs` | `BeachRhythmChallenge` gets a `recordId`, a `State` and three UnityEvents. `BeachRhythmSession` receives the challenge id and a return callback, and keeps the best attempt of the visit. |
| `Assets/Scripts/Rhythm/RhythmPrototypeController.cs` | One line: when a song finishes it calls `BeachRhythmSession.ReportResult(percentage)`. |
| `Assets/Scenes/Beach_Level/Beach.unity` | `Beach_Guardian`, `Kid2`, and `Gate_Guardian` grant `beach_1`, `beach_2`, and `beach_3` respectively. The Islander and Knight require the preceding record. |

## Challenge interface

```text
DialogueInteractable.onDialogueFinished
  -> BeachRhythmChallenge.StartChallenge()
  -> BeachRhythmSession.StartChallenge(scene, sprite, recordId, callback)   // Beach roots hidden, rhythm scene loaded additively
  -> each finished song: BeachRhythmSession.ReportResult(percentage)       // Restart = retry, best attempt kept
  -> "Return to beach": rhythm scene unloaded, Beach roots restored
  -> callback(BeachRhythmOutcome { Attempts, BestPercentage, Stars })
  -> BeachRhythmChallenge: < 50% -> nothing; >= 50% -> RecordProgress.ReportChallengeResult(recordId, stars)
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

`BeachDialogue` uses the first-win and failure events to show the guardian's
response on return. A later interaction with a guardian whose record is already
owned offers a replay. Every result is logged to the Console with the prefix
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
| 8 | Before collecting the previous record, talk to `Kid2` or `Gate_Guardian`. Then collect the prerequisite and pass their challenge. | The locked dialogue starts no song. A pass after unlocking grants the respective record. |
| 9 | Exit and re-enter Play Mode. | Progress starts empty again (`Records: 1` after the first pass). |
| 10 | Pause, then leave during the first attempt. | The player returns to the map without a result dialogue or a newly granted record. |

## Known limits

- Progress is not saved between sessions (out of scope for the slice).
- All three guardians open the same Beach song.
- The exit gate and transition to the next area are still pending.
