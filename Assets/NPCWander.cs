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
    [SerializeField] private float acceleration = 6f;       // how fast it ramps up
    [SerializeField] private float decelDistance = 0.5f;    // starts slowing this far from target
    [SerializeField] private float directionSmooth = 8f;    // how snappily it turns

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleCheckDistance = 0.4f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float stuckTimeout = 3f;      // abandon target after this many seconds

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 spawnPosition;
    private Vector2 moveDir;
    private Vector2 currentVelocity;
    private bool isWandering = false;
    private bool isTalking = false;

    public void SetTalking(bool talking)
    {
        isTalking = talking;
        if (isTalking) StopMoving();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spawnPosition = rb.position;
        StartCoroutine(WanderLoop());
    }

    void FixedUpdate()
    {
        if (ShouldPause())
        {
            if (isWandering) StopMoving();
            return;
        }

        if (isWandering)
        {
            Vector2 steered = AvoidObstacles(moveDir);
            // Smoothly rotate toward the steered direction
            moveDir = Vector2.Lerp(moveDir, steered, directionSmooth * Time.fixedDeltaTime).normalized;

            // Accelerate smoothly toward target velocity
            Vector2 targetVelocity = moveDir * moveSpeed;
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = currentVelocity;

            // Update animator with smoothed direction
            if (animator != null)
            {
                animator.SetFloat("InputX", moveDir.x);
                animator.SetFloat("InputY", moveDir.y);
            }
        }
    }

    private bool ShouldPause()
    {
        if (isTalking) return true;
        var state = GameManager.Instance?.CurrentState;
        return state == GameManager.GameState.Dialogue || state == GameManager.GameState.Pause;
    }

    private IEnumerator WanderLoop()
    {
        while (true)
        {
            // Idle — wait a randomised duration
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
            while (ShouldPause()) yield return null;

            // Pick a destination — bias toward points further from current pos for purposeful strides
            Vector2 target = PickDestination();
            moveDir = (target - rb.position).normalized;
            isWandering = true;

            if (animator != null)
                animator.SetBool("isWalking", true);

            // Walk until arrived, paused, or stuck
            float walkTimer = 0f;
            while (Vector2.Distance(rb.position, target) > arrivalThreshold)
            {
                if (ShouldPause()) { StopMoving(); yield return null; continue; }

                walkTimer += Time.deltaTime;
                if (walkTimer >= stuckTimeout) break; // give up, pick a new destination next loop

                float dist = Vector2.Distance(rb.position, target);
                if (dist < decelDistance)
                {
                    float speedScale = Mathf.Clamp01(dist / decelDistance);
                    currentVelocity = Vector2.Lerp(currentVelocity, Vector2.zero, (1f - speedScale) * Time.deltaTime * 10f);
                    rb.linearVelocity = currentVelocity;
                }
                else
                {
                    moveDir = (target - rb.position).normalized;
                }

                yield return null;
            }

            StopMoving();
        }
    }

    // Picks a random point, favouring spots further from the NPC's current position
    private Vector2 PickDestination()
    {
        Vector2 best = spawnPosition;
        float bestDist = 0f;
        for (int i = 0; i < 5; i++)
        {
            Vector2 candidate = spawnPosition + Random.insideUnitCircle * wanderRadius;
            float d = Vector2.Distance(rb.position, candidate);
            if (d > bestDist) { bestDist = d; best = candidate; }
        }
        return best;
    }

    private Vector2 AvoidObstacles(Vector2 dir)
    {
        if (Physics2D.Raycast(rb.position, dir, obstacleCheckDistance, obstacleLayer))
        {
            Vector2 left  = Quaternion.Euler(0, 0,  45) * dir;
            Vector2 right = Quaternion.Euler(0, 0, -45) * dir;

            bool leftClear  = !Physics2D.Raycast(rb.position, left,  obstacleCheckDistance, obstacleLayer);
            bool rightClear = !Physics2D.Raycast(rb.position, right, obstacleCheckDistance, obstacleLayer);

            if (leftClear)  return left;
            if (rightClear) return right;
            return -dir;
        }
        return dir;
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
