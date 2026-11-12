# Rhythm pattern authoring

Create assets with **Create > Vinyl Shores > Rhythm Pattern**. Length Beats defines
its span; Notes are local beat positions with Left/Right lanes. Fractions are
supported (0.5 = an eighth note at the current BPM). All notes must fall within
[0, Length Beats). The pattern contains no audio or independent BPM.

In a Rhythm Song, **Patterns** places assets at absolute **Start Beat** positions.
**Mirror** swaps Left and Right without changing timing. **Notes** adds individual
notes to the same chart; leave it empty when using only patterns. Blank placement
references, invalid beats, duplicate lane/time notes and notes outside the audio
are rejected with an error. Overlapping placements are allowed if their notes
remain valid. Playback expands and sorts a private snapshot on Start/Restart;
editing assets during a run takes effect next attempt.

## Metronome arrangement

The 100 BPM / 4/4 / 50-bar track uses 50 manually arranged four-beat placements,
not random generation. Eight reusable patterns live in
`Assets/Audio/RhythmSongs/Patterns`:

| Pattern | Local beats | Lanes |
|---|---|---|
| Alternating | 0, 1, 2, 3 | L R L R |
| DoubleSteps | 0, 1, 2, 3 | L L R R |
| Breathing | 0, 1, 3 | L R L |
| Pickup | 0, 1, 2.5, 3 | L R L R |
| EighthRun | 0, 1, 2, 2.5, 3, 3.5 | L R L R L R |
| OffbeatReply | 0, 0.5, 1.5, 2, 3 | L R R L R |
| Staircase | 0, 0.5, 1, 2, 2.5, 3 | L R L R L R |
| Finale | 0, 1, 2, 3 | L R R L |

Bars 1-8 teach quarter notes and repeats; 9-16 introduce pickups; 17-24 introduce
short eighth-note runs; 25-32 relax; 33-46 combine familiar patterns; 47-50 resolve.
Some later phrases mirror lanes. There are no holds, chords or sixteenth notes.
The original OGG, BPM, first-beat offset, input bindings and scoring windows remain
unchanged. Minimum note spacing is 0.3 seconds; the last note is at 119.4 seconds.
This is a practice chart for a metronome, not automatic analysis of musical accents.
