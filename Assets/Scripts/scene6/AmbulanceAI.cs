using System.Collections;
using UnityEngine;

/// <summary>
/// Xe cấp cứu chạy theo waypoint, hú còi spatial audio, đèn nháy đỏ/xanh.
/// Khi bị chặn → dừng lại → báo Scene6Manager.
/// Khi đi qua được → báo Scene6Manager player đã nhường đường.
/// </summary>
public class AmbulanceAI : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Danh sách điểm đường đi theo thứ tự. Waypoint[0] = spawn point.")]
    public Transform[] waypoints;
    public float moveSpeed = 8f;
    public float waypointReachDistance = 1.5f;

    [Header("Âm thanh")]
    [Tooltip("AudioSource 3D gắn trên xe cấp cứu — bật Spatial Blend = 1")]
    public AudioSource sirenAudio;
    public AudioClip sirenClip;

    [Header("Đèn nháy")]
    public GameObject redLight1;
    public GameObject redLight2;
    public GameObject blueLight1;
    public GameObject blueLight2;
    public float flashInterval = 0.25f;

    [Header("Phát hiện bị chặn")]
    [Tooltip("Khoảng cách tối thiểu để phát hiện bị chặn bởi player")]
    public float blockedDetectionDistance = 4f;
    [Tooltip("Thời gian bị chặn (giây) trước khi kết luận player không nhường")]
    public float blockedTimeout = 8f;

    [Header("Tham chiếu")]
    public Scene6Manager scene6Manager;

    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private bool isBlocked = false;
    private bool hasPassedPlayer = false;
    private float blockedTimer = 0f;
    private Transform playerTransform;

    // =========================================================================
    void Start()
    {
        if (sirenAudio != null && sirenClip != null)
        {
            sirenAudio.clip = sirenClip;
            sirenAudio.loop = true;
            sirenAudio.spatialBlend = 1f;
            sirenAudio.Stop();
        }

        SetLights(false, false);
        gameObject.SetActive(false); // Bị kích hoạt bởi EmergencyVehicleTrigger
    }

    // =========================================================================
    /// <summary>Gọi từ EmergencyVehicleTrigger để bắt đầu kịch bản</summary>
    public void StartApproaching(Transform player)
    {
        playerTransform = player;
        isMoving = true;

        if (sirenAudio != null) sirenAudio.Play();
        StartCoroutine(FlashLightsRoutine());

        // Đặt vị trí tại waypoint[0]
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            transform.rotation = waypoints[0].rotation;
            currentWaypointIndex = 1;
        }

        gameObject.SetActive(true);
        StartCoroutine(MoveRoutine());
    }

    // =========================================================================
    private IEnumerator MoveRoutine()
    {
        while (isMoving && currentWaypointIndex < waypoints.Length)
        {
            Transform target = waypoints[currentWaypointIndex];
            float distToTarget = Vector3.Distance(transform.position, target.position);

            // Kiểm tra bị chặn bởi player
            if (playerTransform != null && !hasPassedPlayer)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

                if (distToPlayer < blockedDetectionDistance)
                {
                    // Player đang cản trước mặt
                    isBlocked = true;
                    blockedTimer += Time.deltaTime;

                    if (blockedTimer >= blockedTimeout)
                    {
                        // Hết giờ chờ → player không nhường
                        scene6Manager?.OnPlayerBlocked();
                        isMoving = false;
                        yield break;
                    }

                    yield return null;
                    continue;
                }
                else
                {
                    isBlocked = false;
                    blockedTimer = 0f;
                }
            }

            // Di chuyển đến waypoint hiện tại
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            // Xoay mặt về hướng di chuyển
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction), 8f * Time.deltaTime);

            // Đến waypoint → chuyển sang waypoint tiếp
            if (distToTarget < waypointReachDistance)
            {
                // Waypoint[1] là điểm ngay sau player → nếu đến được = đã vượt qua player
                if (currentWaypointIndex == 1 && !hasPassedPlayer)
                {
                    hasPassedPlayer = true;
                    // Chỉ thông báo "đã qua" nếu player chủ động nhường (Scene6Manager tự phán)
                    scene6Manager?.OnAmbulancePassed();
                }
                currentWaypointIndex++;
            }

            yield return null;
        }

        // Đến waypoint cuối → dừng và tắt
        isMoving = false;
        if (sirenAudio != null) sirenAudio.Stop();
        StartCoroutine(FadeOutAndDisable());
    }

    // =========================================================================
    private IEnumerator FlashLightsRoutine()
    {
        bool redOn = true;
        while (isMoving || isBlocked)
        {
            SetLights(redOn, !redOn);
            redOn = !redOn;
            yield return new WaitForSeconds(flashInterval);
        }
        SetLights(false, false);
    }

    private void SetLights(bool red, bool blue)
    {
        if (redLight1 != null) redLight1.SetActive(red);
        if (redLight2 != null) redLight2.SetActive(red);
        if (blueLight1 != null) blueLight1.SetActive(blue);
        if (blueLight2 != null) blueLight2.SetActive(blue);
    }

    private IEnumerator FadeOutAndDisable()
    {
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
    }

    // =========================================================================
    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
