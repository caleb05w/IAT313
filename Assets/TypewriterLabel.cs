using System.Collections;
using UnityEngine;

// Attach alongside a LabelController.
// Waits for a GameManager flag, then types out a message character by character.
[RequireComponent(typeof(LabelController))]
public class TypewriterLabel : MonoBehaviour
{
    [SerializeField] private string triggerFlag = "discoverBody";
    [SerializeField] private string message = "Phone is ringing...";
    [SerializeField] private float charDelay = 0.05f;

    private LabelController label;
    private bool triggered = false;

    void Awake()
    {
        label = GetComponent<LabelController>();
    }

    void OnEnable()
    {
        // Debug.Log($"[TypewriterLabel] OnEnable. HasFlag={GameManager.Instance?.HasFlag(triggerFlag)}");
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.HasFlag(triggerFlag))
            Trigger();
        else
            GameManager.Instance.OnFlagSet += OnFlagSet;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFlagSet -= OnFlagSet;
    }

    private void OnFlagSet(string flag)
    {
        if (flag == triggerFlag) Trigger();
    }

    public void Trigger()
    {
        // Debug.Log($"[TypewriterLabel] Trigger called. triggered={triggered}, flag={triggerFlag}");
        if (triggered) return;
        triggered = true;

        if (GameManager.Instance != null)
            GameManager.Instance.OnFlagSet -= OnFlagSet;

        StartCoroutine(TypeOut());
    }

    private IEnumerator TypeOut()
    {
        yield return null;
        // Debug.Log("[TypewriterLabel] TypeOut running, writing: " + message);
        string current = "";
        foreach (char c in message)
        {
            current += c;
            label.ShowMessage(current);
            // Debug.Log("[TypewriterLabel] text: " + current);
            yield return new WaitForSeconds(charDelay);
        }
    }
}
