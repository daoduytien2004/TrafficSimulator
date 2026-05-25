using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hiển thị panel hướng dẫn khi bắt đầu scene.
/// Bấm phím 1 (keyboard) hoặc nút VR được gán để đóng panel.
/// Gắn vào bất kỳ GameObject nào trong scene (vd: "TutorialPanelManager").
/// </summary>
public class TutorialPanelController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Kéo GameObject chứa panel hướng dẫn vào đây")]
    public GameObject panel;

    [Header("Input VR (tuỳ chọn)")]
    [Tooltip("Gán nút VR muốn dùng để bỏ panel — để trống nếu chỉ dùng keyboard")]
    public InputActionReference vrConfirmButton;

    [Header("Cài đặt")]
    [Tooltip("Thời gian fade out panel (giây)")]
    public float fadeOutDuration = 0.4f;
    [Tooltip("Đóng băng gameplay trong khi panel đang hiện (xe không di chuyển được)")]
    public bool freezeTimeWhileShowing = true;

    private CanvasGroup canvasGroup;
    private bool dismissed = false;

    void Start()
    {
        if (panel == null)
        {
            Debug.LogWarning("[TutorialPanel] Chưa gán panel trong Inspector!");
            return;
        }

        canvasGroup = panel.GetComponent<CanvasGroup>();
        panel.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (freezeTimeWhileShowing)
            Time.timeScale = 0f;

        if (vrConfirmButton != null)
            vrConfirmButton.action.Enable();
    }

    void Update()
    {
        if (dismissed || panel == null) return;

        bool pressedKeyboard = Keyboard.current != null
                               && Keyboard.current.digit1Key.wasPressedThisFrame;

        bool pressedVR = vrConfirmButton != null
                         && vrConfirmButton.action.WasPressedThisFrame();

        if (pressedKeyboard || pressedVR)
            Dismiss();
    }

    /// <summary>Có thể gọi từ Button UI hoặc code ngoài để đóng panel.</summary>
    public void Dismiss()
    {
        if (dismissed) return;
        dismissed = true;

        if (freezeTimeWhileShowing)
            Time.timeScale = 1f;

        if (canvasGroup != null)
            StartCoroutine(FadeOutAndHide());
        else if (panel != null)
            panel.SetActive(false);
    }

    private IEnumerator FadeOutAndHide()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            // unscaledDeltaTime để chạy đúng kể cả khi timeScale = 0
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }
        panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;

        if (vrConfirmButton != null)
            vrConfirmButton.action.Disable();
    }
}
