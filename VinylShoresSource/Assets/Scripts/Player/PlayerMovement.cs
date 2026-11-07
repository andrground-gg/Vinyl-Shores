using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
/*
Four-direction (no diagonal) top-down movement
Controls: WASD or arrow keys.
If several keys are held, the most recently pressed key wins.
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
    private int lastHorizontal; // -1 = left, +1 = right (last pressed)
    private int lastVertical;   // -1 = down, +1 = up (last pressed)

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

    private void ReadInput(out float x, out float y, out bool hJust, out bool vJust)
    {
        bool left, right, up, down;
        bool leftJust, rightJust, upJust, downJust;

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

        leftJust  = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
        rightJust = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
        upJust    = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
        downJust  = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
#else
        left  = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        up    = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        down  = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        leftJust  = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        rightJust = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        upJust    = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        downJust  = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
#endif

        // Remember the most recently pressed key on each axis.
        if (leftJust)  lastHorizontal = -1;
        if (rightJust) lastHorizontal = 1;
        if (downJust)  lastVertical = -1;
        if (upJust)    lastVertical = 1;

        // Opposite keys held together: the most recently pressed one wins.
        x = ResolveAxis(left, right, lastHorizontal);
        y = ResolveAxis(down, up, lastVertical);

        hJust = leftJust || rightJust;
        vJust = upJust || downJust;
    }

    private static float ResolveAxis(bool negative, bool positive, int lastPressed)
    {
        if (negative && positive) return lastPressed;
        if (positive) return 1f;
        if (negative) return -1f;
        return 0f;
    }

    private void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        animator.SetFloat(MoveXHash, facing.x);
        animator.SetFloat(MoveYHash, facing.y);
        animator.SetBool(IsMovingHash, moveDirection != Vector2.zero);
    }
}