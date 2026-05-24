using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý state machine Scene 6 — Nhường đường xe cấp cứu.
///
/// Phát hiện đèn đỏ bằng raycast (giống NPC cars của FCG):
///   Bắn ray từ xe player về phía trước — nếu trúng object tên "Stop" → đèn đỏ.
///
/// States:
///   WAITING_AT_RED       → đang chờ đèn đỏ
///   AMBULANCE_APPROACHING → xe cấp cứu đang đến
///   PLAYER_YIELDED        → người chơi đã nhường đúng cách → Win
///   PLAYER_BLOCKED        → đứng im chặn → phạt Điều 27
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

    [Header("Phát hiện đèn giao thông (Raycast — không cần gán thủ công)")]
    [Tooltip("Khoảng cách tối đa raycast kiểm tra đèn phía trước xe player")]
    public float stopDetectDistance = 12f;

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
    public GameObject panelBlocked;   // Vi phạm Điều 27 — cản trở xe ưu tiên
    public GameObject panelRanRed;    // Vi phạm Điều 11 — vượt đèn đỏ
    public GameObject panelWin;       // Hoàn thành kịch bản
    public GameObject panelTutorial;  // Hướng dẫn ban đầu
    public GameObject retryButton;    // Nút Thử lại dùng chung

    [Header("Âm thanh vi phạm")]
    public AudioSource policeWhistleAudio;
    public AudioClip policeWhistleClip;

    [Header("Cài đặt Reset")]
    public float resetDelay = 4f;
    public int nextSceneIndex = -1;   // -1 = không chuyển scene

    private float stopLineZ;
    private float startX;
    private bool outcomeDecided = false;
    private bool sceneStarted = false;

    // =========================================================================
    void Start()
    {
        // Đảm bảo scene chạy bình thường dù reload từ CarController (timeScale có thể = 0)
        Time.timeScale = 1f;

        if (panelBlocked != null) panelBlocked.SetActive(false);
        if (panelRanRed != null) panelRanRed.SetActive(false);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelTutorial != null) panelTutorial.SetActive(true);
        if (retryButton != null) retryButton.SetActive(false);

        if (stopLineTransform != null) stopLineZ = stopLineTransform.position.z;
        if (playerTransform != null) startX = playerTransform.position.x;
    }

    // Raycast giống NPC cars — trúng object tên "Stop" = đèn đỏ
    private bool IsPlayerAtRedLight()
    {
        if (playerTransform == null) return false;
        Ray ray = new Ray(playerTransform.position + Vector3.up * 0.5f, playerTransform.forward);
        return Physics.Raycast(ray, out RaycastHit hit, stopDetectDistance)
               && hit.transform.name == "Stop";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            Debug.Log($"[Scene6] Key 1 pressed — panelTutorial={(panelTutorial != null ? panelTutorial.activeSelf.ToString() : "NULL")} sceneStarted={sceneStarted}");
            if (panelTutorial != null && panelTutorial.activeSelf)
            {
                panelTutorial.SetActive(false);
                sceneStarted = true;
                StartCoroutine(TrackLightState());
            }
            else if (outcomeDecided)
                RestartScene();
        }

        if (!sceneStarted || playerTransform == null) return;

        bool isRed = IsPlayerAtRedLight();

        if (!outcomeDecided && redLightCountdownText != null && !isRed)
            redLightCountdownText.text = "ĐÈN XANH";
    }

    // =========================================================================
    private IEnumerator TrackLightState()
    {
        float redElapsed = 0f;

        while (!outcomeDecided)
        {
            bool isRed = IsPlayerAtRedLight();

            if (isRed)
            {
                redElapsed += Time.deltaTime;
                float remaining = Mathf.Max(0f, redLightDuration - redElapsed);
                if (redLightCountdownText != null)
                    redLightCountdownText.text = $"Đèn đỏ: {Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                redElapsed = 0f;
            }

            yield return null;
        }
    }

    private void SwitchToGreen()
    {
        if (redLightCountdownText != null) redLightCountdownText.text = "ĐÈN XANH";
        // IntersectionZone do TrafficLightSync.cs tự tắt khi đèn xanh
    }

    // =========================================================================
    /// <summary>Gọi từ EmergencyVehicleTrigger khi xe cấp cứu kích hoạt</summary>
    public void OnAmbulanceApproaching()
    {
        if (currentState != ScenarioState.WAITING_AT_RED) return;

        // Auto-start nếu player vào trigger trước khi bấm phím 1
        if (!sceneStarted)
        {
            sceneStarted = true;
            if (panelTutorial != null) panelTutorial.SetActive(false);
            StartCoroutine(TrackLightState());
        }

        currentState = ScenarioState.AMBULANCE_APPROACHING;

        if (ambulance == null) Debug.LogError("[Scene6Manager] ambulance chưa được gán!");
        if (ambulance != null) ambulance.StartApproaching(playerTransform);

        StartCoroutine(MonitorPlayerBehavior());
    }

    // =========================================================================
    private IEnumerator MonitorPlayerBehavior()
    {
        while (currentState == ScenarioState.AMBULANCE_APPROACHING)
        {
            if (outcomeDecided) yield break;
            yield return null;
        }
    }

    // =========================================================================
    /// <summary>Gọi từ AmbulanceAI khi xe cấp cứu đã đi qua được</summary>
    public void OnAmbulancePassed()
    {
        if (outcomeDecided) return;

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
            ResolveBlocked();
    }

    /// <summary>Gọi từ AmbulanceAI khi bị chặn quá lâu</summary>
    public void OnPlayerBlocked()
    {
        if (outcomeDecided) return;
        ResolveBlocked();
    }

    /// <summary>Gọi từ IntersectionZoneTrigger khi player vượt qua ngã tư khi đèn đỏ</summary>
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

        SwitchToGreen();

        if (panelWin != null) panelWin.SetActive(true);
        Time.timeScale = 0f;

        StartCoroutine(LoadNextRoutine());
    }

    private void ResolveBlocked()
    {
        outcomeDecided = true;
        currentState = ScenarioState.PLAYER_BLOCKED;

        PlayViolationSound();
        if (panelBlocked != null) panelBlocked.SetActive(true);
        if (retryButton != null) retryButton.SetActive(true);
        Time.timeScale = 0f;
    }

    private void ResolveRanRed()
    {
        outcomeDecided = true;
        currentState = ScenarioState.PLAYER_RAN_RED;

        PlayViolationSound();
        if (panelRanRed != null) panelRanRed.SetActive(true);
        if (retryButton != null) retryButton.SetActive(true);
        Time.timeScale = 0f;
    }

    private void PlayViolationSound()
    {
        foreach (AudioSource src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (src != policeWhistleAudio)
                src.Stop();
        }

        if (policeWhistleAudio != null && policeWhistleClip != null)
        {
            policeWhistleAudio.clip = policeWhistleClip;
            policeWhistleAudio.loop = false;
            policeWhistleAudio.Play();
        }
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
    /// <summary>Gọi từ nút Thử lại trên Panel_Blocked và Panel_RanRed</summary>
    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
