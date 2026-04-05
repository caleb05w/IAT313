using UnityEngine;
using TMPro;

public class LabelController : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;
    [SerializeField] private string defaultText = "!";

    void Start()
    {
        label.text = defaultText;
    }

    public void ShowMessage(string message)
    {
        label.text = string.IsNullOrEmpty(message) ? defaultText : message;
    }

    public void SetDefaultText(string text)
    {
        defaultText = text;
        label.text = defaultText;
    }

    public void FadeOut()
    {
        label.text = defaultText;
    }
}
