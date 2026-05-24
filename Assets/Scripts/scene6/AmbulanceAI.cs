using System.Collections;
using UnityEngine;

public class AmbulanceAI : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;
    [Tooltip("Tốc độ di chuyển (m/s). 15 ≈ 54 km/h, 20 ≈ 72 km/h")]
    public float moveSpeed = 15f;
    public float waypointReachDistance = 2.5f;

    [Header("Căn mặt đường")]
    public float groundYOffset = 2.5f;

    [Header("Âm thanh")]
    public AudioSource sirenAudio;
    public AudioClip sirenClip;

    [Header("Đèn nháy")]
    public GameObject redLight1;
    public GameObject redLight2;
    public GameObject blueLight1;
    public GameObject blueLight2;
    public float flashInterval = 0.25f;

    [Header("Logic dừng chờ")]
    [Tooltip("Phát hiện xe cản trong vòng X mét phía trước → dừng chờ")]
    public float stopDistance = 10f;
    [Tooltip("Thời gian (giây) còi hú trước khi xe bắt đầu chạy")]
    public float sirenWarningDelay = 4f;
    [Tooltip("Nếu xe player chắn quá X giây → kết luận không nhường")]
    public float playerBlockedTimeout = 15f;

    [Header("Tham chiếu")]
    public Scene6Manager scene6Manager;

    // ── Private ───────────────────────────────────────────────────────────────
    private Rigidbody rb;
    private Transform playerTransform;

    private int   currentWaypointIndex = 0;
    private bool  hasPassedPlayer      = false;
    private bool  isActive             = false; // bắt đầu di chuyển sau delay
    private float playerBlockedTimer   = 0f;

    // =========================================================================
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic            = false;
        rb.useGravity             = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints            = RigidbodyConstraints.FreezePositionY
                                  | RigidbodyConstraints.FreezeRotationX
                                  | RigidbodyConstraints.FreezeRotationZ;
    }

    void Start()
    {
        if (sirenAudio != null && sirenClip != null)
        {
            sirenAudio.clip         = sirenClip;
            sirenAudio.loop         = true;
            sirenAudio.spatialBlend = 1f;
            sirenAudio.Stop();
        }
        SetLights(false, false);
        gameObject.SetActive(false);
    }

    // =========================================================================
    public void StartApproaching(Transform player)
    {
        playerTransform = player;

        if (waypoints != null && waypoints.Length > 0)
        {
            Vector3 spawnPos = waypoints[0].position;
            spawnPos.y += groundYOffset;
            transform.position = spawnPos;
            transform.rotation = waypoints[0].rotation;
            currentWaypointIndex = 1;
        }

        gameObject.SetActive(true);

        if (sirenAudio != null) sirenAudio.Play();
        StartCoroutine(FlashLightsRoutine());
        StartCoroutine(DelayThenStart());
    }

    private IEnumerator DelayThenStart()
    {
        // Còi hú trước, xe đứng yên
        yield return new WaitForSeconds(sirenWarningDelay);
        isActive = true;
    }

    // =========================================================================
    void FixedUpdate()
    {
        if (!isActive) { rb.linearVelocity = Vector3.zero; return; }
        if (currentWaypointIndex >= waypoints.Length) return;

        Transform target    = waypoints[currentWaypointIndex];
        Vector3   direction = (target.position - transform.position).normalized;

        // ── Kiểm tra có xe nào trong stopDistance phía trước không ──
        bool carAhead = HasCarAhead(out bool isPlayerAhead);

        if (carAhead)
        {
            // Dừng hẳn, không nhúc nhích
            rb.linearVelocity = Vector3.zero;

            // Nếu xe cản là Player → tính giờ chờ
            if (isPlayerAhead && !hasPassedPlayer)
            {
                playerBlockedTimer += Time.fixedDeltaTime;
                if (playerBlockedTimer >= playerBlockedTimeout)
                {
                    isActive = false;
                    scene6Manager?.OnPlayerBlocked();
                }
            }
        }
        else
        {
            // Đường trống → chạy bình thường
            playerBlockedTimer = 0f;
            rb.linearVelocity  = direction * moveSpeed;

            // Xoay về hướng di chuyển
            if (direction != Vector3.zero)
                rb.MoveRotation(Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction), 10f * Time.fixedDeltaTime));

            // Đến waypoint → chuyển tiếp
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist < waypointReachDistance)
            {
                if (currentWaypointIndex == 2 && !hasPassedPlayer)
                {
                    hasPassedPlayer = true;
                    scene6Manager?.OnAmbulancePassed();
                }
                currentWaypointIndex++;

                // Hết waypoint → kết thúc
                if (currentWaypointIndex >= waypoints.Length)
                {
                    isActive = false;
                    rb.linearVelocity = Vector3.zero;
                    if (sirenAudio != null) sirenAudio.Stop();
                    StartCoroutine(FadeOutAndDisable());
                }
            }
        }
    }

    // =========================================================================
    /// <summary>
    /// Bắn 3 tia raycast phía trước trong stopDistance.
    /// Trả về true nếu có xe cản, isPlayerAhead = true nếu xe đó là Player.
    /// </summary>
    private bool HasCarAhead(out bool isPlayerAhead)
    {
        isPlayerAhead = false;

        Vector3 fwd   = transform.forward;
        Vector3 right = transform.right;

        // Bắn ray ở 3 độ cao × 3 vị trí ngang = 9 tia — bao phủ toàn bộ mặt trước xe
        float[] heights = { 0.3f, 0.8f, 1.4f };
        float[] laterals = { 0f, -0.8f, 0.8f };

        foreach (float h in heights)
        {
            foreach (float lat in laterals)
            {
                Vector3 origin = transform.position + Vector3.up * h + right * lat;

                // ~0 = tất cả layer, tránh bỏ sót xe nằm ở layer lạ
                RaycastHit[] hits = Physics.RaycastAll(origin, fwd, stopDistance, ~0);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.transform.root == transform) continue;

                    bool isPlayer = hit.collider.CompareTag("Player") ||
                                    hit.collider.transform.root.CompareTag("Player");

                    // Bỏ qua trigger zone nhưng GIỮ trigger của xe Player
                    if (hit.collider.isTrigger && !isPlayer) continue;

                    if (isPlayer) isPlayerAhead = true;
                    return true;
                }
            }
        }

        return false;
    }

    // ── Đèn & Coroutines ─────────────────────────────────────────────────────
    private IEnumerator FlashLightsRoutine()
    {
        bool redOn = true;
        while (gameObject.activeSelf)
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

    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length - 1; i++)
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);

        // Vẽ tia raycast để debug
        if (!Application.isPlaying) return;
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Gizmos.DrawRay(origin, transform.forward * stopDistance);
        Gizmos.DrawRay(origin - transform.right * 0.8f, transform.forward * stopDistance);
        Gizmos.DrawRay(origin + transform.right * 0.8f, transform.forward * stopDistance);
    }
}
