using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour
{
    //this is where the variables go
    //serialize field allows this field, despite being private, to appear in the unity interface.
   [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    // Tracks the last direction the player was facing
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    [SerializeField] private bool showFacingArrow = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //allows us to reference components.
        rb = GetComponent<Rigidbody2D>();
        //grab animator from char
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = moveInput * moveSpeed;
        // Debug.Log("velocity: " + rb.linearVelocity + " | moveInput: " + moveInput);
    }

    void OnDrawGizmos()
    {
        if (!showFacingArrow) return;
        Gizmos.color = Color.yellow;
        Vector3 dir = (Vector3)FacingDirection * 0.6f;
        Gizmos.DrawLine(transform.position, transform.position + dir);
        Gizmos.DrawSphere(transform.position + dir, 0.1f);
    }

    public void Move(InputAction.CallbackContext context) {
        // Debug.Log("Move called | phase: " + context.phase + " | value: " + context.ReadValue<Vector2>());

        //triggers when button or keyboard is lifted
        if (context.canceled) {
            animator.SetBool("isWalking", false);
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>();
        FacingDirection = moveInput.normalized;
        animator.SetBool("isWalking", true);
        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);
    }
}
