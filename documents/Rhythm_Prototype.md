# Rhythm prototype

Open `Assets/Scenes/Prototypes/RhythmPrototype.unity` in Unity 6000.3.12f1.
Press Start or Space, listen to the four-beat count-in, then press Left Arrow
for the upper lane and Right Arrow for the lower lane. Each note requires a
new press; holding a key does not repeat hits.

The original metronome is 100 BPM, 4/4, 50 bars. The chart contains 200
alternating tap notes at `beat * 0.6` seconds, from 0 to 119.4 seconds.
Notes travel right to left with a 2.4-second approach.

## Song assets and note authoring

`RhythmPlayback` references `Assets/Audio/RhythmSongs/Metronome100Bpm.asset`.
Select that asset to edit **Track**, **BPM**, **First Beat Offset Seconds**,
and **Notes**. Create another through **Assets > Create > Vinyl Shores >
Rhythm Song**, then assign it to the scene's **Song** field.
**Location** selects Beach, Forest, or Mountains; it is part of the attempt's
snapshot. See [Rhythm presentation](Rhythm_Visuals.md) for the visual setup.

Each note has a zero-based **Beat** position and a **Lane** (`Left` or
`Right`). Fractional beats are supported. For example, with 100 BPM and a
0.3-second first-beat offset, beat 2.5 hits at `0.3 + 2.5 * 60 / 100 = 1.8`
seconds into the file. Repeated lanes, rests, and different note counts
require no code changes. The initial asset preserves beats 0–199,
alternating Left/Right, with an offset of zero.

Notes are sorted in a runtime copy; the asset's list is not reordered or
modified by playing. Each attempt snapshots the song settings. Changes
made during Play Mode apply on the next Start/Restart, and edits to the
asset in Play Mode are persistent Unity asset edits. Invalid BPM, missing
audio, empty lists, invalid beats/lanes, same-lane duplicates, or notes at
or beyond the file's end show a recoverable error instead of starting.

The four count-in clicks follow the asset's BPM and lead into beat zero.
With an intro, audio may start during or before the count-in. Enough
pre-roll is reserved for the first note's full approach. Count-in sources
reuse a short runtime copy of the existing metronome's first click; no
audio file is edited. The result waits for the last note's late window.

## Scoring and calibration

| Judgement | Absolute timing error | Points |
| --- | --- | --- |
| Perfect | Up to 50 ms | 100 |
| Good | Over 50, up to 100 ms | 70 |
| OK | Over 100, up to 150 ms | 40 |
| Miss | No matching press within 150 ms | 0 |

Windows include their endpoints and can be adjusted on `RhythmPlayback`.
An extra or wrong-lane press resets combo without deducting points or
consuming another lane's note. Misses also reset combo. The percentage is
earned points divided by `note count * 100` (20,000 for the initial asset), including missed
notes. During play only combo 2 and above is shown beside the hit markers.
The result screen animates points, percentage, and earned stars at
50/70/90%, alongside counts and best combo. Restart clears the attempt.

`+ / =` delays the display and judgement clock by 10 ms; `-` advances it.
Backspace resets the offset. This setting is stored locally in PlayerPrefs
under `VinylShores.RhythmPrototype.VisualDelayMs`, not in the shared scene.
The former debug text is hidden; these calibration controls still work.
Calibrate for the current output device before comparing scores. Changing
calibration during a run can move notes across judgement windows.

`AudioSettings.dspTime` drives scheduled playback and presentation. Input
event timestamps are mapped into that clock before calibration and scoring;
judgement does not depend on note collisions or accumulated frame time.
`RhythmScoreSession` owns note resolution, points, and combo;
`RhythmLaneView` draws pending notes and flashes the hit marker.

Each lane shows its own judgement immediately above the hit marker, with
Early/Late timing beside successful hits. Arrow presses briefly compress
and expand the receptor; hits add a bright, expanding square pulse, while
misses and extra presses use red feedback. Labels fade after 0.7 seconds.
All effects use absolute DSP time and keep the marker's centre fixed.
Restart, disable, and scene exit clear the effects.

This remains an isolated prototype: no Beach integration, record rewards,
star persistence, hold notes, or pause menu. Shared movement, dialogue, and
Input Actions are unchanged.
