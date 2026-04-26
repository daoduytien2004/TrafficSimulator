using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HornUIController v2 — kết nối VR_CarController_Scene5 với HornWarningHUD.uxml
/// Gắn vào GameObject HornHUD (con của Main Camera trong XR Origin)
/// </summary>
public class HornUIController : MonoBehaviour
{
    [Header("Tham chiếu")]
    public VR_CarController_Scene5 carController;

    private VisualElement warningCard;
    private Label warningTitle;
    private Label warnSubText;
    private VisualElement vignetteOverlay;
    private VisualElement cooldownPill;
    private VisualElement hornHint;
    private VisualElement dot1, dot2, dot3;

    private Coroutine blinkRoutine;

    // Sub text theo từng cấp độ
    private readonly string[] subTexts = {
        "",
        "Khu dan cu dang ngu",
        "Hang xom thuc giac!",
        "GAME OVER sap xay ra!"
    };

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        warningCard    = root.Q<VisualElement>("WarningIcon");
        warningTitle   = root.Q<Label>("WarningCountText");
        warnSubText    = root.Q<Label>("WarnSubText");
        vignetteOverlay = root.Q<VisualElement>("VignetteOverlay");
        cooldownPill   = root.Q<VisualElement>("CooldownIndicator");
        hornHint       = root.Q<VisualElement>("HornHint");
        dot1 = root.Q<VisualElement>("Dot1");
        dot2 = root.Q<VisualElement>("Dot2");
        dot3 = root.Q<VisualElement>("Dot3");

        HideAll();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Hiện card cảnh báo với đúng cấp độ 1/2/3</summary>
    public void ShowWarning(int level, string titleText)
    {
        // Hiện card
        warningCard.RemoveFromClassList("hidden");
        warningCard.AddToClassList("visible");

        // Set text
        if (warningTitle != null) warningTitle.text = titleText;
        if (warnSubText != null && level < subTexts.Length)
            warnSubText.text = subTexts[level];

        // Đổi class cấp độ (màu border, icon, text)
        warningCard.RemoveFromClassList("level-2");
        warningCard.RemoveFromClassList("level-3");
        if (level == 2) warningCard.AddToClassList("level-2");
        if (level == 3) warningCard.AddToClassList("level-3");

        // Cập nhật vignette class
        vignetteOverlay.RemoveFromClassList("hidden");
        vignetteOverlay.RemoveFromClassList("level-2");
        vignetteOverlay.RemoveFromClassList("level-3");
        if (level == 2) vignetteOverlay.AddToClassList("level-2");
        if (level == 3) vignetteOverlay.AddToClassList("level-3");

        // Cập nhật 3 chấm
        UpdateDots(level);

        // Nhấp nháy
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkCard(level == 3 ? 6 : 3));
    }

    /// <summary>Hiện/ẩn vignette flash (gọi riêng nếu cần)</summary>
    public void ShowVignette(float intensity, bool flash = false)
    {
        vignetteOverlay.RemoveFromClassList("hidden");
        if (flash)
        {
            vignetteOverlay.AddToClassList("level-3");
            StartCoroutine(FadeOutVignette(2f));
        }
    }

    /// <summary>Hiện/ẩn cooldown pill</summary>
    public void ShowCooldown(bool show)
    {
        if (show)
        {
            cooldownPill.RemoveFromClassList("hidden");
            cooldownPill.AddToClassList("visible");
        }
        else
        {
            cooldownPill.RemoveFromClassList("visible");
            cooldownPill.AddToClassList("hidden");
        }
    }

    /// <summary>Hiện hint "Nhấn H để bấm còi" khi vào zone</summary>
    public void ShowHornHint(bool show)
    {
        if (show)
        {
            hornHint.RemoveFromClassList("hidden");
            hornHint.AddToClassList("visible");
        }
        else
        {
            hornHint.RemoveFromClassList("visible");
            hornHint.AddToClassList("hidden");
        }
    }

    /// <summary>Reset toàn bộ về trạng thái ẩn</summary>
    public void HideAll()
    {
        warningCard?.AddToClassList("hidden");
        warningCard?.RemoveFromClassList("visible");
        warningCard?.RemoveFromClassList("level-2");
        warningCard?.RemoveFromClassList("level-3");

        vignetteOverlay?.AddToClassList("hidden");
        vignetteOverlay?.RemoveFromClassList("level-2");
        vignetteOverlay?.RemoveFromClassList("level-3");

        cooldownPill?.AddToClassList("hidden");
        cooldownPill?.RemoveFromClassList("visible");

        hornHint?.AddToClassList("hidden");
        hornHint?.RemoveFromClassList("visible");

        if (warningTitle != null) warningTitle.text = "";
        ResetDots();
    }

    // ── Dots ──────────────────────────────────────────────────────────────────

    private void UpdateDots(int level)
    {
        ResetDots();
        if (level >= 1 && dot1 != null) dot1.AddToClassList("filled-1");
        if (level >= 2 && dot2 != null) dot2.AddToClassList("filled-2");
        if (level >= 3 && dot3 != null) dot3.AddToClassList("filled-3");
    }

    private void ResetDots()
    {
        dot1?.RemoveFromClassList("filled-1");
        dot2?.RemoveFromClassList("filled-2");
        dot3?.RemoveFromClassList("filled-3");
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator BlinkCard(int times)
    {
        for (int i = 0; i < times; i++)
        {
            warningCard.style.opacity = 0.2f;
            yield return new WaitForSeconds(0.12f);
            warningCard.style.opacity = 1f;
            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator FadeOutVignette(float duration)
    {
        yield return new WaitForSeconds(0.5f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            vignetteOverlay.style.opacity = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        vignetteOverlay.style.opacity = 0f;
        vignetteOverlay.AddToClassList("hidden");
        vignetteOverlay.RemoveFromClassList("level-3");
    }
}
