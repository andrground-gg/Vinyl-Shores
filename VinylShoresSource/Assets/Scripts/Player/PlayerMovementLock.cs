using UnityEngine;

namespace VinylShores.Core
{
    /// <summary>
    /// Bridges the dialogue movement lock with the shared player controller.
    /// Toggling <see cref="PlayerMovement"/> off lets its OnDisable stop the
    /// body, so dialogue can freeze the player without changing the controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerMovementLock : MonoBehaviour, IPlayerMovementLock
    {
        [SerializeField] private PlayerMovement movement;

        private bool disabledByLock;

        private void Awake()
        {
            ResolveMovement();
        }

        /// <summary>
        /// Enables or disables the shared movement controller. Only re-enables
        /// it when this component was the one that disabled it.
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            ResolveMovement();
            if (movement == null)
            {
                return;
            }

            if (!enabled && movement.enabled)
            {
                movement.enabled = false;
                disabledByLock = true;
            }
            else if (enabled && disabledByLock)
            {
                movement.enabled = true;
                disabledByLock = false;
            }
        }

        private void ResolveMovement()
        {
            if (movement == null)
            {
                movement = GetComponent<PlayerMovement>();
            }
        }
    }
}
