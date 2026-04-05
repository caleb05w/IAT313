using UnityEngine;
using UnityEngine.InputSystem;

// Handles player movement and animation.
// Attach to the Player GameObject alongside a Rigidbody2D and Animator.
// Wire the "Move" method to the Move input action in the Player Input component.
public class playerMovement : MonoBehaviour
{
    // Movement speed in units per second — adjust in the Inspector
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private InputAction moveAction;

    // The last direction the player was facing — used by InteractionDetector and other systems
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    // Toggle to draw a yellow arrow in the Scene view showing facing direction
    [SerializeField] private bool showFacingArrow = false;

    // Unsubscribe when disabled — null check guards against scene unload order
    void OnDisable() { if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged; }

    // Called whenever GameManager changes state
    void OnStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Explore && state != GameManager.GameState.Combat)
        {
            // Stop all movement when leaving Explore/Combat (dialogue, teleport, pause, etc.)
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
            animator.SetFloat("LastInputX", FacingDirection.x);
            animator.SetFloat("LastInputY", FacingDirection.y);
        }
        else if (moveAction != null)
        {
            // Re-read held input so the player doesn't need to re-press after state restores
            moveInput = moveAction.ReadValue<Vector2>();
            if (moveInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = moveInput.normalized;
                animator.SetBool("isWalking", true);
                animator.SetFloat("InputX", moveInput.x);
                animator.SetFloat("InputY", moveInput.y);
            }
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            moveAction = playerInput.actions["Move"];
    }

    // Subscribe in Start — guarantees GameManager.Instance exists
    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    private bool CanMove()
    {
        if (GameManager.Instance == null) return true;
        return GameManager.Instance.IsState(GameManager.GameState.Explore) ||
               GameManager.Instance.IsState(GameManager.GameState.Combat);
    }

    // Apply velocity each physics step — enforces zero velocity when not in a movement state
    void FixedUpdate()
    {
        rb.linearVelocity = CanMove() ? moveInput * moveSpeed : Vector2.zero;
    }

    // Draws a yellow arrow in the Scene view pointing in the facing direction
    void OnDrawGizmos()
    {
        if (!showFacingArrow) return;
        Gizmos.color = Color.yellow;
        Vector3 dir = (Vector3)FacingDirection * 0.6f;
        Gizmos.DrawLine(transform.position, transform.position + dir);
        Gizmos.DrawSphere(transform.position + dir, 0.1f);
    }

    // Called by the Input System when the Move action fires
    // Wire this in the Player Input component under "Move" → "Move"
    public void Move(InputAction.CallbackContext context)
    {
        if (!CanMove()) return;

        // Input released — stop walking and freeze the animator on the last direction
        if (context.canceled)
        {
            animator.SetBool("isWalking", false);
            // LastInputX/Y hold the final direction so the idle animation faces the right way
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
            moveInput = Vector2.zero;
            return;
        }

        // Input active — update movement and animator
        moveInput = context.ReadValue<Vector2>();
        FacingDirection = moveInput.normalized;
        animator.SetBool("isWalking", true);
        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);
    }
}
