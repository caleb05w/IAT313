using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraPan : MonoBehaviour
{
    [System.Serializable]
    public class FlagPan
    {
        public string flag;
        public Transform target;
        [Tooltip("If true, this pan only triggers once and is ignored on subsequent flag sets.")]
        public bool oneShot;
        [HideInInspector] public bool hasFired;
    }

    [SerializeField] private List<FlagPan> flagPans = new List<FlagPan>();
    [SerializeField] private float slideDuration = 1f;

    private CinemachineVirtualCamera vcam;
    private Transform playerTransform;
    private Transform currentTarget;
    private Coroutine activeSlide;
    private Queue<Transform> panQueue = new Queue<Transform>();

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

        if (GameManager.Instance != null)
            GameManager.Instance.OnFlagSet += OnFlagSet;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFlagSet -= OnFlagSet;
    }

    private void OnFlagSet(string flag)
    {
        foreach (var entry in flagPans)
        {
            if (entry.flag != flag) continue;
            if (entry.oneShot && entry.hasFired) continue;
            entry.hasFired = true;
            EnqueuePan(entry.target);
        }
    }

    public void PanToPlayer()
    {
        EnqueuePan(playerTransform);
    }

    private void EnqueuePan(Transform dest)
    {
        if (vcam == null || dest == null) return;

        if (activeSlide != null)
        {
            panQueue.Enqueue(dest);
        }
        else
        {
            activeSlide = StartCoroutine(SlideCoroutine(dest));
        }
    }

    private IEnumerator SlideCoroutine(Transform dest)
    {
        if (dest != currentTarget)
        {
            GameManager.Instance?.SetState(GameManager.GameState.Dialogue);

            var proxy = new GameObject("CameraProxy").transform;
            proxy.position = currentTarget != null ? currentTarget.position : dest.position;
            vcam.Follow = proxy;

            Vector3 startPos = proxy.position;
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                proxy.position = Vector3.Lerp(startPos, dest.position, Mathf.SmoothStep(0f, 1f, elapsed / slideDuration));
                yield return null;
            }

            vcam.Follow = dest;
            currentTarget = dest;
            Destroy(proxy.gameObject);
        }

        activeSlide = null;

        if (panQueue.Count > 0)
        {
            activeSlide = StartCoroutine(SlideCoroutine(panQueue.Dequeue()));
        }
        else
        {
            GameManager.Instance?.SetState(GameManager.GameState.Explore);
        }
    }
}
