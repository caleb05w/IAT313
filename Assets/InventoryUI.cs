using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Displays inventory items above the player's head.
// 1 item  → center only, no animation.
// 2 items → symmetric pair (-slotStep/2 and +slotStep/2), smooth slide with wrap.
// 3+items → left + center + right, smooth slide with wrap.
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
    [SerializeField] private float     nameFontSize    = 0.1f;

    private Image leftSlot, centerSlot, rightSlot;
    private Image background;
    private TextMeshProUGUI nameLabel;
    private float slotStep, slotW, slotH;

    private Transform playerTransform;
    private bool isOpen      = false;
    private bool isAnimating = false;
    private Coroutine openCoroutine;

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
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= OnInventoryChanged;
    }

    // -------------------------------------------------------------------------
    void LateUpdate()
    {
        if (Keyboard.current[toggleKey].wasPressedThisFrame &&
            (GameManager.Instance == null || GameManager.Instance.IsState(GameManager.GameState.Explore)))
        {
            isOpen = !isOpen;
            if (openCoroutine != null) StopCoroutine(openCoroutine);
            openCoroutine = StartCoroutine(AnimateToggle(isOpen));
        }

        if (playerTransform != null && transform.localScale.x > 0f)
            transform.position = playerTransform.position + headOffset;
    }

    // -------------------------------------------------------------------------
    IEnumerator AnimateToggle(bool opening)
    {
        if (opening)
        {
            Rebuild();
            transform.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / openDuration));
                // Slight overshoot: scale to 1.1 then snap — punch feel
                float s = t < 0.8f ? Mathf.Lerp(0f, 1.1f, t / 0.8f) : Mathf.Lerp(1.1f, 1f, (t - 0.8f) / 0.2f);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;
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
        if (!isOpen || inventory.Count == 0) { UpdateBackground(0); UpdateNameLabel(); return; }

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

        float offScreen = slotStep * 2f;

        if (direction < 0) // slide left
        {
            outgoingEnd         = new Vector2(-offScreen, 0f);
            incomingStart       = new Vector2( offScreen, 0f);
            incomingTargetAlpha = 0.2f;
            outgoing            = leftSlot;
        }
        else               // slide right
        {
            outgoingEnd         = new Vector2( offScreen, 0f);
            incomingStart       = new Vector2(-offScreen, 0f);
            incomingTargetAlpha = 0.2f;
            outgoing            = rightSlot;
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
