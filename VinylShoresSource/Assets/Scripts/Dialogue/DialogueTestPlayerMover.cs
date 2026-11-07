using UnityEngine;
using UnityEngine.InputSystem;
using VinylShores.Core;

namespace VinylShores.Dialogue
{
    /// <summary>
    /// Temporary top-down mover for the dialogue test scene. It will be
    /// replaced by the team's real player controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueTestPlayerMover : MonoBehaviour, IPlayerMovementLock
    {
        private const string MoveActionPath = "Player/Move";

        [SerializeField, Min(0f)] private float moveSpeed = 5f;

        private InputAction moveAction;
        private Rigidbody2D body;
        private Vector2 moveInput;
        private bool movementEnabled = true;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError(
                    "Project-wide Input Actions are not assigned. " +
                    "Assign InputSystem_Actions in Project Settings > Input System Package.",
                    this);
                return;
            }

            moveAction = actions.FindAction(MoveActionPath, false);
            if (moveAction == null)
            {
                Debug.LogError("Input action '" + MoveActionPath + "' was not found.", this);
                return;
            }

            if (moveAction.actionMap != null)
            {
                moveAction.actionMap.Enable();
            }
        }

        private void OnDisable()
        {
            moveAction = null;
        }

        private void Update()
        {
            if (!movementEnabled || moveAction == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            Vector2 input = moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            moveInput = input;

            // Fallback for scenes without a Rigidbody2D, such as older test
            // scenes that still move the Transform directly.
            if (body == null)
            {
                transform.position += (Vector3)(moveInput * (moveSpeed * Time.deltaTime));
            }
        }

        private void FixedUpdate()
        {
            if (!movementEnabled || body == null)
            {
                return;
            }

            body.MovePosition(body.position + moveInput * (moveSpeed * Time.fixedDeltaTime));
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;

            if (!enabled)
            {
                moveInput = Vector2.zero;
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }
            }
        }
    }
}
