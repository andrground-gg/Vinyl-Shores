# Rhythm prototype

Open `Assets/Scenes/Prototypes/RhythmPrototype.unity` in Unity 6000.3.12f1.
Press Start or Space, listen to the four-beat count-in, then press Left Arrow
for the upper lane and Right Arrow for the lower lane. Each note requires a
new press; holding a key does not repeat hits.

The original metronome is 100 BPM, 4/4, 50 bars. The chart contains 200
alternating tap notes at `beat * 0.6` seconds, from 0 to 119.4 seconds.
Notes travel right to left with a 2.4-second approach.

| Judgement | Absolute timing error | Points |
| --- | --- | --- |
| Perfect | Up to 50 ms | 100 |
| Good | Over 50, up to 100 ms | 70 |
| OK | Over 100, up to 150 ms | 40 |
| Miss | No matching press within 150 ms | 0 |

Windows include their endpoints and can be adjusted on `RhythmPlayback`.
An extra or wrong-lane press resets combo without deducting points or
consuming another lane's note. Misses also reset combo. The percentage is
earned points divided by the full chart maximum (20,000), including missed
notes. The result screen shows counts, best combo, and whether the 50%
threshold was reached. Restart clears the attempt.

`+ / =` delays the display and judgement clock by 10 ms; `-` advances it.
Backspace resets the offset. This setting is stored locally in PlayerPrefs
under `VinylShores.RhythmPrototype.VisualDelayMs`, not in the shared scene.
Calibrate for the current output device before comparing scores. Changing
calibration during a run can move notes across judgement windows.

`AudioSettings.dspTime` drives scheduled playback and presentation. Input
event timestamps are mapped into that clock before calibration and scoring;
judgement does not depend on note collisions or accumulated frame time.
`RhythmScoreSession` owns note resolution, points, and combo;
`RhythmLaneView` draws pending notes and flashes the hit marker.

This remains an isolated prototype: no Beach integration, record rewards,
star persistence, hold notes, or pause menu. Shared movement, dialogue, and
Input Actions are unchanged.
