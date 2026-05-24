using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý màn hình Game Over khi người chơi bấm còi quá 3 lần.
/// Gắn vào một GameObject tên "GameOverManager" trong scene.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("UI Game Over")]
    public GameObject gameOverPanel;          // Panel tổng (bật lên khi thua)
    public TextMeshProUGUI titleText;          // "Bạn đã làm phiền cả khu phố!"
    public TextMeshProUGUI statsText;          // Thống kê: X người bị đánh thức
    public TextMeshProUGUI messageText;        // Thông điệp giáo dục
    public Button retryButton;                // Nút chơi lại
    public Button mainMenuButton;             // Nút về menu

    [Header("Cài đặt")]
    public float fadeInDuration = 0.8f;
    [Tooltip("Delay (giây) sau khi scene reload")]
    public float reloadDelay = 1f;

    [Header("Thống kê tác hại (tuỳ chỉnh)")]
    [Tooltip("Số người bị đánh thức mỗi lần bấm còi")]
    public int peopleAffectedPerHorn = 12;

    private CanvasGroup canvasGroup;

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        canvasGroup = gameOverPanel != null
            ? gameOverPanel.GetComponent<CanvasGroup>()
            : null;

        if (retryButton != null)
            retryButton.onClick.AddListener(ReloadScene);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    /// <summary>
    /// Gọi từ VR_CarController khi hết 3 cảnh báo.
    /// hornCount = tổng số lần bấm còi (thường là 3).
    /// </summary>
    public void TriggerGameOver(int hornCount)
    {
        if (gameOverPanel == null)
        {
            Debug.LogError("[GameOverManager] gameOverPanel CHƯA GÁN! Game Over sẽ không hoạt động.");
            return;
        }

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;

        // Điền nội dung
        int totalAffected = hornCount * peopleAffectedPerHorn;

        if (statsText != null)
            statsText.text = $"Ban da bam coi {hornCount} lan\n"
                           + $"Uoc tinh {totalAffected} nguoi bi danh thuc\n"
                           + $"Tieng coi xe ban dem co the dat 100 dB\n"
                           + $"— tuong duong tieng khoan tuong!";


        // Fade in panel
        if (canvasGroup != null)
            StartCoroutine(FadeIn());

        Debug.Log($"[GameOverManager] Game Over — {hornCount} lan bam coi, {totalAffected} nguoi bi anh huong.");
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    private void ReloadScene()
    {
        Time.timeScale = 1f;
        StartCoroutine(ReloadAfterDelay());
    }

    private IEnumerator ReloadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(reloadDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // Scene index 0 = Main Menu
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime; // dùng unscaled vì Time.timeScale = 0
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}