using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCWander : MonoBehaviour
{
    [Header("Wander")]
    [SerializeField] private float wanderRadius = 2f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float minWaitTime = 0.5f;
    [SerializeField] private float maxWaitTime = 1.5f;
    [SerializeField] private float arrivalThreshold = 0.15f;

    [Header("Feel")]
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float decelDistance = 0.5f;
    [SerializeField] private float directionSmooth = 8f;

    [Header("Player Awareness")]
    [SerializeField] private float facePlayerRadius = 2f;
    [SerializeField] private float facePlayerSpeed = 3f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleCheckDistance = 0.8f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float stuckTimeout = 2f;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;

    // State
    private Vector2 spawnPosition;
    private Vector2 moveDir;
    private Vector2 currentVelocity;
    private Vector2 facingDir = Vector2.down;
    private Vector2 lastFailedDir;   // direction of last stuck/wall attempt
    private bool isWandering;
    private bool isTalking;
    private Coroutine wanderCoroutine;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void SetTalking(bool talking)
    {
        isTalking = talking;
        if (isTalking) StopMoving();
    }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spawnPosition = rb.position;

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        wanderCoroutine = StartCoroutine(WanderLoop());
    }

    void Update()
    {
        UpdateFacing();
    }

    void FixedUpdate()
    {
        if (ShouldPause())
        {
            if (isWandering) StopMoving();
            return;
        }

        if (isWandering)
            UpdateMovement();
    }

    void OnCollisionEnter2D(Collision2D col) => OnWallContact(col);
    void OnCollisionStay2D(Collision2D col)  => OnWallContact(col);

    // -------------------------------------------------------------------------
    // Facing
    // -------------------------------------------------------------------------

    private void UpdateFacing()
    {
        if (player == null || animator == null || isWandering) return;

        bool playerIsClose = Vector2.Distance(rb.position, player.position) <= facePlayerRadius;
        if (!playerIsClose && !isTalking) return;

        Vector2 toPlayer = ((Vector2)player.position - rb.position).normalized;
        facingDir = Vector2.Lerp(facingDir, toPlayer, facePlayerSpeed * Time.deltaTime);
        animator.SetFloat("LastInputX", facingDir.x);
        animator.SetFloat("LastInputY", facingDir.y);
    }

    // -------------------------------------------------------------------------
    // Movement (FixedUpdate)
    // -------------------------------------------------------------------------

    private void UpdateMovement()
    {
        Vector2 steered = AvoidObstacles(moveDir);

        if (steered == Vector2.zero)
        {
            RestartWander(withPause: true);
            return;
        }

        moveDir = Vector2.Lerp(moveDir, steered, directionSmooth * Time.fixedDeltaTime).normalized;
        currentVelocity = Vector2.MoveTowards(currentVelocity, moveDir * moveSpeed, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = currentVelocity;

        animator?.SetFloat("InputX", moveDir.x);
        animator?.SetFloat("InputY", moveDir.y);
    }

    // -------------------------------------------------------------------------
    // Wander coroutine
    // -------------------------------------------------------------------------

    private IEnumerator WanderLoop()
    {
        while (true)
        {
            // Wait out hard pauses (dialogue, pause menu) or player proximity
            while (ShouldPause() || PlayerIsClose()) yield return null;

            Vector2 target = PickDestination();

            // If no valid destination found, wait and retry rather than walking to self
            if (Vector2.Distance(target, rb.position) < arrivalThreshold)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            moveDir = (target - rb.position).normalized;
            isWandering = true;
            animator?.SetBool("isWalking", true);

            yield return WalkToTarget(target);

            StopMoving();

            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        }
    }

    private IEnumerator WalkToTarget(Vector2 target)
    {
        float timer = 0f;
        while (Vector2.Distance(rb.position, target) > arrivalThreshold)
        {
            if (ShouldPause() || PlayerIsClose()) { StopMoving(); yield return null; continue; }

            timer += Time.deltaTime;  // only count time actually spent trying to move
            if (timer >= stuckTimeout)
            {
                lastFailedDir = moveDir; // remember we got stuck in this direction
                yield break;
            }

            float dist = Vector2.Distance(rb.position, target);
            if (dist < decelDistance)
            {
                float t = 1f - Mathf.Clamp01(dist / decelDistance);
                currentVelocity = Vector2.Lerp(currentVelocity, Vector2.zero, t * Time.deltaTime * 10f);
                rb.linearVelocity = currentVelocity;
            }
            else
            {
                moveDir = (target - rb.position).normalized;
            }

            yield return null;
        }
    }

    // -------------------------------------------------------------------------
    // Wall collision
    // -------------------------------------------------------------------------

    private void OnWallContact(Collision2D col)
    {
        if (!isWandering) return;
        if ((obstacleLayer.value & (1 << col.gameObject.layer)) == 0) return;

        lastFailedDir = moveDir;
        RestartWander(withPause: Random.value < 0.5f);
    }

    private void RestartWander(bool withPause)
    {
        if (wanderCoroutine != null) StopCoroutine(wanderCoroutine);
        wanderCoroutine = StartCoroutine(WallHitReaction(withPause));
    }

    private IEnumerator WallHitReaction(bool pause)
    {
        StopMoving();
        if (pause) yield return new WaitForSeconds(1f);
        wanderCoroutine = StartCoroutine(WanderLoop());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private bool ShouldPause()
    {
        if (isTalking) return true;
        var state = GameManager.Instance?.CurrentState;
        return state == GameManager.GameState.Dialogue || state == GameManager.GameState.Pause;
    }

    private bool PlayerIsClose() =>
        player != null && Vector2.Distance(rb.position, player.position) <= facePlayerRadius;

    private Vector2 PickDestination()
    {
        Vector2 best = rb.position;
        float bestScore = -1f;
        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate = spawnPosition + Random.insideUnitCircle * wanderRadius;
            Vector2 toCandidate = candidate - rb.position;
            float d = toCandidate.magnitude;
            if (Physics2D.Raycast(rb.position, toCandidate.normalized, d, obstacleLayer)) continue;

            // Penalise candidates that point in the same direction as the last failure.
            // dot = 1 means same direction, -1 means opposite — we want low dot scores.
            float similarity = lastFailedDir != Vector2.zero
                ? Vector2.Dot(toCandidate.normalized, lastFailedDir.normalized)
                : 0f;
            float score = d * (1f - Mathf.Max(0f, similarity));

            if (score > bestScore) { bestScore = score; best = candidate; }
        }
        return best;
    }

    private Vector2 AvoidObstacles(Vector2 dir)
    {
        if (!Physics2D.Raycast(rb.position, dir, obstacleCheckDistance, obstacleLayer))
            return dir;

        int[] angles = { 45, -45, 90, -90, 135, -135 };
        foreach (int angle in angles)
        {
            Vector2 candidate = Quaternion.Euler(0, 0, angle) * dir;
            if (!Physics2D.Raycast(rb.position, candidate, obstacleCheckDistance, obstacleLayer))
                return candidate;
        }

        return Vector2.zero;
    }

    private void StopMoving()
    {
        isWandering = false;
        currentVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetFloat("LastInputX", moveDir.x);
            animator.SetFloat("LastInputY", moveDir.y);
        }
    }
}
