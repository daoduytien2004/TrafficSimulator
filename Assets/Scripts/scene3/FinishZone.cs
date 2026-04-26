using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class FinishZone : MonoBehaviour
{
    [Header("Cài đặt kết thúc")]
    public GameObject victoryPanel;   // Kéo VictoryPanel vào đây
    public AudioSource victoryAudio; // (Tùy chọn) Loa phát nhạc chiến thắng
    public float restartDelay = 5f;  // Đợi 5 giây rồi mới reset game

    private bool isFinished = false;

    void OnTriggerEnter(Collider other)
    {
        // Nếu người chơi chạm vào vạch đích
        if (other.CompareTag("Player") && !isFinished)
        {
            isFinished = true;
            FinishLevel();
        }
    }

    void FinishLevel()
    {
        Debug.Log("Chúc mừng! Đã về đích.");

        // 1. Hiện bảng chiến thắng
        if (victoryPanel != null) victoryPanel.SetActive(true);

        // 2. Phát nhạc chiến thắng
        if (victoryAudio != null) victoryAudio.Play();

        // 3. Tìm và xóa hết tất cả xe máy đang có trên map để tránh tai nạn muộn
        PunishmentMoto[] allMotos = FindObjectsOfType<PunishmentMoto>();
        foreach (PunishmentMoto moto in allMotos)
        {
            Destroy(moto.gameObject);
        }

        // 4. Dừng thời gian (Đóng băng game)
        Time.timeScale = 0f;

        // 5. Đợi một lát rồi quay lại màn hình chính hoặc chơi lại
        StartCoroutine(RestartAfterVictory());
    }

    IEnumerator RestartAfterVictory()
    {
        // Vì Time.timeScale = 0 nên phải dùng WaitForSecondsRealtime
        yield return new WaitForSecondsRealtime(restartDelay);
        Time.timeScale = 1f;

        // --- ĐÃ SỬA MỚI: QUAY VỀ MENU CHÍNH ---
        SceneManager.LoadScene("MainMenu");
        // --------------------------------------
    }
}