using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Thư viện để load lại map

public class TurnScenario : MonoBehaviour
{
    // BÌNH MỚI: Thêm lựa chọn "Finish" vào danh sách
    public enum TurnType { Left, Right, Straight, Finish }
    public TurnType turnDirection;

    [Header("UI Chỉ đường")]
    public GameObject turnIndicator;

    [Header("Xe máy trừng phạt / Giao thông")]
    public GameObject motoPrefab;
    public float spawnDistance = 25f;
    public float groundOffset = 0f;

    [Header("Cài đặt Đích đến (Chỉ dùng khi chọn Finish)")]
    public GameObject victoryPanel;   // Kéo bảng chúc mừng vào đây
    public float restartDelay = 5f;  // Thời gian đợi trước khi chơi lại

    private VR_CarController playerCar;
    private PunishmentMoto spawnedMoto;
    private bool playerInZone = false;
    private bool hasPassed = false;
    private bool hasDoneSafetyCheck = false;

    private Vector3 initialCarForward;
    private Vector3 initialLaneForward;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerInZone)
        {
            playerCar = other.GetComponent<VR_CarController>();
            playerInZone = true;

            // NẾU LÀ ĐÍCH ĐẾN: Chạy kịch bản chiến thắng và kết thúc luôn tại đây
            if (turnDirection == TurnType.Finish)
            {
                StartCoroutine(HandleVictory());
                return; // Lệnh này giúp bỏ qua toàn bộ phần code rẽ/xe máy ở bên dưới
            }

            // --- KỊCH BẢN BÌNH THƯỜNG (Rẽ hoặc Đi thẳng) ---
            hasPassed = (turnDirection == TurnType.Straight);
            hasDoneSafetyCheck = false;

            initialCarForward = playerCar.transform.forward;
            initialLaneForward = transform.forward;

            if (turnIndicator != null) turnIndicator.SetActive(true);

            if (turnDirection == TurnType.Left) playerCar.hasLookedLeftMirror = false;
            else if (turnDirection == TurnType.Right) playerCar.hasLookedRightMirror = false;

            SpawnMoto();

            if (turnDirection == TurnType.Straight && spawnedMoto != null) spawnedMoto.PassStraight();
        }
    }

    // KỊCH BẢN CHIẾN THẮNG
    IEnumerator HandleVictory()
    {
        Debug.Log("Chúc mừng! Đã về đích.");

        // Bật bảng chúc mừng
        if (victoryPanel != null) victoryPanel.SetActive(true);

        // Dọn dẹp: Xóa sạch xe máy đang có trên đường để không ai tông mình lúc đang ăn mừng
        PunishmentMoto[] allMotos = FindObjectsOfType<PunishmentMoto>();
        foreach (PunishmentMoto moto in allMotos)
        {
            Destroy(moto.gameObject);
        }

        // Đóng băng trò chơi
        Time.timeScale = 0f;

        // Đợi vài giây rồi reset lại bàn chơi
        yield return new WaitForSecondsRealtime(restartDelay);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void SpawnMoto()
    {
        float sideOffset = 0f;
        if (turnDirection == TurnType.Left) sideOffset = -4f;
        else if (turnDirection == TurnType.Right) sideOffset = 4f;
        else sideOffset = (Random.value > 0.5f) ? 4f : -4f;

        Vector3 spawnPos = playerCar.transform.position - initialLaneForward * spawnDistance + playerCar.transform.right * sideOffset;

        Vector3 rayStart = spawnPos + Vector3.up * 5f;
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f)) spawnPos.y = hit.point.y + groundOffset;
        else spawnPos.y = playerCar.transform.position.y;

        Quaternion spawnRot = Quaternion.LookRotation(initialLaneForward);

        GameObject newMoto = Instantiate(motoPrefab, spawnPos, spawnRot);
        spawnedMoto = newMoto.GetComponent<PunishmentMoto>();

        if (spawnedMoto != null) spawnedMoto.SetTarget(playerCar.transform, sideOffset);
    }

    void Update()
    {
        // Bỏ qua update nếu là đích đến
        if (turnDirection == TurnType.Finish) return;

        if (playerInZone && playerCar != null && spawnedMoto != null && turnDirection != TurnType.Straight)
        {
            if (!hasPassed)
            {
                bool isSignaling = (turnDirection == TurnType.Left) ? playerCar.isLeftSignalOn : playerCar.isRightSignalOn;
                bool hasLooked = (turnDirection == TurnType.Left) ? playerCar.hasLookedLeftMirror : playerCar.hasLookedRightMirror;

                if (isSignaling && hasLooked)
                {
                    hasPassed = true;
                    spawnedMoto.SlowDown();
                }
                else
                {
                    float turnAngle = Vector3.SignedAngle(initialLaneForward, playerCar.transform.forward, Vector3.up);

                    bool isTurning = false;
                    if (turnDirection == TurnType.Left && turnAngle < -20f) isTurning = true;
                    else if (turnDirection == TurnType.Right && turnAngle > 20f) isTurning = true;

                    if (isTurning)
                    {
                        spawnedMoto.SpeedUpToCrash();
                    }
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Bỏ qua nếu là đích đến
        if (turnDirection == TurnType.Finish) return;

        if (other.CompareTag("Player") && playerInZone)
        {
            playerInZone = false;
            if (turnIndicator != null) turnIndicator.SetActive(false);

            if (turnDirection != TurnType.Straight)
            {
                if (!hasPassed && spawnedMoto != null)
                {
                    spawnedMoto.PassStraight();
                    Destroy(spawnedMoto.gameObject, 5f);
                }
                else if (hasPassed)
                {
                    playerCar.isLeftSignalOn = false;
                    playerCar.isRightSignalOn = false;
                    if (spawnedMoto != null) Destroy(spawnedMoto.gameObject, 4f);
                }
            }
            else
            {
                if (spawnedMoto != null) Destroy(spawnedMoto.gameObject, 5f);
            }
        }
    }
}