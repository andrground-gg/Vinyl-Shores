using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using VinylShores.Core;

namespace VinylShores.Dialogue
{
    /// <summary>
    /// Repeatable, distance-based NPC interaction. Pressing Interact opens the
    /// dialogue when the player is close enough, advances it while open, and
    /// closes it after the last line.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueInteractable : MonoBehaviour
    {
        private const string InteractActionPath = "Player/Interact";

        [Header("Dialogue")]
        [SerializeField] private DialogueLine[] lines = new DialogueLine[0];

        [Header("Interaction")]
        [SerializeField, Min(0f)] private float interactionRadius = 1.5f;
        [SerializeField] private Transform player;
        [SerializeField] private DialogueUI ui;

        [Header("Events")]
        [Tooltip("Invoked every time this NPC's dialogue closes after its last line.")]
        public UnityEvent onDialogueFinished = new UnityEvent();

        private static int lastHandledFrame = -1;

        private InputAction interactAction;
        private IPlayerMovementLock movementLock;
        private bool promptVisible;
        private bool dialogueOwner;

        private void Awake()
        {
            CacheMovementLock();
        }

        private void OnEnable()
        {
            CacheMovementLock();

            if (ui != null)
            {
                ui.Closed += HandleDialogueClosed;
            }
            else
            {
                Debug.LogWarning("DialogueInteractable has no DialogueUI assigned.", this);
            }

            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError(
                    "Project-wide Input Actions are not assigned. " +
                    "Assign InputSystem_Actions in Project Settings > Input System Package.",
                    this);
                return;
            }

            interactAction = actions.FindAction(InteractActionPath, false);
            if (interactAction == null)
            {
                Debug.LogError("Input action '" + InteractActionPath + "' was not found.", this);
                return;
            }

            if (interactAction.actionMap != null)
            {
                interactAction.actionMap.Enable();
            }

            // Started fires on the key press, even though Interact is configured
            // as a Hold interaction. This keeps quick taps working without
            // changing InputSystem_Actions.inputactions.
            interactAction.started += HandleInteractStarted;
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.started -= HandleInteractStarted;
                interactAction = null;
            }

            if (ui != null)
            {
                ui.Closed -= HandleDialogueClosed;
            }

            bool wasDialogueOwner = dialogueOwner;
            dialogueOwner = false;

            if (wasDialogueOwner && ui != null && ui.IsOpen)
            {
                ui.Close();
            }

            if (wasDialogueOwner)
            {
                SetMovementLocked(false);
            }

            promptVisible = false;

            if (ui != null)
            {
                ui.ShowPrompt(false);
            }
        }

        private void Update()
        {
            if (ui == null || player == null)
            {
                return;
            }

            bool shouldShowPrompt = !ui.IsOpen && IsPlayerInRange();
            if (shouldShowPrompt == promptVisible)
            {
                return;
            }

            promptVisible = shouldShowPrompt;
            ui.ShowPrompt(shouldShowPrompt);
        }

        private void HandleInteractStarted(InputAction.CallbackContext context)
        {
            // Several interactables can subscribe to the same action. Consume a
            // press only once per frame so nearby NPCs cannot advance or reopen
            // the dialogue with the same key event.
            if (Time.frameCount == lastHandledFrame)
            {
                return;
            }

            if (ui == null || player == null)
            {
                return;
            }

            if (ui.IsOpen)
            {
                lastHandledFrame = Time.frameCount;
                if (!ui.OpenedThisFrame)
                {
                    ui.Advance();
                }

                return;
            }

            if (!IsPlayerInRange())
            {
                return;
            }

            if (lines == null || lines.Length == 0)
            {
                Debug.LogWarning("DialogueInteractable has no dialogue lines.", this);
                return;
            }

            lastHandledFrame = Time.frameCount;

            CacheMovementLock();
            if (movementLock == null)
            {
                Debug.LogWarning(
                    "Player does not implement IPlayerMovementLock; dialogue will not block movement.",
                    player);
            }

            SetMovementLocked(true);

            dialogueOwner = true;
            promptVisible = false;
            ui.ShowPrompt(false);
            ui.Open(lines);

            if (!ui.IsOpen)
            {
                // Open failed (for example, an empty array supplied at runtime).
                dialogueOwner = false;
                SetMovementLocked(false);
            }
        }

        private void HandleDialogueClosed()
        {
            if (!dialogueOwner)
            {
                return;
            }

            dialogueOwner = false;
            SetMovementLocked(false);

            if (onDialogueFinished != null)
            {
                onDialogueFinished.Invoke();
            }
        }

        private bool IsPlayerInRange()
        {
            if (player == null)
            {
                return false;
            }

            float radius = Mathf.Max(0f, interactionRadius);
            Vector2 delta = (Vector2)player.position - (Vector2)transform.position;
            return delta.sqrMagnitude <= radius * radius;
        }

        private void CacheMovementLock()
        {
            if (player == null)
            {
                movementLock = null;
                return;
            }

            movementLock = player.GetComponentInParent<IPlayerMovementLock>();
            if (movementLock == null)
            {
                movementLock = player.GetComponentInChildren<IPlayerMovementLock>(true);
            }
        }

        private void SetMovementLocked(bool locked)
        {
            if (movementLock == null)
            {
                CacheMovementLock();
            }

            if (movementLock != null)
            {
                movementLock.SetMovementEnabled(!locked);
            }
        }
    }
}
