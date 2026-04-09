using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class TurnScenario : MonoBehaviour
{
    public enum TurnType { Left, Right, Straight, Finish }
    public TurnType turnDirection;

    [Header("UI Chỉ đường")]
    public GameObject turnIndicator;

    [Header("Xe máy trừng phạt / Giao thông")]
    public GameObject motoPrefab;

    [Tooltip("Khoảng cách xuất hiện phía sau ô tô. Nếu đoạn đường trước ngã tư bị ngắn/cong, hãy GIẢM số này xuống (VD: 15)")]
    public float spawnDistance = 25f;
    public float groundOffset = 0f;

    // --- BÌNH MỚI: Biến chỉnh độ rộng làn đường ---
    [Tooltip("Khoảng cách lệch sang ngang. Nếu xe máy chui từ tường ra do đường hẹp, hãy GIẢM số này xuống (VD: 2 hoặc 2.5)")]
    public float laneWidthOffset = 3f;

    [Header("Cài đặt Đích đến (Chỉ dùng khi chọn Finish)")]
    public GameObject victoryPanel;
    public float restartDelay = 5f;

    private VR_CarController playerCar;
    private PunishmentMoto spawnedMoto;
    private bool playerInZone = false;
    private bool hasPassed = false;
    private bool hasDoneSafetyCheck = false;

    private Vector3 initialCarForward;
    private Vector3 initialLaneForward;
    private Vector3 initialLaneRight; // Lưu hướng ngang của mặt đường

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerInZone)
        {
            playerCar = other.GetComponent<VR_CarController>();
            playerInZone = true;

            if (turnDirection == TurnType.Finish)
            {
                StartCoroutine(HandleVictory());
                return;
            }

            hasPassed = (turnDirection == TurnType.Straight);
            hasDoneSafetyCheck = false;

            initialCarForward = playerCar.transform.forward;

            // Lấy chuẩn hướng của MẶT ĐƯỜNG (Khối Cube tàng hình), không lấy hướng của ô tô nữa
            initialLaneForward = transform.forward;
            initialLaneRight = transform.right;

            if (turnIndicator != null) turnIndicator.SetActive(true);

            if (turnDirection == TurnType.Left) playerCar.hasLookedLeftMirror = false;
            else if (turnDirection == TurnType.Right) playerCar.hasLookedRightMirror = false;

            SpawnMoto();

            if (turnDirection == TurnType.Straight && spawnedMoto != null) spawnedMoto.PassStraight();
        }
    }

    IEnumerator HandleVictory()
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);

        PunishmentMoto[] allMotos = FindObjectsOfType<PunishmentMoto>();
        foreach (PunishmentMoto moto in allMotos)
        {
            Destroy(moto.gameObject);
        }

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(restartDelay);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void SpawnMoto()
    {
        float sideOffset = 0f;
        // Dùng biến laneWidthOffset thay vì số 4 cứng ngắc
        if (turnDirection == TurnType.Left) sideOffset = -laneWidthOffset;
        else if (turnDirection == TurnType.Right) sideOffset = laneWidthOffset;
        else sideOffset = (Random.value > 0.5f) ? laneWidthOffset : -laneWidthOffset;

        // DÙNG initialLaneRight: Đảm bảo xe máy luôn xuất hiện thẳng tắp trên đường, bất chấp ô tô đi ngoằn ngoèo
        Vector3 spawnPos = playerCar.transform.position - initialLaneForward * spawnDistance + initialLaneRight * sideOffset;

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