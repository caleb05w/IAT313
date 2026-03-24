using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Singleton that provides a full-screen black fade overlay.
// No manual setup required — it creates its own Canvas and Image on first use.
public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance { get; private set; }

    private Image overlay;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        SetAlpha(0f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(Fade(1f, 0f, 0.3f));
    }

    private void BuildOverlay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var img = new GameObject("FadeImage");
        img.transform.SetParent(transform, false);

        var rect = img.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay = img.AddComponent<Image>();
        overlay.color = Color.black;
        overlay.raycastTarget = false;
    }

    private void SetAlpha(float a)
    {
        var c = overlay.color;
        c.a = a;
        overlay.color = c;
    }

    // Fade to black, run action, then fade back. Total hold time = holdSeconds.
    public IEnumerator FadeOutAndIn(float fadeDuration, float holdSeconds, System.Action action)
    {
        // Fade to black
        yield return Fade(0f, 1f, fadeDuration);

        action?.Invoke();

        yield return new WaitForSeconds(holdSeconds);

        // Fade back
        yield return Fade(1f, 0f, fadeDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    // Convenience: get or create the singleton in the scene
    public static ScreenFade Get()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("ScreenFade");
        return go.AddComponent<ScreenFade>();
    }
}
