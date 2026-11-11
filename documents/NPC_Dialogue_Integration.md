# NPC Dialogue Integration Contract

This document describes how the dialogue prototype connects to the rest of
Vinyl Shores. It is the contract that the movement, world and rhythm systems
should follow when they integrate the dialogue.

## Scope

The standalone prototype covers two repeatable NPCs (`Old Spin` and
`Dock Worker`) in `Assets/Scenes/DialogueTest.unity`. It is a standalone test
scene and is not part of the build settings yet.

The rhythm challenge is **not implemented** in the repository. This document
defines the hook and the expected contract so the rhythm work can plug in
without changing the dialogue code.

## Files

| File | Responsibility |
| --- | --- |
| `Assets/Scripts/Core/IPlayerMovementLock.cs` | Movement-lock contract implemented by the player controller. |
| `Assets/Scripts/Dialogue/DialogueLine.cs` | Serialized line: speaker, optional portrait, text. |
| `Assets/Scripts/Dialogue/DialogueUI.cs` | Screen-space dialogue UI, prompt, portrait and dialogue state. |
| `Assets/Scripts/Dialogue/DialogueInteractable.cs` | Distance-based NPC interaction, movement lock and `onDialogueFinished`. |
| `Assets/Scripts/Player/PlayerMovement.cs` | Shared four-direction controller from the movement prototype. |
| `Assets/Scripts/Player/PlayerMovementLock.cs` | Adapter that implements `IPlayerMovementLock` by toggling `PlayerMovement`. |
| `Assets/Prefabs/Dialogue/DialogueNpc.prefab` | Reusable NPC with collider, visuals and dialogue lines. |
| `Assets/Prefabs/Dialogue/DialogueCanvas.prefab` | Reusable dialogue UI. |
| `Assets/Editor/DialogueArtLibrary.cs` | Shared editor helpers for sprites, placeholders and serialized lines. |
| `Assets/Editor/DialoguePrefabBuilder.cs` | Editor menu that generates the dialogue prefabs. |
| `Assets/Editor/DialogueTestSceneBuilder.cs` | Editor menu that generates the test scene from the prefabs. |

## Interaction flow

1. The player moves within `interactionRadius` (1.5 world units by default).
2. `DialogueInteractable` shows the `Press E to talk` prompt.
3. Pressing `Player/Interact` opens the dialogue when the player is in range.
4. Each press advances one line. The last press closes the dialogue.
5. Closing restores movement and raises `onDialogueFinished`.
6. The interaction is repeatable: there is no "consumed" flag.

Input notes:

- `Player/Interact` stays configured as a `Hold` interaction in
  `InputSystem_Actions.inputactions`. The dialogue subscribes to
  `InputAction.started`, so a quick tap works without editing the asset.
- Only one press is consumed per frame, so two nearby NPCs cannot advance or
  reopen the dialogue with the same key event.

## Prefab behavior

The prefabs are intentionally unwired:

- `DialogueInteractable.player` and `DialogueInteractable.ui` are left empty in
  `DialogueNpc.prefab`.
- At runtime, `DialogueInteractable` fills empty references:
  - `player` comes from the first `MonoBehaviour` that implements
    `IPlayerMovementLock`, with a `Player` tag fallback.
  - `ui` comes from `FindFirstObjectByType<DialogueUI>` (inactive objects
    included).
- References assigned in the inspector always win; auto-resolution only fills
  null fields.

This keeps the NPC prefab drop-in for any scene that contains a player
controller and a `DialogueCanvas`.

## Multiple NPCs

Several NPCs can share one `DialogueUI`:

- Every in-range NPC requests the prompt through
  `DialogueUI.SetPromptRequest`. The prompt stays visible while at least one
  NPC requests it, and hides only when the last requester leaves.
- Pressing `Interact` opens the dialogue of the nearest in-range NPC.
  Distances are compared in world units; exact ties are broken by instance ID
  so only one NPC handles the press.
- Only the NPC that opened the dialogue advances it and raises
  `onDialogueFinished`.
- NPCs that are out of range, inactive or disabled do not block the nearest
  active NPC.
- `DialogueTest.unity` contains two NPCs (`NPC_OldSpin` and
  `NPC_DockWorker`) to exercise this behavior.

## Movement lock

The dialogue blocks movement through `IPlayerMovementLock`:

```csharp
public interface IPlayerMovementLock
{
    void SetMovementEnabled(bool enabled);
}
```

`DialogueInteractable` caches the component from the player transform and calls
`SetMovementEnabled(false)` when the dialogue opens and `true` when it closes.

The player uses the shared `PlayerMovement` controller from the movement
prototype. `PlayerMovementLock` adapts it to the interface:

- It disables `PlayerMovement` while a dialogue is open. `PlayerMovement`'s
  `OnDisable` already zeroes its direction and velocity, so the body stops
  immediately.
- It re-enables the controller only if the lock was the one that disabled it,
  so it never overrides another system that paused movement on purpose.
- `PlayerMovement.cs` is not modified. Keeping the adapter separate avoids
  conflicts with the movement branch and lets the controller change freely.

## Branch integration

`feature/npc-dialogue` is rebased on top of `feature/movement-prototype`, so it
contains the shared controller and compiles on its own. `Assets/Scripts.meta`
uses the movement branch GUID, so merging both branches does not conflict.

## `onDialogueFinished`

`DialogueInteractable.onDialogueFinished` is a `UnityEvent` that fires every
time the dialogue closes after its last line.

Contract:

- It fires **after** movement has been restored.
- It carries no data; the listener owns the challenge reference.
- It fires on every close, so listeners must keep their own state to avoid
  restarting a challenge that is already completed.

Typical listener flow:

```text
onDialogueFinished
  -> if challenge state is Pending: start the rhythm challenge
  -> re-lock movement while the challenge runs
```

## Rhythm challenge contract (proposed)

The rhythm system does not exist yet. When it is implemented, it should expose
something equivalent to:

```csharp
public interface IRhythmChallengeLauncher
{
    void StartChallenge(string challengeId);
    event Action<bool, int> ChallengeCompleted; // success, stars
}
```

Recommended bridge, owned by the gameplay/integration layer:

1. Add an `NpcChallengeTrigger` component next to `DialogueInteractable`.
2. Subscribe it to `onDialogueFinished`.
3. When the NPC state is `Pending`, call `StartChallenge(challengeId)`.
4. Re-lock movement through `IPlayerMovementLock` while the challenge runs.
5. On `ChallengeCompleted(success, stars)`:
   - `Completed` on success, `Failed` on failure.
   - Unlock movement.
   - Raise a separate `UnityEvent` to award the record or change the dialogue.
6. Allow a retry after `Failed`; do not award the record twice.

The dialogue system must not depend on the rhythm system. The trigger is the
only bridge between them.

## Per-NPC state

Each NPC that owns a challenge should track at least:

- `Pending`: dialogue has not started the challenge yet.
- `Running`: the rhythm challenge is active.
- `Completed`: the challenge was cleared and the reward was granted.
- `Failed`: the challenge failed and can be retried.

`onDialogueFinished` fires regardless of this state, so the trigger decides
whether to start the challenge.

## Test scene

`Assets/Scenes/DialogueTest.unity` is generated by
`Tools > Vinyl Shores > Build Dialogue Test Scene`. It contains:

- `Main Camera` (orthographic, size 5).
- `Global Light 2D`.
- `Player` with `Rigidbody2D`, `CapsuleCollider2D`, `PlayerMovement` and
  `PlayerMovementLock`.
- `NPC_OldSpin` instantiated from `DialogueNpc.prefab` at `(2, 0)`.
- `NPC_DockWorker` instantiated from `DialogueNpc.prefab` at `(4, 0)`, with
  its own lines and no portrait.
- `DialogueCanvas` instantiated from `DialogueCanvas.prefab`.

Movement uses WASD or the arrow keys. `E` only interacts, so there is no input
overlap between the controller and the dialogue.

Each NPC also has a `DialogueTestEventLogger` that logs when its
`onDialogueFinished` event fires. This helper is for the test scene only and
can be removed once the integration tests exist.

The scene is not added to `EditorBuildSettings` on purpose. `SampleScene`,
tilemaps, rhythm code and `InputSystem_Actions.inputactions` remain untouched.

## Beach integration

Open `Assets/Scenes/Beach_Level/Beach.unity` in Unity **6000.3.12f1**.
The scene has one player, one shared `DialogueCanvas`, and four stationary NPCs:

| Scene object | Sprite variant | Role |
| --- | --- | --- |
| `Old-Spin` | `OldMan2` | Old Spin, the introductory guide. |
| `Beach_Guardian` | `Village6` | Record guardian 1, the beach musician. |
| `Kid2` | `Villager3` | Record guardian 2, a young islander. |
| `Gate_Guardian` | `Knight` | Record guardian 3, located by the gate. |

`Kid1` was removed. Existing names of the retained roots are preserved so map
work can still identify the same objects. The knight is one of the three record
guardians, not an additional gate-opening NPC.

All four have an explicit reference to the scene player and shared `DialogueUI`.
The player has `PlayerMovementLock` and the `Player` tag. Interact with E as in
the prototype; closing a conversation restores movement, and conversations can
be repeated. The four NPCs use solid **2D** colliders instead of the map draft's
3D colliders. A global 2D light makes the existing Sprite-Lit materials visible.

The unchanged movement controller runs at 4 units/second with four directions
and last-pressed-key priority. Following visual review, Beach's player and all
four NPCs are **one world unit tall**, with the Boy visual's aspect ratio
preserved. NPCs sit at grid-cell centers with centered **1 x 1** boxes, so the
player cannot stand over their bodies. The player's centered **0.85 x 0.85** box
allows clearance through one-cell passages. The player starts at a dock cell's
center. Environment collision is painted on the separate **Collision** tilemap
in whole 1 x 1 cells; Water and Obstacles are visual layers without colliders.
See [collision editing and validation](Beach_Integration_Review.md). The standalone
movement prototype retains its original settings. The unfinished Animator is
not assigned.

Character textures used by Beach are isolated in `Assets/Sprites/BeachCharacters`:
Boy, OldMan2, Knight, Village6 and Villager3. These are copies from Ninja Adventure,
not new external artwork. They use 16 PPU, 16x16 slices, centered pivots, Point
filtering and no mipmaps/compression. Original shared character importers and
GUIDs remain as on main, preserving MovementTest and DialogueTest references.
Do not move or reimport the shared character sheets to reproduce Beach's scale.

The map retains its tiles, palette and Cinemachine **3.1.7** camera. Invisible
blocked Collision tiles at the Ground tilemap's perimeter prevent walking off the map;
they do not implement a progression gate. Project build settings are unchanged.
The same Collision layer covers the pot and tall sign on the south peninsula,
stumps, barrel and the other solid props; it does not use separate prop boxes.
Three missing inner foam corners (the reported peninsula corner and two dock
corners) use existing water tiles; shared tile assets/importers are unchanged.

The pack already supplies `SeparateAnim/Idle.png` (four 16x16 frames) and
`SeparateAnim/Walk.png` (sixteen 16x16 frames) for all five selected characters.
The Idle strip contains directional standing poses, not a breathing loop.
Player directional walking/idle and stationary NPC poses are tracked in
[the animation task](https://trello.com/c/HTDv0uXE). They are not implemented in
this scene yet. Attack/jump/death/special sheets are outside the game's current
scope.

### Temporary limits

- Dialogue text is placeholder English text. Old Spin gives a static clue to
  the first guardian northwest along the beach.
- All guardians are available immediately. Dialogue completion awards no
  record and does not start a fake challenge; `onDialogueFinished` has no
  progression listener in Beach.
- Rhythm challenges, record state, sequential hints, gate unlocking and pause
  are separate integration work.
- All NPCs remain stationary. GDD wandering for introductory NPCs is deferred.
- Keep new map edits on a branch based on the merged integration; coordinate
  changes to the shared Beach scene before merging concurrent scene edits.

## Current status

- Dialogue, prompt, portrait and movement lock: implemented and manually tested.
- Movement integration: uses the shared `PlayerMovement` controller through
  `PlayerMovementLock`; the branch is rebased on `feature/movement-prototype`.
- `onDialogueFinished`: implemented; the test scene wires it to a logger.
- Dialogue prefabs: implemented.
- Rhythm challenge: not implemented; waiting for the rhythm system.
- The standalone movement and dialogue prototypes are merged into main (PRs
  #1 and #2). Beach integration is reviewed and delivered separately.
