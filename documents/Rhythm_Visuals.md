# Rhythm presentation

The rhythm prototype combines painted location backgrounds with separate
pixel characters and ground from Ninja Adventure. Select **Location** on a
Rhythm Song asset: **Beach**, **Forest**, or **Mountains**. The location is
snapshotted on Start/Restart; changing an asset during a run does not change
the active presentation.

During play the HUD contains the track title, progress/time, notes,
judgements, and one combo readout beside the hit area. Combo 0/1 is hidden;
increments pop with capped strength rising up to combo 40, multiples of ten add a small extra impulse, and a break briefly
fades the previous number in red. Device calibration and its local saved
value/keyboard controls remain functional without an on-screen debug line.

Boy and Village6 use dedicated 16×16 front-facing Idle/Jump imports under
`Assets/Sprites/RhythmCharacters`. Shared character sheets are unchanged.
Each actor hops locally with a landing squash, lean, and shadow. Presentation
uses calibrated DSP time and changes cadence at landings. Player energy is
`0.65 × average(last 8 judgements) + 0.35 × min(combo/20, 1)`; quality is
1/0.7/0.4/0 for Perfect/Good/OK/failure. Opponent energy is its inverse.
Failures briefly squash the player and force low energy for two beats.
Low-energy actors stay grounded and gently compress vertically instead of hopping.
Strong reactions have a two-beat cooldown. Finish settles both actors; Restart resets them.

Results show points, percentage, hit statistics, best combo, and three
procedural UI stars. Thresholds remain 50/70/90%. The counter rises over
1.2 seconds; earned stars fill sequentially in 0.25 seconds each, with a
small scale pulse. Unearned stars stay empty. Restart cancels the result
animation. No rewards, persistence, Beach integration, or ending logic are
added by this presentation layer.

## Generated background assets

Saved in `Assets/Art/Rhythm/Backdrops/`: `Beach.png`, `Forest.png`, and
`Mountains.png`. Generated using the built-in imagegen tool, then imported
with Bilinear filtering. Each keeps its aspect ratio and covers the
viewport with centre cropping; pixel foreground imports use Point filtering.

Shared prompt: full-screen 16:9 fantasy rhythm-game landscape; soft
hand-painted gouache/watercolour texture, natural light and atmospheric
depth; calm low-detail upper area, detail at sides and lower third; safe
central 4:3 crop; no text, UI, characters, creatures, buildings, or pixel art.

- **Beach:** quiet tropical beach, turquoise sea, hazy coastal headlands,
  pale warm sand, palm fronds at outer edges, peach/blue afternoon sky.
- **Forest:** lush clearing framed by trees and ferns, cool teal distant
  woodland, diffuse canopy opening and warm filtered sunlight.
- **Mountains:** layered blue-grey peaks and mist, lavender/peach sky,
  rocky plateau and sparse alpine greenery; no lava or structures.

Track Title on the song asset supplies the header (blank uses the audio clip name).
Combo uses Lilita One, distributed with its OFL license under `Assets/Art/Rhythm/Fonts`.

## Location colours

Beach uses deep teal, Forest green and Mountains plum for the title and combo,
with a light outline. Progress uses a lighter matching accent on a dark track.
Colours are authored in RhythmStageView Themes and snapshotted at Start/Restart.
