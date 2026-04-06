using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

[System.Serializable]
public class CutsceneSlide
{
    public Sprite image;
    [TextArea] public string text;
}

// Attach to a Canvas. Assign slides in the Inspector.
// Call Show() to start. Press advance key to move through slides.
[RequireComponent(typeof(CanvasGroup))]
public class CutscenePlayer : MonoBehaviour
{
    [Header("Slides")]
    [SerializeField] private CutsceneSlide[] slides;
    [SerializeField] private Image displayImage;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI proceedLabel;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration  = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Advance")]
    [SerializeField] private Key advanceKey = Key.E;

    [Header("Teleport After (optional)")]
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnPointName;
    [SerializeField] private TeleportPad.SpawnDirection spawnDirection = TeleportPad.SpawnDirection.Down;

    private CanvasGroup canvasGroup;
    private int currentSlide = 0;
    private bool isShowing   = false;
    private bool isAnimating = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

    }

    void Update()
    {
        if (!isShowing || isAnimating) return;

        if (Keyboard.current[advanceKey].wasPressedThisFrame)
        {
            if (currentSlide < slides.Length - 1)
                StartCoroutine(NextSlide());
            else
                StartCoroutine(EndCutscene());
        }
    }

    public void Show()
    {
        if (isShowing) return;
        currentSlide = 0;
        ApplySlide(0, instant: true);
        StartCoroutine(FadeIn());
    }

    private void ApplySlide(int index, bool instant = false)
    {
        if (displayImage != null)  displayImage.sprite = slides[index].image;
        if (dialogueText != null)  dialogueText.text   = slides[index].text;
        if (instant)
        {
            if (displayImage != null) displayImage.color = new Color(1f, 1f, 1f, 0f);
            if (dialogueText != null) dialogueText.color = new Color(1f, 1f, 1f, 0f);
            if (proceedLabel != null) proceedLabel.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private IEnumerator FadeIn()
    {
        isShowing   = true;
        isAnimating = true;
        GameManager.Instance?.SetState(GameManager.GameState.Dialogue);
        canvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f, fadeInDuration);
        yield return FadeImage(0f, 1f, fadeInDuration);

        isAnimating = false;
    }

    private IEnumerator NextSlide()
    {
        isAnimating = true;
        yield return FadeImage(1f, 0f, fadeOutDuration);

        currentSlide++;
        ApplySlide(currentSlide);

        yield return FadeImage(0f, 1f, fadeInDuration);
        isAnimating = false;
    }

    private IEnumerator EndCutscene()
    {
        isAnimating = true;
        isShowing   = false;

        // fade to black
        yield return FadeImage(1f, 0f, fadeOutDuration);

        if (!string.IsNullOrEmpty(targetScene))
        {
            // screen is black — load scene now, canvas dies with this scene
            if (GameManager.Instance != null)
            {
                GameManager.Instance.targetSpawnPointName   = targetSpawnPointName;
                GameManager.Instance.savedApproachDirection = ResolveSpawnDirection();
                GameManager.Instance.pendingTeleportSpawn   = true;
                GameManager.Instance.SetState(GameManager.GameState.Transition);
            }
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            yield return Fade(1f, 0f, fadeOutDuration);
            canvasGroup.blocksRaycasts = false;
            GameManager.Instance?.SetState(GameManager.GameState.Explore);
        }

        isAnimating = false;
    }

    private Vector2 ResolveSpawnDirection()
    {
        switch (spawnDirection)
        {
            case TeleportPad.SpawnDirection.Up:    return Vector2.up;
            case TeleportPad.SpawnDirection.Left:  return Vector2.left;
            case TeleportPad.SpawnDirection.Right: return Vector2.right;
            default:                               return Vector2.down;
        }
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.SmoothStep(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator FadeImage(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.SmoothStep(from, to, elapsed / duration);
            if (displayImage != null) displayImage.color = new Color(1f, 1f, 1f, a);
            if (dialogueText != null) dialogueText.color = new Color(1f, 1f, 1f, a);
            if (proceedLabel != null) proceedLabel.color = new Color(1f, 1f, 1f, a * 0.25f);
            yield return null;
        }
        if (displayImage  != null) displayImage.color  = new Color(1f, 1f, 1f, to);
        if (dialogueText  != null) dialogueText.color  = new Color(1f, 1f, 1f, to);
        if (proceedLabel  != null) proceedLabel.color  = new Color(1f, 1f, 1f, to * 0.25f);
    }
}
