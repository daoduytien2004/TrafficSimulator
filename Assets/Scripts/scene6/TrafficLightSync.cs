using System.Collections;
using UnityEngine;
using FCG;

/// <summary>
/// Gắn vào IntersectionZone — đồng bộ Collider với trạng thái đèn FCG.
///
/// Quy tắc:
///   Đèn ĐỎ  → Collider.enabled = true  (bắt đầu detect vi phạm)
///   Đèn XANH → Collider.enabled = false (NPC và player qua tự do)
///   Đèn VÀNG → Collider.enabled = false (không phạt)
///
/// KHÔNG dùng SetActive (tắt GameObject sẽ kill script này).
/// Dùng Collider.enabled thay thế — NPC raycast cũng không detect được.
/// </summary>
public class TrafficLightSync : MonoBehaviour
{
    [Header("Chế độ 1 — Manual (khuyến nghị, chính xác hơn)")]
    [Tooltip("Kéo component TrafficLight (FCG) của đèn PHÍA TRƯỚC xe player vào đây.\n" +
             "Tìm trong Hierarchy: TL-Japan → Traffic_Lights-Japan (N/S/E/W)\n" +
             "Để trống nếu muốn dùng chế độ Auto bên dưới.")]
    public TrafficLight facingLight;

    [Header("Chế độ 2 — Auto Raycast (fallback khi facingLight trống)")]
    [Tooltip("Transform của xe player — dùng để bắn raycast về phía trước tìm StopCollider FCG")]
    public Transform playerTransform;
    [Tooltip("Khoảng cách raycast tìm đèn (m)")]
    public float detectDistance = 15f;

    [Header("Giả lập / Debug")]
    [Tooltip("Khoá đèn ĐỎ cố định — tắt chu kỳ FCG để test kịch bản.\n" +
             "Cần gán trafficLightsController bên dưới để dừng được chu kỳ.")]
    public bool forceRedLight = false;
    [Tooltip("Kéo GameObject TL-Japan (có TrafficLights2 script) vào đây.\n" +
             "Dùng để dừng chu kỳ tự động khi bật forceRedLight.")]
    public TrafficLights2 trafficLightsController;
    [Tooltip("Vẽ ray trong Scene view khi đang chạy")]
    public bool showDebugRay = false;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private Collider zoneCollider;
    private bool lastState = true;
    private bool cycleWasStopped = false;

    // =========================================================================
    void Start()
    {
        zoneCollider = GetComponent<Collider>();

        if (zoneCollider == null)
        {
            Debug.LogError("[TrafficLightSync] Không tìm thấy Collider trên " + gameObject.name);
            enabled = false;
            return;
        }

        if (facingLight == null && playerTransform == null)
            Debug.LogWarning("[TrafficLightSync] Chưa gán facingLight lẫn playerTransform — zone sẽ luôn bật.");

        StartCoroutine(InitAndWatch());
    }

    private IEnumerator InitAndWatch()
    {
        yield return null; // chờ FCG Start() chạy xong

        // Áp dụng forceRed ngay khi khởi động nếu được bật sẵn
        if (forceRedLight)
            ApplyForceRed();

        // Force apply trạng thái ban đầu
        lastState = !IsRedLight();
        ApplyState(IsRedLight());

        // Vòng lặp theo dõi — 10 lần/giây
        while (true)
        {
            // Phát hiện forceRedLight được bật/tắt trong lúc chạy (hữu ích khi test)
            if (forceRedLight && !cycleWasStopped)
                ApplyForceRed();

            bool currentlyRed = IsRedLight();

            if (currentlyRed != lastState)
            {
                lastState = currentlyRed;
                ApplyState(currentlyRed);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // ── Force đèn đỏ ─────────────────────────────────────────────────────────

    private void ApplyForceRed()
    {
        if (trafficLightsController != null)
        {
            // CancelInvoke dừng hẳn InvokeRepeating của FCG
            // (enabled = false KHÔNG dừng được InvokeRepeating)
            trafficLightsController.CancelInvoke();
            trafficLightsController.enabled = false;
            cycleWasStopped = true;
            Debug.Log("[TrafficLightSync] Đã dừng chu kỳ FCG — đèn khoá đỏ.");
        }
        else
        {
            Debug.LogWarning("[TrafficLightSync] forceRedLight=true nhưng trafficLightsController chưa gán — " +
                             "FCG vẫn có thể override đèn. Hãy kéo TL-Japan vào field trafficLightsController.");
        }

        // Khoá đèn đỏ trực tiếp
        if (facingLight != null)
            facingLight.SetStatus("1");
    }

    // ── Áp dụng trạng thái zone ──────────────────────────────────────────────

    private void ApplyState(bool isRed)
    {
        zoneCollider.enabled = isRed;
    }

    // ── Kiểm tra trạng thái đèn ─────────────────────────────────────────────

    private bool IsRedLight()
    {
        // Khi đang force red → luôn trả về true
        if (forceRedLight) return true;

        // Chế độ 1: đọc trực tiếp từ TrafficLight FCG
        if (facingLight != null)
        {
            if (facingLight.Red == null)
            {
                Debug.LogWarning("[TrafficLightSync] facingLight.Red chưa được assign trong TrafficLight!");
                return true;
            }
            return facingLight.Red.activeSelf;
        }

        // Chế độ 2: raycast từ xe player
        if (playerTransform != null)
            return RaycastDetectRed();

        return true; // mặc định an toàn
    }

    private bool RaycastDetectRed()
    {
        Vector3 origin = playerTransform.position + Vector3.up * 0.5f;
        Vector3 direction = playerTransform.forward;
        bool hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, detectDistance)
                   && hitInfo.transform.name == "Stop";

        if (showDebugRay)
            Debug.DrawRay(origin, direction * detectDistance, hit ? Color.red : Color.green);

        return hit;
    }

    // =========================================================================
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null || facingLight != null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerTransform.position + Vector3.up * 0.5f,
                       playerTransform.forward * detectDistance);
    }
#endif
}
