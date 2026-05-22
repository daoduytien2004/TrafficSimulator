using UnityEngine;

/// <summary>
/// Zone kích hoạt kịch bản xe cấp cứu.
///
/// Đặt trigger này trên làn đường phía sau player.
/// Khi xe cấp cứu vào zone → báo Scene6Manager bắt đầu kịch bản.
///
/// Cũng dùng làm IntersectionZone: đặt BoxCollider bao toàn bộ ngã tư,
/// set isIntersectionZone = true → khi player vào = vượt đèn đỏ.
/// </summary>
public class EmergencyVehicleTrigger : MonoBehaviour
{
    [Header("Chế độ")]
    [Tooltip("true = zone phát hiện xe cấp cứu (đặt phía sau player)\nfalse = zone phát hiện player vượt ngã tư")]
    public bool isAmbulanceTrigger = true;

    [Header("Tham chiếu")]
    public Scene6Manager scene6Manager;

    [Header("Debug")]
    public bool showGizmos = true;

    private bool triggered = false;

    // =========================================================================
    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (isAmbulanceTrigger)
        {
            // Zone tại vạch dừng — player đi vào đây = bắt đầu kịch bản
            if (other.CompareTag("Player"))
            {
                triggered = true;
                scene6Manager?.OnAmbulanceApproaching();
            }
        }
        else
        {
            // Zone ngã tư — player đi vào đây = vượt đèn đỏ
            if (other.CompareTag("Player"))
            {
                triggered = true;
                scene6Manager?.OnPlayerRanRedLight();
            }
        }
    }

    // =========================================================================
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Color c = isAmbulanceTrigger
            ? new Color(1f,0.2f, 0.2f, 0.25f)   // đỏ = trigger cấp cứu
            : new Color(1f, 0.6f, 0f, 0.25f);     // cam = ngã tư

        Gizmos.color = c;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);

        c.a = 0.9f;
        Gizmos.color = c;
        Gizmos.DrawWireCube(col.center, col.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
