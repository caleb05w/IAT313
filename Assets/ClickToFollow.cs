using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

// Attach to any GameObject with a Collider2D.
// Clicking it smoothly slides the camera to follow it.
// Press R to return to the player.
[RequireComponent(typeof(Collider2D))]
public class ClickToFollow : MonoBehaviour
{
    [SerializeField] private Key returnKey = Key.R;
    [SerializeField] private float slideDuration = 1f;

    private CinemachineVirtualCamera vcam;
    private Transform playerTransform;
    private Transform currentTarget;
    private Coroutine activeSlide;
    private Transform activeProxy;

    void Start()
    {
        foreach (var v in FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None))
        {
            if (v.Follow != null && v.Follow.CompareTag("Player"))
            {
                vcam = v;
                playerTransform = v.Follow;
                currentTarget = v.Follow;
                break;
            }
        }
    }

    void Update()
    {
        if (vcam == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            foreach (var hit in Physics2D.OverlapPointAll(worldPos))
                if (hit.gameObject == gameObject) { SlideTo(transform); break; }
        }

        if (Keyboard.current[returnKey].wasPressedThisFrame)
            SlideTo(playerTransform);

        // DEBUG
        if (Keyboard.current.kKey.wasPressedThisFrame)
            SlideTo(transform);
    }

    private void SlideTo(Transform target)
    {
        if (target == null || target == currentTarget) return;

        if (activeSlide != null)
            StopCoroutine(activeSlide);

        if (activeProxy != null)
        {
            Destroy(activeProxy.gameObject);
            activeProxy = null;
        }

        activeSlide = StartCoroutine(SlideCoroutine(target));
    }

    private IEnumerator SlideCoroutine(Transform target)
    {
        var proxy = new GameObject("CameraProxy").transform;
        proxy.position = currentTarget != null ? currentTarget.position : target.position;
        activeProxy = proxy;
        vcam.Follow = proxy;

        Vector3 startPos = proxy.position;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            proxy.position = Vector3.Lerp(startPos, target.position, t);
            yield return null;
        }

        vcam.Follow = target;
        currentTarget = target;
        Destroy(proxy.gameObject);
        activeProxy = null;
        activeSlide = null;
    }
}
