using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public enum SlideLayout { TextOnly, TextWithImage }

[System.Serializable]
public class WordHighlight
{
    public string word;
    public Color color = new Color(0.91f, 0.66f, 0.22f); // amber default
}

[System.Serializable]
public class SlideSound
{
    public AudioClip clip;
    [Range(0f, 30f), Tooltip("Seconds after the slide appears before this sound plays.")]
    public float offset = 0f;
}

[System.Serializable]
public class CutsceneSlide
{
    public SlideLayout layout;
    public Sprite image;
    [TextArea] public string text;
    public bool rollingText;
    [Tooltip("Words to highlight, each with its own colour.")]
    public WordHighlight[] highlights;
    [Tooltip("Sounds to play on this slide, each with its own time offset.")]
    public SlideSound[] sounds;
}

// Attach to a Canvas. Assign slides in the Inspector.
// Call Show() to start. Press advance key to move through slides.
[RequireComponent(typeof(CanvasGroup))]
public class CutscenePlayer : MonoBehaviour
{
    [Header("Auto Play")]
    [Tooltip("If true, Start Slides play automatically when the scene begins.")]
    [SerializeField] private bool playOnStart = false;

    [Header("Start Slides")]
    [Tooltip("Plays automatically on scene start when Play On Start is enabled.")]
    [SerializeField] private CutsceneSlide[] startSlides;

    [Header("End Slides")]
    [Tooltip("Plays when Show() is called (e.g. wired to an InteractionDetector event).")]
    [SerializeField] private CutsceneSlide[] slides;

    [Header("Layout: Text Only")]
    [SerializeField] private GameObject textOnlyLayout;
    [SerializeField] private TextMeshProUGUI textOnlyDialogue;
    [SerializeField] private TextMeshProUGUI textOnlyProceed;

    [Header("Layout: Text With Image")]
    [SerializeField] private GameObject textWithImageLayout;
    [SerializeField] private Image displayImage;
    [SerializeField] private TextMeshProUGUI textWithImageDialogue;
    [SerializeField] private TextMeshProUGUI textWithImageProceed;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration  = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Advance")]
    [SerializeField] private Key advanceKey = Key.E;
    [SerializeField] private float charDelay = 0.04f;

    [Header("On Complete")]
    [SerializeField] private string flagOnComplete;

    [Header("Teleport After (optional)")]
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnPointName;
    [SerializeField] private TeleportPad.SpawnDirection spawnDirection = TeleportPad.SpawnDirection.Down;

    private CanvasGroup canvasGroup;
    private int currentSlide    = 0;
    private bool inStartPhase   = false;
    private bool isShowing      = false;
    private bool isAnimating    = false;
    private bool isTyping       = false;
    private bool finishedTyping = false;
    private Coroutine typeCoroutine;
    private SlideLayout activeLayout;
    private List<Coroutine> soundCoroutines = new List<Coroutine>();

    // Switches between startSlides and slides depending on which phase is active
    private CutsceneSlide[] ActiveSlides => inStartPhase ? startSlides : slides;

    // Returns the dialogue/proceed components for whichever layout is currently active
    private TextMeshProUGUI DialogueText => activeLayout == SlideLayout.TextOnly ? textOnlyDialogue     : textWithImageDialogue;
    private TextMeshProUGUI ProceedLabel => activeLayout == SlideLayout.TextOnly ? textOnlyProceed      : textWithImageProceed;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        if (textOnlyLayout != null)      textOnlyLayout.SetActive(false);
        if (textWithImageLayout != null) textWithImageLayout.SetActive(false);
    }

    void Start()
    {
        if (playOnStart && startSlides != null && startSlides.Length > 0)
        {
            inStartPhase = true;
            currentSlide = 0;
            ApplySlide(0, instant: true);
            StartCoroutine(FadeIn());
        }
    }

    void Update()
    {
        if (!isShowing || isAnimating) return;

        if (Keyboard.current[advanceKey].wasPressedThisFrame)
        {
            if (isTyping)
            {
                // First press while typing: complete the text, don't advance yet
                if (typeCoroutine != null) StopCoroutine(typeCoroutine);
                isTyping = false;
                finishedTyping = true;
                if (DialogueText != null) DialogueText.text = ApplyHighlights(ActiveSlides[currentSlide]);
                return;
            }

            if (!finishedTyping) return;

            if (currentSlide < ActiveSlides.Length - 1)
                StartCoroutine(NextSlide());
            else
                StartCoroutine(EndCutscene());
        }
    }

    public void Show()
    {
        if (isShowing || slides == null || slides.Length == 0) return;
        inStartPhase = false;
        currentSlide = 0;
        ApplySlide(0, instant: true);
        StartCoroutine(FadeIn());
    }

    private void ApplyLayout(SlideLayout layout)
    {
        activeLayout = layout;
        if (textOnlyLayout != null)      textOnlyLayout.SetActive(layout == SlideLayout.TextOnly);
        if (textWithImageLayout != null) textWithImageLayout.SetActive(layout == SlideLayout.TextWithImage);
    }

    private void ApplySlide(int index, bool instant = false)
    {
        var slide = ActiveSlides[index];

        ApplyLayout(slide.layout);

        if (displayImage != null) displayImage.sprite = slide.image;

        // cancel any pending sounds from the previous slide
        foreach (var c in soundCoroutines)
            if (c != null) StopCoroutine(c);
        soundCoroutines.Clear();

        // schedule each sound with its offset
        if (sfxSource != null && slide.sounds != null)
            foreach (var s in slide.sounds)
                if (s.clip != null)
                    soundCoroutines.Add(StartCoroutine(PlaySoundDelayed(s.clip, s.offset)));

        if (slide.rollingText)
        {
            finishedTyping = false;
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);
            if (DialogueText != null) DialogueText.text = "";
            typeCoroutine = StartCoroutine(TypeText(slide.text, slide));
        }
        else
        {
            finishedTyping = true;
            if (DialogueText != null) DialogueText.text = ApplyHighlights(slide);
        }

        if (instant)
        {
            if (displayImage  != null) displayImage.color  = new Color(1f, 1f, 1f, 0f);
            if (DialogueText  != null) DialogueText.color  = new Color(1f, 1f, 1f, 0f);
            if (ProceedLabel  != null) ProceedLabel.color  = new Color(1f, 1f, 1f, 0f);
        }
    }

    private string ApplyHighlights(CutsceneSlide slide) => ApplyHighlights(slide.text, slide);

    private string ApplyHighlights(string text, CutsceneSlide slide)
    {
        if (slide.highlights == null) return text;
        foreach (var h in slide.highlights)
            if (!string.IsNullOrEmpty(h.word))
                text = text.Replace(h.word, $"<color=#{ColorUtility.ToHtmlStringRGB(h.color)}>{h.word}</color>");
        return text;
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        sfxSource.PlayOneShot(clip);
    }

    private IEnumerator TypeText(string plainText, CutsceneSlide slide)
    {
        isTyping = true;
        for (int i = 0; i <= plainText.Length; i++)
        {
            if (!isTyping) yield break; // skipped externally
            // Apply highlights to each substring — words colour the moment they complete
            if (DialogueText != null)
                DialogueText.text = ApplyHighlights(plainText.Substring(0, i), slide);
            if (i == plainText.Length) break; // full text shown — exit without an extra delay
            yield return new WaitForSeconds(charDelay);
        }
        isTyping = false;
        finishedTyping = true;
    }

    private IEnumerator FadeIn()
    {
        isShowing   = true;
        isAnimating = true;
        GameManager.Instance?.SetState(GameManager.GameState.Dialogue);
        canvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f, fadeInDuration);
        yield return FadeContent(0f, 1f, fadeInDuration);

        isAnimating = false;
    }

    private IEnumerator NextSlide()
    {
        isAnimating = true;
        yield return FadeContent(1f, 0f, fadeOutDuration);

        currentSlide++;
        ApplySlide(currentSlide);

        yield return FadeContent(0f, 1f, fadeInDuration);
        isAnimating = false;
    }

    private IEnumerator EndCutscene()
    {
        isAnimating = true;
        isShowing   = false;

        // fade content to black
        yield return FadeContent(1f, 0f, fadeOutDuration);

        if (!inStartPhase && !string.IsNullOrEmpty(targetScene))
        {
            // screen is black — load scene now, canvas dies with this scene
            if (GameManager.Instance != null)
            {
                GameManager.Instance.targetSpawnPointName   = targetSpawnPointName;
                GameManager.Instance.savedApproachDirection = ResolveSpawnDirection();
                GameManager.Instance.pendingTeleportSpawn   = true;
                GameManager.Instance.SetState(GameManager.GameState.Transition);
            }
            if (!string.IsNullOrEmpty(flagOnComplete))
                GameManager.Instance?.SetFlag(flagOnComplete);
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            // fade canvas back out to reveal the scene
            yield return Fade(1f, 0f, fadeOutDuration);
            canvasGroup.blocksRaycasts = false;
            if (!string.IsNullOrEmpty(flagOnComplete))
                GameManager.Instance?.SetFlag(flagOnComplete);
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

    private IEnumerator FadeContent(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.SmoothStep(from, to, elapsed / duration);
            if (displayImage != null) displayImage.color = new Color(1f, 1f, 1f, a);
            if (DialogueText != null) DialogueText.color = new Color(1f, 1f, 1f, a);
            if (ProceedLabel != null) ProceedLabel.color = new Color(1f, 1f, 1f, a * 0.25f);
            yield return null;
        }
        if (displayImage != null) displayImage.color = new Color(1f, 1f, 1f, to);
        if (DialogueText != null) DialogueText.color = new Color(1f, 1f, 1f, to);
        if (ProceedLabel != null) ProceedLabel.color = new Color(1f, 1f, 1f, to * 0.25f);
    }
}
