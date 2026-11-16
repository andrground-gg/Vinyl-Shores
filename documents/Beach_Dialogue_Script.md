# Beach Dialogue Script

Approved dialogue content for the four stationary NPCs of
`Assets/Scenes/Beach_Level/Beach.unity`. This document is the source of truth
for the text; it does not describe code.

## Status

- **Phase A (current):** this document only. `Beach.unity` still contains the
  original placeholder lines and has **not** been edited.
- **Phase B (blocked):** the lines are copied into the scene and wired to the
  record state, the rhythm result and the gate state. Phase B needs the
  progression API and the rhythm outcome, neither of which exists on `main`.
- The scene is a shared file. Announce the Phase B scene edit to Aitor before
  starting it.

Because of that split, nothing in this file is playable yet. Do not paste
individual lines into `Beach.unity` outside the Phase B integration: the
current scene text contradicts this script (see
[Current scene text](#current-scene-text-outdated)).

## Scope

- Content only. No new NPCs, sprites, map, tilemap, collision, input or
  dialogue-framework changes.
- The four NPCs stay stationary: `Old-Spin`, `Beach_Guardian`, `Kid2`,
  `Gate_Guardian`.
- The three records belong to the three guardians. There are no separate vinyl
  objects in the scene.
- Every line is English, one or two short sentences, and must fit the existing
  `DialogueUI` panel. Re-check wrapping in Play Mode when the lines are applied.

## Speakers

| Speaker field | Character | Notes |
| --- | --- | --- |
| `Old Spin` | Old Spin | Guide and composer. |
| `Boy` | Player | The player character. `portrait` stays empty. |
| `Beach Guardian` | Record guardian 1 | Beach musician. |
| `Islander` | Record guardian 2 | Young islander (`Kid2` in the scene). |
| `Knight` | Record guardian 3 | `Gate_Guardian` in the scene. |

The scene currently uses `Record Guardian 1/2/3` for the guardians. The names
above replace them in Phase B.

## Voices

| Character | Tone | Vocabulary |
| --- | --- | --- |
| Old Spin | Old, warm, tired, poetic. Short sentences. Moved by the records. | "child", "old man", "the sea", "the shore", "long ago" |
| Boy | Young, polite, determined, enthusiastic. | "I came looking for inspiration", "I'll help you", "We have a deal" |
| Beach Guardian | Young musician, relaxed, artistic, informal. | "friend", "groove", "beat", "tide" |
| Islander | Young islander, energetic, competitive, informal. | "Whoa", "Bet you can't", "cool", "island beat" |
| Knight | Formal, stern, archaic, protective. Blunt at first, respectful after. | "Halt", "state your purpose", "duty", "by right", "you shall" |

## Story rules

- Old Spin does **not** know how many records exist or what happened to them.
  He never announces a count. The player learns the number as records return.
- The player starts the conversation: he is looking for inspiration and help to
  compose his own album, and proposes a trade.
- Old Spin accepts the trade and then remembers fragments, one record at a time.
  The player asks where each record was composed.
- Hints use the real Beach landmarks:
  - Record 1: northwest, past the stumps, where the trees meet the sand.
  - Record 2: east, past the cabin, to the barrel by the rocks.
  - Record 3: north, through the trees, to the stone gate.
- Ordinary dialogue never grants a record and never fakes a success. Success
  lines play only after a real rhythm success.
- Stars never change the hint and never unlock access.
- The Knight is guardian 3. He guards the record, not the gate, and says so.
- Guardian order and access rules belong to the progression integration. Until
  it exists, the guardians keep an honest "not connected" message.

## Old Spin

### First conversation (0 records)

| # | Speaker | Line |
| --- | --- | --- |
| 1 | Old Spin | Well, now. A visitor? The years don't bring many strangers here. |
| 2 | Boy | I came looking for inspiration. They say you're a great composer. |
| 3 | Old Spin | A great composer? That was a long time ago, child. |
| 4 | Old Spin | I wrote an album once. Then I lost it. Or it lost me. |
| 5 | Old Spin | Since then, I haven't had the strength to write or to search. |
| 6 | Boy | I'll help you find your album. In return, help me compose mine. |
| 7 | Old Spin | A trade? You help an old man, and he helps you? |
| 8 | Old Spin | All right. We have a deal. |
| 9 | Boy | Do you remember where you wrote any of them? |
| 10 | Old Spin | The first one... I wrote it with the beach musician. |
| 11 | Old Spin | Northwest, past the stumps, where the trees meet the sand. |
| 12 | Old Spin | He may have kept a copy. Ask about the tide and the rhythm. |

This conversation doubles as the re-readable first hint. Phase A has no variant
selector, so repeating the interaction replays these lines.

### Repeat hint 1 (0 records, introduction already seen)

| # | Speaker | Line |
| --- | --- | --- |
| 1 | Old Spin | The first one was with the beach musician. |
| 2 | Old Spin | Northwest, past the stumps, where the trees meet the sand. |
| 3 | Old Spin | He may have kept a copy. Ask about the tide and the rhythm. |

### After record 1 (1 record)

| # | Speaker | Line |
| --- | --- | --- |
| 1 | Old Spin | That label... I know that label. You found it. |
| 2 | Old Spin | Put it here, child. Let me hear it again. |
| 3 | Old Spin | There's another. I wrote it on the east side, near the old barrel. |
| 4 | Old Spin | The young islander would know. They keep the songs of the shore. |
| 5 | Old Spin | Go east, past the cabin. Look for the barrel by the rocks. |

### Repeat hint 2 (1 record)

| # | Speaker | Line |
| --- | --- | --- |
| 1 | Old Spin | The next one was with the young islander. |
| 2 | Old Spin | East, past the cabin, to the barrel by the rocks. |
| 3 | Old Spin | They know the songs I wrote for the shore. |

### After record 2 (2 records)

| # | Speaker | Line |
| --- | --- | --- |
| 1 | Old Spin | Two. Two of them, back from the sea. |
| 2 | Old Spin | I can almost hear the whole thing again. |
| 3 | Old Spin | One more... I wrote it by the stone gate, north through the trees. |
| 4 | Old Spin | The knight watched that place. He may keep more than duty. |
| 5 | Old Spin | Ask him for the record. He guards it, not the gate. |

### Repeat hint 3 (2 records)

| # | Speaker | Line |
| --- | --- | --- |
| 1 | Old Spin | The last one was with the knight. |
| 2 | Old Spin | North through the trees, by the stone gate. |
| 3 | Old Spin | He guards the record, not the gate. |

### Beach complete (3 records)

| # | Speaker | Line |
| --- | --- | --- |
| 1 | Old Spin | Three. Three songs I thought the sea had taken. |
| 2 | Old Spin | I don't know if there are more. I don't even remember writing all of them. |
| 3 | Old Spin | But these three... these three are home. |
| 4 | Boy | We make a good team. |
| 5 | Old Spin | We do. And I still owe you a song, child. |
| 6 | Old Spin | Rest first. Then we'll cross the gate and search for the rest of the album. |

### Beach complete, gate open (3 records, gate unlocked)

Same lines 1-5, with the last line replaced:

| # | Speaker | Line |
| --- | --- | --- |
| 6 | Old Spin | The gate is open. Rest, then we'll find the rest of the album. |

## Beach Guardian (record 1)

### First greeting and challenge invitation

| # | Line |
| --- | --- |
| 1 | Hey, friend. Old Spin sent you? He still remembers the tide, huh. |
| 2 | This record's got a good groove. I kept it safe. |
| 3 | Think you can keep up with it? |

### Repeat invitation

| # | Line |
| --- | --- |
| 1 | The tide's right. Let's ride that groove. |
| 2 | Keep the beat and the record is yours. |

### Failure and retry

| # | Line |
| --- | --- |
| 1 | Ah, the beat got away from you. No shame. |
| 2 | Shake it off. Try again when you're ready. |

### Success

| # | Line |
| --- | --- |
| 1 | Yeah! That's the groove. Here's the record. |
| 2 | There's more of his music out there. I can feel it. |

### Replay (record 1 already collected)

| # | Line |
| --- | --- |
| 1 | You already got this one, friend. |
| 2 | Want to ride the rhythm again? |

## Islander (record 2)

### First greeting and challenge invitation

| # | Line |
| --- | --- |
| 1 | Whoa, a new face! Old Spin sent you? He never sends anyone. |
| 2 | I've been keeping one of his records safe. Island secret. |
| 3 | Bet you can't keep up with our rhythm! |

### Repeat invitation

| # | Line |
| --- | --- |
| 1 | You came back! Ready for the island beat? |
| 2 | Play with me and the record is yours. |

### Failure and retry

| # | Line |
| --- | --- |
| 1 | Aw, close! The island beat is tricky, huh? |
| 2 | Try again! I'm not going anywhere. |

### Success

| # | Line |
| --- | --- |
| 1 | Yes! You got it! Here's the record. |
| 2 | There's one more song out there. I can feel it. |

### Replay (record 2 already collected)

| # | Line |
| --- | --- |
| 1 | You already beat me. That was cool. |
| 2 | Wanna go again? I'll go easy. Maybe. |

### Locked by sequence (record 1 not collected)

| # | Line |
| --- | --- |
| 1 | Hey, Old Spin's first song comes first. |
| 2 | Bring it back, then we'll talk rhythm. |

## Knight (record 3)

### First greeting and challenge invitation

| # | Line |
| --- | --- |
| 1 | Halt. State your purpose. |
| 2 | Old Spin's record lies under my watch. The gate is not my charge. |
| 3 | If you would claim it, prove your rhythm. |

### Repeat invitation

| # | Line |
| --- | --- |
| 1 | You return. Steady your hands. |
| 2 | Play, and the record is yours by right. |

### Failure and retry

| # | Line |
| --- | --- |
| 1 | You falter. The record remains with me. |
| 2 | Return when your hands are steady. |

### Success

| # | Line |
| --- | --- |
| 1 | Well fought. The last beach record is yours. |
| 2 | Tell Old Spin. His shore is whole again. |

### Replay (record 3 already collected)

| # | Line |
| --- | --- |
| 1 | The record is already yours. I have no quarrel with you. |
| 2 | I can play the song again, if you wish. |

### Locked by sequence (record 2 not collected)

| # | Line |
| --- | --- |
| 1 | The earlier records must be found before mine. |
| 2 | I keep the order, not the gate. |

## Current scene text (outdated)

The scene still holds the original placeholders. They are listed here so the
Phase B diff is easy to review.

| NPC | Speaker in scene | Lines |
| --- | --- | --- |
| `Old-Spin` | `Old Spin` | "Welcome to Vinyl Shores. Three of my records are being kept safe on this beach." / "Look northwest along the beach for the first record guardian." / "You can talk to all three guardians now. Their musical challenges are coming later." |
| `Beach_Guardian` | `Record Guardian 1` | "I'm the beach musician, and I'm looking after Old Spin's first record." / "My rhythm challenge is not connected yet. Come back when the music is ready." |
| `Kid2` | `Record Guardian 2` | "I've been keeping Old Spin's second record safe here on the island." / "My rhythm challenge is not connected yet. Come back when the music is ready." |
| `Gate_Guardian` | `Record Guardian 3` | "I guard Old Spin's third record here by the gate." / "My rhythm challenge is not connected yet. No record is awarded by this conversation." |

Problems with this text:

- Old Spin announces three records and a list of guardians, which breaks the
  gradual discovery.
- The guardians reveal their record number before the player can know it.
- The speaker labels are placeholders, not character names.
- Old Spin promises "musical challenges" as future content, which reads as a
  teaser rather than an honest placeholder.

## Phase B checklist

Text is already written. Wiring needs:

1. Progression state: records collected, last rhythm result, gate state.
2. A variant source for `DialogueInteractable` (lightweight selector or
   external provider). Do not rewrite `DialogueInteractable` or `DialogueUI`
   beyond adding that selector.
3. Apply the speaker names and the line arrays to the four scene components.
4. Verify in Play Mode: line wrapping, movement lock and restore, repeat,
   failure and retry, success only after a real rhythm win, three records,
   completion, and gate-open variant.
5. Confirm stars do not change hints or access.
6. Announce the `Beach.unity` edit to Aitor before merging.
