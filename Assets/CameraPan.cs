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
        [Tooltip("Flag to set after this pan (and optional dialogue) fully completes.")]
        public string flagOnComplete;
        [Tooltip("Optional dialogue to show after the pan. State stays locked until the player dismisses it.")]
        public DialogueLine[] dialogueLines;
        [Tooltip("If true, this pan only triggers once.")]
        public bool oneShot;
        [HideInInspector] public bool hasFired;
    }

    private struct PanRequest
    {
        public Transform target;
        public string flagOnComplete;
        public DialogueLine[] dialogueLines;
    }

    [SerializeField] private List<FlagPan> flagPans = new List<FlagPan>();
    [SerializeField] private float slideDuration = 1f;

    private CinemachineVirtualCamera vcam;
    private Transform playerTransform;
    private Transform currentTarget;
    private Coroutine activeSlide;
    private Queue<PanRequest> panQueue = new Queue<PanRequest>();

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
            EnqueuePan(entry.target, entry.flagOnComplete, entry.dialogueLines);
        }
    }

    public void PanToPlayer()
    {
        EnqueuePan(playerTransform);
    }

    private void EnqueuePan(Transform dest, string flagOnComplete = "", DialogueLine[] dialogueLines = null)
    {
        if (vcam == null || dest == null) return;

        var request = new PanRequest { target = dest, flagOnComplete = flagOnComplete, dialogueLines = dialogueLines };

        if (activeSlide != null)
            panQueue.Enqueue(request);
        else
            activeSlide = StartCoroutine(SlideCoroutine(request));
    }

    private IEnumerator SlideCoroutine(PanRequest request)
    {
        bool didPan = request.target != currentTarget;
        bool hasDialogue = request.dialogueLines != null && request.dialogueLines.Length > 0;
        bool lockState = didPan || hasDialogue;

        if (lockState)
            GameManager.Instance?.SetState(GameManager.GameState.Dialogue);

        if (didPan)
        {
            var proxy = new GameObject("CameraProxy").transform;
            proxy.position = currentTarget != null ? currentTarget.position : request.target.position;
            vcam.Follow = proxy;

            Vector3 startPos = proxy.position;
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                proxy.position = Vector3.Lerp(startPos, request.target.position, Mathf.SmoothStep(0f, 1f, elapsed / slideDuration));
                yield return null;
            }

            vcam.Follow = request.target;
            currentTarget = request.target;
            Destroy(proxy.gameObject);
        }

        if (hasDialogue && DialogueManager.Instance != null)
        {
            var d = new Dialogue();
            d.showDialogue = true;
            d.lines = request.dialogueLines;
            DialogueManager.Instance.StartDialogue(d);
            yield return new WaitUntil(() => !(DialogueManager.Instance?.IsOpen() ?? false));
        }

        if (lockState)
            GameManager.Instance?.SetState(GameManager.GameState.Explore);

        if (!string.IsNullOrEmpty(request.flagOnComplete))
            GameManager.Instance?.SetFlag(request.flagOnComplete);

        activeSlide = null;

        if (panQueue.Count > 0)
            activeSlide = StartCoroutine(SlideCoroutine(panQueue.Dequeue()));
    }
}
