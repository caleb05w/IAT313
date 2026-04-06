using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Displays inventory items above the player's head.
// 1–3 items → all shown at fixed positions, selector moves between them.
// 4+ items  → left + center + right, smooth slide with wrap.
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private Image     slotPrefab;
    [SerializeField] private Key       toggleKey       = Key.Tab;
    [SerializeField] private float     slotSpacing     = 0.1f;
    [SerializeField] private float     animDuration    = 0.2f;
    [SerializeField] private float     openDuration    = 0.15f;
    [SerializeField] private Vector3   headOffset      = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Color     bgColor         = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Vector2   bgPadding       = new Vector2(0.15f, 0.1f);
    [SerializeField] private float     nameFontSize       = 0.1f;
    [SerializeField] private float     referenceOrthoSize = 5f;

    private float CameraScale => Camera.main != null ? Camera.main.orthographicSize / referenceOrthoSize : 1f;

    private Image leftSlot, centerSlot, rightSlot;
    private Image background;
    private TextMeshProUGUI nameLabel;
    private float slotStep, slotW, slotH;

    private Transform playerTransform;
    private bool isOpen      = false;
    private bool isAnimating = false;
    private Coroutine openCoroutine;
    private Coroutine autoCloseCoroutine;

    // -------------------------------------------------------------------------
    void Awake()
    {
        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) Destroy(hlg);
        var csf = GetComponent<ContentSizeFitter>();
        if (csf != null) Destroy(csf);
    }

    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        var pr = slotPrefab.GetComponent<RectTransform>();
        slotW    = pr.sizeDelta.x;
        slotH    = pr.sizeDelta.y;
        slotStep = slotW + slotSpacing;

        // Create background panel (sits behind all slots)
        var bgGO = new GameObject("InventoryBG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGO.transform.SetParent(transform, false);
        background          = bgGO.GetComponent<Image>();
        background.material = null;
        background.color    = bgColor;
        background.gameObject.SetActive(false);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax        = new Vector2(0.5f, 0.5f);
        bgRect.pivot            = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgGO.transform.SetAsFirstSibling(); // always behind slots

        // Create item name label (sits below slots)
        var labelGO = new GameObject("ItemNameLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(transform, false);
        nameLabel = labelGO.GetComponent<TextMeshProUGUI>();
        nameLabel.alignment  = TextAlignmentOptions.Center;
        nameLabel.fontSize   = nameFontSize;
        nameLabel.color      = Color.white;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin        = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax        = new Vector2(0.5f, 0.5f);
        labelRect.pivot            = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta        = new Vector2(0f, nameFontSize * 1.5f);
        labelRect.anchoredPosition = new Vector2(0f, -(slotH * 0.5f + bgPadding.y + nameFontSize * 0.5f));
        nameLabel.gameObject.SetActive(false);

        inventory.OnChanged += OnInventoryChanged;
        inventory.OnItemConsumed += OnItemConsumed;
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnChanged -= OnInventoryChanged;
            inventory.OnItemConsumed -= OnItemConsumed;
        }
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (openCoroutine != null) StopCoroutine(openCoroutine);
        openCoroutine = StartCoroutine(AnimateToggle(true));
        ScheduleAutoClose(2f);
    }

    void ScheduleAutoClose(float delay)
    {
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = StartCoroutine(AutoClose(delay));
    }

    IEnumerator AutoClose(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isOpen) yield break;
        isOpen = false;
        if (openCoroutine != null) StopCoroutine(openCoroutine);
        openCoroutine = StartCoroutine(AnimateToggle(false));
        autoCloseCoroutine = null;
    }

    // -------------------------------------------------------------------------
    void LateUpdate()
    {
        if (Keyboard.current[toggleKey].wasPressedThisFrame &&
            (GameManager.Instance == null || GameManager.Instance.IsState(GameManager.GameState.Explore)))
        {
            if (autoCloseCoroutine != null) { StopCoroutine(autoCloseCoroutine); autoCloseCoroutine = null; }
            isOpen = !isOpen;
            if (openCoroutine != null) StopCoroutine(openCoroutine);
            openCoroutine = StartCoroutine(AnimateToggle(isOpen));
        }

        if (playerTransform != null && transform.localScale.x > 0f)
            transform.position = playerTransform.position + headOffset;

        if (isOpen && !isAnimating && openCoroutine == null)
            transform.localScale = Vector3.one * CameraScale;
    }

    // -------------------------------------------------------------------------
    IEnumerator AnimateToggle(bool opening)
    {
        if (opening)
        {
            Rebuild();
            float cs = CameraScale;
            transform.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / openDuration));
                float s = t < 0.8f ? Mathf.Lerp(0f, cs * 1.1f, t / 0.8f) : Mathf.Lerp(cs * 1.1f, cs, (t - 0.8f) / 0.2f);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one * cs;
        }
        else
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / openDuration));
                float s = Mathf.Lerp(startScale.x, 0f, t);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one; // reset for next open
            DestroySlots();
            UpdateBackground(0);
            if (nameLabel != null) nameLabel.gameObject.SetActive(false);
        }
        openCoroutine = null;
    }

    // -------------------------------------------------------------------------
    void OnItemConsumed(ItemData item)
    {
        Open();
        Image slot = GetSlotForItem(item);
        StartCoroutine(AnimateConsume(slot, item));
    }

    Image GetSlotForItem(ItemData item)
    {
        int idx   = inventory.IndexOf(item);
        int sel   = inventory.SelectedIndex;
        int count = inventory.Count;

        if (count == 1) return centerSlot;
        if (count == 2) return idx == 0 ? leftSlot : rightSlot;
        if (count == 3)
        {
            if (idx == 0) return leftSlot;
            if (idx == 1) return centerSlot;
            return rightSlot;
        }
        // 4+ items
        if (idx == sel)                         return centerSlot;
        if (idx == (sel + 1) % count)           return rightSlot;
        if (idx == (sel - 1 + count) % count)   return leftSlot;
        return null;
    }

    IEnumerator AnimateConsume(Image slot, ItemData item)
    {
        // Wait for open animation to finish so slots exist
        while (openCoroutine != null) yield return null;

        // Re-resolve slot in case Rebuild ran during open
        slot = GetSlotForItem(item);

        isAnimating = true;

        if (slot != null)
        {
            var rt = slot.GetComponent<RectTransform>();
            Vector3 startScale = rt.localScale;
            Color startColor   = slot.color;
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rt.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                slot.color    = new Color(1f, 1f, 1f, Mathf.Lerp(startColor.a, 0f, t));
                yield return null;
            }
        }

        inventory.RemoveItem(item);
        isAnimating = false;
    }

    // -------------------------------------------------------------------------
    void OnInventoryChanged()
    {
        if (!isOpen || isAnimating) return;

        if (inventory.Count <= 3)                              { Rebuild(); return; }
        if (Keyboard.current.rightArrowKey.isPressed)          StartCoroutine(Slide(-1));
        else if (Keyboard.current.leftArrowKey.isPressed)      StartCoroutine(Slide(+1));
        else                                                   Rebuild();
    }

    // -------------------------------------------------------------------------
    void Rebuild()
    {
        DestroySlots();
        if (!isOpen) { UpdateBackground(0); UpdateNameLabel(); return; }
        if (inventory.Count == 0) { UpdateNameLabel(); UpdateBackgroundForEmpty(); return; }

        int sel   = inventory.SelectedIndex;
        int count = inventory.Count;

        if (count == 1)
        {
            centerSlot = MakeSlot(0, Vector2.zero, 1f);
        }
        else if (count == 2)
        {
            leftSlot  = MakeSlot(0, new Vector2(-slotStep * 0.5f, 0f), sel == 0 ? 1f : 0.2f);
            rightSlot = MakeSlot(1, new Vector2( slotStep * 0.5f, 0f), sel == 1 ? 1f : 0.2f);
        }
        else if (count == 3)
        {
            leftSlot   = MakeSlot(0, new Vector2(-slotStep, 0f), sel == 0 ? 1f : 0.2f);
            centerSlot = MakeSlot(1, Vector2.zero,               sel == 1 ? 1f : 0.2f);
            rightSlot  = MakeSlot(2, new Vector2( slotStep, 0f), sel == 2 ? 1f : 0.2f);
        }
        else // 4+ items: selected in center with neighbours
        {
            centerSlot = MakeSlot(sel,                          Vector2.zero,            1f);
            rightSlot  = MakeSlot((sel + 1) % count,           new Vector2( slotStep, 0f), 0.2f);
            leftSlot   = MakeSlot((sel - 1 + count) % count,   new Vector2(-slotStep, 0f), 0.2f);
        }

        UpdateNameLabel();
        UpdateBackground(count);
    }

    void UpdateNameLabel()
    {
        if (nameLabel == null) return;

        if (isOpen && inventory.Count == 0)
        {
            nameLabel.text = "Inventory empty";
            float textW = nameLabel.GetPreferredValues(nameLabel.text, float.MaxValue, float.MaxValue).x;
            var labelRect = nameLabel.GetComponent<RectTransform>();
            labelRect.sizeDelta        = new Vector2(textW, nameFontSize * 1.5f);
            labelRect.anchoredPosition = Vector2.zero;
            nameLabel.gameObject.SetActive(true);
            return;
        }

        // restore normal position below slots
        nameLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -(slotH * 0.5f + bgPadding.y + nameFontSize * 0.5f));

        var selected = inventory.GetSelected();
        if (isOpen && selected != null)
        {
            nameLabel.text = selected.itemName;
            float textW = nameLabel.GetPreferredValues(nameLabel.text, float.MaxValue, float.MaxValue).x;
            nameLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(textW, nameFontSize * 1.5f);
            nameLabel.gameObject.SetActive(true);
        }
        else
        {
            nameLabel.gameObject.SetActive(false);
        }
    }

    void UpdateBackgroundForEmpty()
    {
        if (background == null) return;
        float textW = nameLabel != null && nameLabel.gameObject.activeSelf
            ? nameLabel.GetComponent<RectTransform>().sizeDelta.x + bgPadding.x * 2f
            : slotW + bgPadding.x * 2f;
        float h = nameFontSize * 1.5f + bgPadding.y * 2f;
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.sizeDelta        = new Vector2(textW, h);
        bgRect.anchoredPosition = Vector2.zero;
        background.gameObject.SetActive(true);
        background.transform.SetAsFirstSibling();
    }

    void UpdateBackground(int visibleSlots)
    {
        if (background == null) return;
        if (visibleSlots == 0) { background.gameObject.SetActive(false); background.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; return; }

        int slotCount = Mathf.Min(visibleSlots, 3);
        float slotsW  = slotW * slotCount + slotSpacing * (slotCount - 1) + bgPadding.x * 2f;
        float textW   = nameLabel != null && nameLabel.gameObject.activeSelf
                        ? nameLabel.GetComponent<RectTransform>().sizeDelta.x + bgPadding.x * 2f
                        : 0f;
        float w = Mathf.Max(slotsW, textW);
        float labelRowH = nameFontSize + bgPadding.y * 1.5f;
        float h = slotH + bgPadding.y * 2f + labelRowH;
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.sizeDelta        = new Vector2(w, h);
        bgRect.anchoredPosition = new Vector2(0f, -labelRowH * 0.5f);
        background.gameObject.SetActive(true);
        background.transform.SetAsFirstSibling();
    }

    // -------------------------------------------------------------------------
    // direction: -1 = slide left (right arrow pressed), +1 = slide right (left arrow pressed)
    IEnumerator Slide(int direction)
    {
        isAnimating = true;

        int sel   = inventory.SelectedIndex;
        int count = inventory.Count;

        // --- Determine outgoing and incoming ---
        Image   outgoing;
        Vector2 outgoingEnd, incomingStart;
        float   incomingTargetAlpha;

        float offScreen     = slotStep * 2f;
        incomingTargetAlpha = 0.2f;

        if (direction < 0) // slide left
        {
            outgoingEnd   = new Vector2(-offScreen, 0f);
            incomingStart = new Vector2( offScreen, 0f);
            outgoing      = leftSlot;
        }
        else               // slide right
        {
            outgoingEnd   = new Vector2( offScreen, 0f);
            incomingStart = new Vector2(-offScreen, 0f);
            outgoing      = rightSlot;
        }

        int incomingIdx = direction < 0
            ? (sel + 1) % count
            : (sel - 1 + count) % count;

        Image incoming = MakeSlot(incomingIdx, incomingStart, 0f); // start transparent

        // --- Which slot fades to full (new center) ---
        Image newCenter = direction < 0
            ? rightSlot   // right becomes center
            : leftSlot;   // left becomes center

        // --- Center always leaves its position (fades to dim) ---
        Image leavingCenter = centerSlot;

        // Cache RTs and starting positions
        var outRT = outgoing?.GetComponent<RectTransform>();
        var incRT = incoming.GetComponent<RectTransform>();
        var lefRT = leftSlot?.GetComponent<RectTransform>();
        var cenRT = centerSlot?.GetComponent<RectTransform>();
        var rigRT = rightSlot?.GetComponent<RectTransform>();

        Vector2 outStart = outRT != null ? outRT.anchoredPosition : outgoingEnd;
        Vector2 lefStart = lefRT != null ? lefRT.anchoredPosition : new Vector2(-slotStep, 0f);
        Vector2 cenStart = cenRT != null ? cenRT.anchoredPosition : Vector2.zero;
        Vector2 rigStart = rigRT != null ? rigRT.anchoredPosition : new Vector2(slotStep,  0f);
        Vector2 shift = new Vector2(direction * slotStep, 0f);

        float leavingStartAlpha = leavingCenter != null ? leavingCenter.color.a : 1f;

        // --- Animate ---
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));

            // outRT handles the outgoing slot; skip it in the shift group to avoid RT conflict
            if (outRT != null)                     outRT.anchoredPosition = Vector2.Lerp(outStart, outgoingEnd, t);
            if (lefRT != null && lefRT != outRT)   lefRT.anchoredPosition = Vector2.Lerp(lefStart, lefStart + shift, t);
            if (cenRT != null && cenRT != outRT)   cenRT.anchoredPosition = Vector2.Lerp(cenStart, cenStart + shift, t);
            if (rigRT != null && rigRT != outRT)   rigRT.anchoredPosition = Vector2.Lerp(rigStart, rigStart + shift, t);
                                                   incRT.anchoredPosition = Vector2.Lerp(incomingStart, incomingStart + shift, t);

            if (outgoing      != null) outgoing.color      = new Color(1f,1f,1f, Mathf.Lerp(outgoing.color.a, 0f,                   t));
                                       incoming.color      = new Color(1f,1f,1f, Mathf.Lerp(0f,              incomingTargetAlpha,   t));
            if (newCenter     != null) newCenter.color     = new Color(1f,1f,1f, Mathf.Lerp(0.2f,            1f,                   t));
            if (leavingCenter != null) leavingCenter.color = new Color(1f,1f,1f, Mathf.Lerp(leavingStartAlpha, 0.2f,               t));

            yield return null;
        }

        // --- Destroy outgoing, reassign slots ---
        if (outgoing != null) Destroy(outgoing.gameObject);

        if (direction < 0) { leftSlot = centerSlot; centerSlot = rightSlot; rightSlot = incoming; }
        else               { rightSlot = centerSlot; centerSlot = leftSlot; leftSlot  = incoming; }

        // Snap final positions and colors
        if (leftSlot   != null) { leftSlot.GetComponent<RectTransform>().anchoredPosition   = new Vector2(-slotStep, 0f); leftSlot.color   = new Color(1f,1f,1f,0.2f); }
        if (centerSlot != null) { centerSlot.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;               centerSlot.color = new Color(1f,1f,1f,1f);   }
        if (rightSlot  != null) { rightSlot.GetComponent<RectTransform>().anchoredPosition  = new Vector2( slotStep, 0f); rightSlot.color  = new Color(1f,1f,1f,0.2f); }

        UpdateNameLabel();
        UpdateBackground(count);
        isAnimating = false;
    }

    // -------------------------------------------------------------------------
    Image MakeSlot(int itemIndex, Vector2 pos, float alpha)
    {
        var slot = Instantiate(slotPrefab, transform);
        slot.material       = null;
        slot.sprite         = inventory.GetItem(itemIndex)?.icon;
        slot.preserveAspect = slot.sprite != null;
        slot.color          = new Color(1f, 1f, 1f, alpha);

        var rect = slot.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = new Vector2(slotW, slotH);
        rect.anchoredPosition = pos;
        return slot;
    }

    void DestroySlots()
    {
        if (leftSlot   != null) { Destroy(leftSlot.gameObject);   leftSlot   = null; }
        if (centerSlot != null) { Destroy(centerSlot.gameObject); centerSlot = null; }
        if (rightSlot  != null) { Destroy(rightSlot.gameObject);  rightSlot  = null; }
    }
}
