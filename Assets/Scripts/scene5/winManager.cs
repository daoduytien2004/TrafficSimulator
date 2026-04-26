using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// WinManager — Màn hình thắng khi đến đích.
/// Gắn vào GameObject "WinManager" trong scene.
/// </summary>
public class WinManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI statsText;
    public Button replayButton;
    public Button nextSceneButton;

    [Header("Cài đặt")]
    public float fadeInDuration = 1f;
    public int nextSceneIndex = -1;   // -1 = không có scene tiếp

    private CanvasGroup canvasGroup;

    void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        canvasGroup = winPanel != null ? winPanel.GetComponent<CanvasGroup>() : null;

        if (replayButton != null)
            replayButton.onClick.AddListener(() => {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });

        if (nextSceneButton != null)
        {
            nextSceneButton.gameObject.SetActive(nextSceneIndex >= 0);
            nextSceneButton.onClick.AddListener(() => {
                Time.timeScale = 1f;
                SceneManager.LoadScene(nextSceneIndex);
            });
        }
    }

    /// <summary>Gọi từ GoalTrigger khi đến đích</summary>
    public void TriggerWin(VR_CarController_Scene5 car)
    {
        if (winPanel == null) return;

        winPanel.SetActive(true);
        Time.timeScale = 0f;

        // Thống kê
        int hornCount = car != null ? car.GetHornCount() : 0;
        string hornMsg = hornCount == 0
            ? "Tuyet voi! Ban khong bam coi lan nao!"
            : $"Ban da bam coi {hornCount} lan.";

        if (titleText != null)
            titleText.text = "Da den dich!";

        if (subtitleText != null)
            subtitleText.text = "Cam on ban da ton trong giac ngu cua hang xom.";

        if (statsText != null)
            statsText.text = hornMsg;

        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}