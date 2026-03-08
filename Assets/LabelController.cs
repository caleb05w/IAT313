using UnityEngine;
using TMPro;
using System.Collections;

public class LabelController : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;
    [SerializeField] private string defaultText = "Object";
    [SerializeField] private string interactionPrompt = "Press E to interact";
    // How long the fade in/out takes in seconds
    [SerializeField] private float fadeDuration = 0.3f;

    void Start()
    {
        label.text = defaultText;
        // Start fully transparent
        SetAlpha(0f);
    }

    public void FadeIn(string message = null)
    {
        label.text = message ?? interactionPrompt;
        StopAllCoroutines();
        StartCoroutine(Fade(0f, 1f));
    }

    public void FadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutThenReset());
    }

    private IEnumerator FadeOutThenReset()
    {
        yield return Fade(label.color.a, 0f);
        label.text = defaultText;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        Color c = label.color;
        c.a = alpha;
        label.color = c;
    }

    public void SetColor(Color color)
    {
        label.color = color;
    }
}
