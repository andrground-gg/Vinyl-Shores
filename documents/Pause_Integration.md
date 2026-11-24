# Exploration Pause — Integration Notes

Small, reusable pause menu for exploration. Escape opens and closes it, the
Resume button continues, and the panel shows a short controls hint.

This is **exploration only**. It does not pause music, does not touch
`Time.timeScale`, and does not connect to the rhythm challenge. The rhythm
challenge keeps its own Escape/pause path (see [Rhythm boundary](#rhythm-boundary)).

## Files

| Path | Purpose |
| --- | --- |
| `Assets/Scripts/UI/ExplorationPauseMenu.cs` | Runtime component: input, freeze/restore, panel and Resume button. |
| `Assets/Prefabs/UI/PauseCanvas.prefab` | Drop-in prefab (Canvas + component + generated panel). |
| `Assets/Editor/PausePrefabBuilder.cs` | Rebuilds the prefab (`Tools > Vinyl Shores > Build Pause Prefab`). |
| `Assets/Editor/PauseTestSceneBuilder.cs` | Rebuilds the test scene (`Tools > Vinyl Shores > Build Pause Test Scene`). |
| `Assets/Scenes/Prototypes/PauseTest.unity` | Standalone scene for walking, idle and open-dialogue tests. |
| `documents/Pause_Integration.md` | This document. |

## Add it to Beach

1. Open `Assets/Scenes/Beach_Level/Beach.unity`.
2. Drag `Assets/Prefabs/UI/PauseCanvas.prefab` into the scene root.
3. Save the scene. Nothing else is required.

The prefab resolves the player and the project-wide `Player` action map at
runtime. If the scene has no `EventSystem`, the component creates one with the
Input System's default UI actions so the Resume button works with the mouse.
If the scene already has an `EventSystem`, it is reused.

Do **not** add a second `ExplorationPauseMenu` to the scene: the component keeps
one instance and destroys duplicates.

## What is frozen while paused

| System | Action |
| --- | --- |
| Player movement | `PlayerMovement.enabled = false` (its `OnDisable` zeroes velocity). |
| Player physics | `Rigidbody2D.simulated = false`. |
| NPC interaction | `Player` action map disabled, so `Player/Interact` does not fire. |
| Dialogue advancement | Same action map; the full-screen panel also blocks mouse clicks. |
| UI buttons | Not frozen. The `UI` action map and the `EventSystem` stay active. |

`Time.timeScale` is **not** changed. `AudioListener.pause` and `BeachAudio` are
**not** touched.

## State preservation on Resume

The component records the previous state and restores it exactly:

| Situation before pause | Movement after Resume |
| --- | --- |
| Walking | Enabled, reads the keyboard again. |
| Idle | Enabled. |
| Dialogue open (dialogue lock already disabled movement) | Stays disabled until the dialogue closes. |
| Player action map disabled by another system | Stays disabled. |

This is why the component does not call `PlayerMovementLock.SetMovementEnabled(true)`:
that helper would re-enable movement even when the dialogue still owns the lock.
The pause menu snapshots `PlayerMovement.enabled` and restores that value.

## Leaving the scene

`OnDisable`/`OnDestroy` restore the captured state before the menu disappears.
This is required because the `Player` action map belongs to the project-wide
input asset, not to the scene. Without it, exiting Play mode or unloading the
scene while paused would leave the map disabled in the next scene.

## Rhythm boundary

- The component ignores Escape while `BeachRhythmSession.IsActive` is true.
- `BeachRhythmChallenge` deactivates every Beach root while a challenge is open,
  so the pause menu is inactive during rhythm gameplay anyway.
- `RhythmPrototypeController` keeps its own Escape handling, DSP timing and
  `AudioListener.pause`. This PR does not modify it.
- A future rhythm pause must suspend the audio and the chart clock **together**.
  `Time.timeScale` alone does not pause music. Coordinate that work with Vadim;
  do not route the exploration pause into the rhythm challenge.

## Test scene

Open `Assets/Scenes/Prototypes/PauseTest.unity` and press Play.

The scene contains the player, one dialogue NPC and both canvases. Walk right
with `A`/`D` or the arrow keys, press `E` next to the NPC, and press `Esc` at
any point.

## Play Mode checklist

1. **Idle:** press `Esc`. The panel appears, the player cannot move, the hint is
   visible. Press `Esc` again or click **Resume**. The panel closes and movement
   works.
2. **Walking:** hold `D`, press `Esc` while moving. The player stops immediately.
   Resume and confirm there is no drift.
3. **Open dialogue:** press `E` next to the NPC, then `Esc`. The dialogue stays
   visible behind the pause panel and movement stays locked. Click **Resume**.
   The dialogue is still open and movement is **still locked**. Press `E` to
   advance and close the dialogue; only then does movement return.
4. **Several cycles:** repeat `Esc`/Resume at least ten times, alternating the
   key and the button. There must be only one panel, and the game must never
   stay frozen.
5. **Blocked input:** while paused, press `E` and the movement keys, and click on
   the dialogue behind the panel. Nothing behind the menu reacts. Clicking
   **Resume** works.
6. **Scene exit:** pause, then stop Play mode (or load another scene). Start
   again and confirm movement and interaction still work.
7. **Rhythm:** start a guardian challenge. The exploration pause must not react
   to Escape during the challenge; the rhythm pause keeps working. After
   returning to the Beach, `Esc` opens the exploration pause again.

## Rebuild

- Prefab: `Tools > Vinyl Shores > Build Pause Prefab`
- Test scene: `Tools > Vinyl Shores > Build Pause Test Scene`

Both menu items run in the Unity editor. The test scene builder also rebuilds
the pause prefab and ensures the dialogue prefabs exist.

## Known limitations

- If `Esc` and `E` are pressed on the exact same frame, the interact event may
  already have been queued before the `Player` map is disabled. The dialogue
  then opens and the pause menu preserves it, so Resume still leaves movement
  locked until the dialogue closes. This is harmless but worth knowing.
- The menu does not pause music. That is intentional and belongs to the rhythm
  work.
- The panel uses legacy uGUI `Text` with the built-in runtime font, matching the
  dialogue UI. No new fonts or sprites are required.

## Handoff

- **Aitor (Beach):** only the prefab needs to be added to `Beach.unity`. No
  tilemap, collision, sprite, NPC or dialogue changes.
- **Vadim (rhythm):** the exploration pause is intentionally isolated from the
  rhythm clock. The future rhythm pause must suspend audio and chart timing
  together.
