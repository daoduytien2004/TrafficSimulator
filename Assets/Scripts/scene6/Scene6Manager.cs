using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;

/// <summary>
/// Quản lý state machine Scene 6 — Nhường đường xe cấp cứu.
///
/// States:
///   WAITING_AT_RED       → đang chờ đèn đỏ
///   AMBULANCE_APPROACHING → xe cấp cứu đang đến
///   PLAYER_YIELDED        → người chơi đã nhường đúng cách → Win
///   PLAYER_BLOCKED        → đứng im chặn → phạt Điều 22
///   PLAYER_RAN_RED        → vượt hẳn qua ngã tư → phạt Điều 11
/// </summary>
public class Scene6Manager : MonoBehaviour
{
    public enum ScenarioState
    {
        WAITING_AT_RED,
        AMBULANCE_APPROACHING,
        PLAYER_YIELDED,
        PLAYER_BLOCKED,
        PLAYER_RAN_RED
    }

    [Header("Trạng thái hiện tại (chỉ xem)")]
    [SerializeField] private ScenarioState currentState = ScenarioState.WAITING_AT_RED;

    [Header("Tham chiếu")]
    public AmbulanceAI ambulance;
    public Transform playerTransform;

    [Header("Đèn giao thông FCG")]
    [Tooltip("Kéo component TrafficLights2 của cụm đèn ngã tư vào đây")]
    public TrafficLights2 trafficLightsController;
    [Tooltip("Kéo component TrafficLight của đèn PHÍA TRƯỚC xe player vào đây")]
    public TrafficLight playerFacingLight;

    [Header("Điều kiện nhường đúng cách")]
    [Tooltip("Khoảng cách tối thiểu player phải dịch về phía bên (ngang) để tính là nhường")]
    public float minLateralOffset = 1.2f;
    [Tooltip("Khoảng cách tối đa player được tiến về phía trước (không vượt ngã tư)")]
    public float maxForwardDistance = 3.5f;
    [Tooltip("Vị trí vạch dừng — dùng để tính khoảng tiến")]
    public Transform stopLineTransform;

    [Header("Đếm ngược đèn đỏ")]
    public TextMeshProUGUI redLightCountdownText;
    public float redLightDuration = 15f;

    [Header("UI Panels")]
    public GameObject panelBlocked;   // Vi phạm Điều 22 — cản trở xe ưu tiên
    public GameObject panelRanRed;    // Vi phạm Điều 11 — vượt đèn đỏ
    public GameObject panelWin;       // Hoàn thành kịch bản
    public GameObject panelTutorial;  // Hướng dẫn ban đầu

    [Header("Cài đặt Reset")]
    public float resetDelay = 4f;
    public int nextSceneIndex = -1;   // -1 = không chuyển scene

    private float stopLineZ;          // Z của vạch dừng (tính tiến/lùi)
    private float startX;             // X ban đầu của player (tính lệch ngang)
    private bool outcomeDecided = false;

    // =========================================================================
    void Start()
    {
        if (panelBlocked != null) panelBlocked.SetActive(false);
        if (panelRanRed != null) panelRanRed.SetActive(false);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelTutorial != null) panelTutorial.SetActive(true);

        if (stopLineTransform != null) stopLineZ = stopLineTransform.position.z;
        if (playerTransform != null) startX = playerTransform.position.x;

        // Tắt chu kỳ tự động của FCG, khoá đèn đỏ cho player
        if (trafficLightsController != null) trafficLightsController.enabled = false;
        if (playerFacingLight != null) playerFacingLight.SetStatus("1"); // "1" = Đỏ

        StartCoroutine(RedLightCountdown());
    }

    // =========================================================================
    private IEnumerator RedLightCountdown()
    {
        float remaining = redLightDuration;

        while (remaining > 0f)
        {
            if (redLightCountdownText != null)
                redLightCountdownText.text = $"Den do: {Mathf.CeilToInt(remaining)}s";

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (redLightCountdownText != null)
            redLightCountdownText.text = "DEN XANH";
    }

    // =========================================================================
    /// <summary>Gọi từ EmergencyVehicleTrigger khi xe cấp cứu kích hoạt</summary>
    public void OnAmbulanceApproaching()
    {
        if (currentState != ScenarioState.WAITING_AT_RED) return;

        currentState = ScenarioState.AMBULANCE_APPROACHING;

        if (panelTutorial != null) panelTutorial.SetActive(false);
        if (ambulance != null) ambulance.StartApproaching(playerTransform);

        StartCoroutine(MonitorPlayerBehavior());
    }

    // =========================================================================
    private IEnumerator MonitorPlayerBehavior()
    {
        while (currentState == ScenarioState.AMBULANCE_APPROACHING)
        {
            // Đã có outcome từ ambulance callback → thoát
            if (outcomeDecided) yield break;

            yield return null;
        }
    }

    // =========================================================================
    /// <summary>Gọi từ AmbulanceAI khi xe cấp cứu đã đi qua được</summary>
    public void OnAmbulancePassed()
    {
        if (outcomeDecided) return;

        // Kiểm tra player có thực sự nhường đúng cách không
        if (playerTransform == null) { ResolveYielded(); return; }

        float lateralMove = Mathf.Abs(playerTransform.position.x - startX);
        float forwardMove = playerTransform.position.z - stopLineZ;

        bool movedSideways = lateralMove >= minLateralOffset;
        bool didNotCrossIntersection = forwardMove <= maxForwardDistance;

        if (movedSideways && didNotCrossIntersection)
            ResolveYielded();
        else if (forwardMove > maxForwardDistance)
            ResolveRanRed();
        else
            ResolveBlocked(); // Dạt ngang không đủ → không tạo được lối
    }

    /// <summary>Gọi từ AmbulanceAI khi bị chặn quá lâu</summary>
    public void OnPlayerBlocked()
    {
        if (outcomeDecided) return;
        ResolveBlocked();
    }

    /// <summary>Gọi từ IntersectionZone khi player vượt qua ngã tư hoàn toàn</summary>
    public void OnPlayerRanRedLight()
    {
        if (outcomeDecided) return;
        ResolveRanRed();
    }

    // =========================================================================
    private void ResolveYielded()
    {
        outcomeDecided = true;
        currentState = ScenarioState.PLAYER_YIELDED;

        // Bật đèn xanh khi player thắng
        if (playerFacingLight != null) playerFacingLight.SetStatus("3"); // "3" = Xanh
        if (redLightCountdownText != null) redLightCountdownText.text = "ĐÈN XANH";

        if (panelWin != null) panelWin.SetActive(true);
        Time.timeScale = 0f;

        StartCoroutine(LoadNextRoutine());
    }

    private void ResolveBlocked()
    {
        outcomeDecided = true;
        currentState = ScenarioState.PLAYER_BLOCKED;

        if (panelBlocked != null) panelBlocked.SetActive(true);
        Time.timeScale = 0f;

        StartCoroutine(ResetRoutine());
    }

    private void ResolveRanRed()
    {
        outcomeDecided = true;
        currentState = ScenarioState.PLAYER_RAN_RED;

        if (panelRanRed != null) panelRanRed.SetActive(true);
        Time.timeScale = 0f;

        StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        yield return new WaitForSecondsRealtime(resetDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator LoadNextRoutine()
    {
        yield return new WaitForSecondsRealtime(resetDelay);
        Time.timeScale = 1f;
        if (nextSceneIndex >= 0)
            SceneManager.LoadScene(nextSceneIndex);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // =========================================================================
    // GỌI TỪ NÚT "THỬ LẠI" TRÊN UI
    // =========================================================================

    /// <summary>Gọi từ nút Thử lại trên Panel_Blocked và Panel_RanRed</summary>
    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
