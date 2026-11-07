# NPC Dialogue Integration Contract

This document describes how the dialogue prototype connects to the rest of
Vinyl Shores. It is the contract that the movement, world and rhythm systems
should follow when they integrate the dialogue.

## Scope

The prototype currently covers one repeatable NPC (`Old Spin`) in
`Assets/Scenes/DialogueTest.unity`. It is a standalone test scene and is not
part of the build settings yet.

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
| `Assets/Scripts/Dialogue/DialogueTestPlayerMover.cs` | Temporary test mover; the real controller should replace it. |
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

The real player controller must implement this interface. `DialogueInteractable`
caches the component from the player transform and calls
`SetMovementEnabled(false)` when the dialogue opens and `true` when it closes.

`DialogueTestPlayerMover` is a temporary implementation used by the test scene.

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
- `Player` with `Rigidbody2D`, `CapsuleCollider2D` and
  `DialogueTestPlayerMover`.
- `NPC_OldSpin` instantiated from `DialogueNpc.prefab` at `(2, 0)`.
- `NPC_DockWorker` instantiated from `DialogueNpc.prefab` at `(4, 0)`, with
  its own lines and no portrait.
- `DialogueCanvas` instantiated from `DialogueCanvas.prefab`.

Each NPC also has a `DialogueTestEventLogger` that logs when its
`onDialogueFinished` event fires. This helper is for the test scene only and
can be removed once the integration tests exist.

The scene is not added to `EditorBuildSettings` on purpose. `SampleScene`,
tilemaps, rhythm code and `InputSystem_Actions.inputactions` remain untouched.

## Current status

- Dialogue, prompt, portrait and movement lock: implemented and manually tested.
- `onDialogueFinished`: implemented, currently with no listeners.
- Dialogue prefabs: implemented.
- Rhythm challenge: not implemented; waiting for the rhythm system.
- Push, pull request and colleague test: pending.
