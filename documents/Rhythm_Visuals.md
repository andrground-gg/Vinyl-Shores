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

Boy and Village6 use dedicated 16Ã—16 front-facing Idle/Jump imports under
`Assets/Sprites/RhythmCharacters`. Shared character sheets are unchanged.
Each actor hops locally with a landing squash, lean, and shadow. Presentation
uses calibrated DSP time and changes cadence at landings. Player energy is
`0.65 Ã— average(last 8 judgements) + 0.35 Ã— min(combo/20, 1)`; quality is
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
The arcade HUD uses Press Start 2P, distributed with its OFL license under `Assets/Art/Rhythm/Fonts`.

## Location colours

The arcade HUD uses cream text on opaque navy frames. Beach uses mint, Forest
yellow and Mountains lavender accents for combo and progress. Combo has an ink
outline and a dark COMBO badge.
Colours are authored in RhythmStageView Themes and snapshotted at Start/Restart.

## Pixel Arcade HUD

`RhythmPixelImage` is an Image subclass that draws stepped frames, target
brackets, pixel arrows and fixed-pitch progress segments. It remains compatible
with the existing Image references used for hit feedback. Each lane owns its
frame and target; note templates carry their own arrow geometry. No shared sprite
sheets are changed. The scene still has two lanes: adding more will require lane
layout, bindings and chart validation changes; the decorative frame has no fixed
row count. Painting and dancing actors remain unchanged.

Only HUD typography is changed; result-screen typography is retained. The initial
Ready panel stays hidden, with automatic four-beat count-in. Results and retry
continue to reuse the existing overlay.

## Arcade depth and feedback pass

Style: layered pixel UI over the existing painted locations and pixel actors.
Palette: ink `#06111F`, panel navy `#163047`, cream `#FFE3A6`, gold `#FFC961`,
coral `#FF876E`; location accents remain mint/yellow/lavender for combo/progress.
Mood: bright outdoor locations, with UI readability carried by blue shadows and outlines.

HUD is top anchored. Rows are 58 UI units high at a 66-unit pitch, starting at
local Y 232. Four rows would occupy Y 232/166/100/34 without covering actors.
The active two-row layout keeps combo beneath the judgement column; when wiring
four lanes, move the combo root from Y 52 to Y -80. This is layout preparation,
not four-lane input support. Row-local note clipping follows the new lane bounds;
DSP travel and scoring stay unchanged.

Time labels and their separator live inside the progress panel. Layered frame
rims, keycap bevels, top highlights and pixel shadows share the same navy palette.
Large judgement labels omit the small early/late timing detail. Success pops,
rises and emits a short pooled-in-place pixel burst; Miss/Extra sinks and tilts.
Feedback uses absolute DSP age, replaces the previous label, and resets on restart.
Combo gains an ink outline, capped punch and tilt; zero/one remain hidden.

Craft review is scoped to the HUD (game-feel and visual-scorecard references).
Palette discipline: 4/5 — blue panels, ink shadows and controlled warm accents.
UI intentionality: 4/5 — aligned header, compact independent rows, time enclosed.
Feedback visibility: 4/5 — outlined labels and distinct success/failure motion.
Before this pass these scoped axes were approximately 3/5, 2/5 and 2/5.
3D lighting and character style floors were outside this 2D HUD change. The chosen
painted-background/pixel-character mixture is retained. No new audio feedback is
added, preserving the metronome timing reference. Unity DSP-time documentation and
the installed Pipeline script/lifecycle documentation were consulted for verification.

## Flat reference refinement

The latest reference pass removes keycap bevels, highlights, UI drop shadows,
segment shading and text vertex gradients. Panels and notes use flat navy fills
with larger cream/gold/coral borders; text keeps its dark ink outline. Keycaps
are 48 units and arrows 35 units inside the existing 58-unit lanes. Judgements
use 26-unit lettering, combo uses 52, with smaller angular motion.

PlaybackTime is one right-aligned TMP label (`elapsed / total`) in the progress
panel. The former separate time labels and separator are hidden. The controller
updates the combined label while preserving legacy references. A narrower gauge
reserves a compact, consistent space for the time string; there is no manual
spacing between three independent text elements.

## Solid lettering and arrow silhouette

HUD lettering now uses the dense Jersey 10 face with a static Latin atlas and
bundled OFL license. The contour is a centred uniform ink expansion (zero X/Y
offset), while the face outline is kept narrow so the coloured letter body is
not eaten by the border. This is a flat contour, not directional lighting or a
cast shadow. Header, timer, judgements, combo and count-in share the face; result
screen typography is unchanged. Line boxes are sized for this font's metrics.

Arrows use an explicit symmetric 9x7 pixel mask with a three-row shaft and a
solid triangular head. The 27-unit silhouette sits in a 32-unit icon box inside
the 48-unit keycap, leaving clear space from the surrounding frame. Left and
Right are exact mirrored silhouettes; there are no bevels, lighting or gradients.

## Combo intensity

Number colour eases over 0.18 seconds from the location accent through gold
(roughly combo 25) to coral at 60; higher combos keep the final colour. At combo
10, a subtle continuous motion starts on the number only. Its strength/cadence
rises to combo 60 and caps there: 2.5 UI units horizontally, 1.625 vertically,
1.5 degrees and 1.5% scale. The existing bounded on-hit punch remains. The badge
and lane geometry do not drift. Reset clears both pose and colour progression;
0/1 remain hidden and a break still fades the old number red. Tunables are on
RhythmComboView; time comes from DSP age, with no flashing or accumulated offsets.

## Scorecard results and rounded awards

The selected two-column scoreboard uses pixel stars and rounded percentage/points
on the left, five judgement counts on the right, and a separate gold-framed Best
Combo achievement below the counts. Restart is available immediately and cancels
animations. Header, table and button share the flat navy/cream pixel HUD style.

Stars and the pass/fail title use the unrounded percentage: 49.5 fails,
69.5 earns one star and 89.5 earns two. The displayed percentage is rounded
down. Raw points, hit ratings and the score denominator are unchanged.
The counter animates toward the displayed percentage. Unawarded stars remain empty; no persistent rewards
or ending progression are introduced.

## Result sprite states

Result stars now use dedicated Point-filtered `StarFilled`/`StarEmpty` sprites in
`Assets/Art/Rhythm/ResultSprites`, enlarged to 125 UI units. The filled star is
flat gold with an ink edge; the empty star has a continuous cream contour.
Existing sequential fill masks use the unrounded award thresholds.

Restart uses native Button Sprite Swap with dedicated normal (cream), hover/
selected (mint), pressed (gold), and disabled (muted) sprites. Sliced borders
preserve the pixel corners across its ready/results sizes. The label uses a solid
unoutlined material and moves down two units on press, restoring on release,
exit and disable. The sprite-state assets are authored by the editor utility,
not screenshots or temporary review files.

## Result left-column proportions

Stars are slightly fuller and sit at local Y 112. The percentage is the left
column's hero element (256-point type, Y -60); points below use 88-point type at
Y -200. Both share X -270 and a 470-unit text width. This fills the left column
while retaining the right-hand statistics and Best Combo layout. 0%, 80% and 100%
and maximum point strings are checked in 16:9 and 4:3 using visible glyph bounds.
