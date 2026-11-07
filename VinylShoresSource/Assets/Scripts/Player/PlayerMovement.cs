using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
/*
Four-direction (no diagonal) top-down movement
Controls: WASD or arrow keys.
If two axes are held at once, the most recently pressed axis wins.
Collision is handled by the 2D physics engine (Dynamic Rigidbody2D + Collider2D)*/

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Speed in world units per second (with 16 PPU, 1 unit = 1 tile).")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Optional animation")]
    [Tooltip("Optional. If assigned, the Animator needs float params MoveX, MoveY and bool IsMoving.")]
    [SerializeField] private Animator animator;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private Vector2 facing = Vector2.down;
    private bool horizontalPressedLast;

    /// <summary>Last non-zero direction. Useful later for interacting with NPCs in front of the player.</summary>
    public Vector2 Facing => facing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Top-down setup: enforced in code so teammates can't break it in the Inspector by accident.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        ReadInput(out float x, out float y, out bool horizontalJustPressed, out bool verticalJustPressed);

        if (horizontalJustPressed) horizontalPressedLast = true;
        if (verticalJustPressed) horizontalPressedLast = false;

        if (x != 0f && y != 0f)
            moveDirection = horizontalPressedLast ? new Vector2(x, 0f) : new Vector2(0f, y);
        else
            moveDirection = new Vector2(x, y);

        if (moveDirection != Vector2.zero)
            facing = moveDirection;

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Setting velocity every physics step: stops instantly on release, never drifts,
        // and the physics engine blocks movement into colliders.
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    private void OnDisable()
    {
        // e.g. when a dialogue or rhythm challenge disables movement
        moveDirection = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        UpdateAnimator();
    }

    private static void ReadInput(out float x, out float y, out bool hJust, out bool vJust)
    {
        bool left, right, up, down;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            x = y = 0f;
            hJust = vJust = false;
            return;
        }

        left  = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
        right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
        up    = kb.wKey.isPressed || kb.upArrowKey.isPressed;
        down  = kb.sKey.isPressed || kb.downArrowKey.isPressed;

        hJust = kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
                kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
        vJust = kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame ||
                kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
#else
        left  = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        up    = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        down  = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        hJust = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
        vJust = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);
#endif

        // Opposite keys cancel each other out.
        x = (right ? 1f : 0f) - (left ? 1f : 0f);
        y = (up ? 1f : 0f) - (down ? 1f : 0f);
    }

    private void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        animator.SetFloat(MoveXHash, facing.x);
        animator.SetFloat(MoveYHash, facing.y);
        animator.SetBool(IsMovingHash, moveDirection != Vector2.zero);
    }
}