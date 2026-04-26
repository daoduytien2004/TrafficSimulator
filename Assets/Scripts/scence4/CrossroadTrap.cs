using UnityEngine;

public class CrossroadTrap : MonoBehaviour
{
    [Header("Cài đặt Bẫy Ngã Tư (TÁI SỬ DỤNG)")]
    [Tooltip("Kéo CHIẾC XE MÁY ĐANG CÓ TRÊN CỘT HIERARCHY vào đây")]
    public CrossingVehicle reusedVehicle;
    public Transform spawnPoint;

    [Tooltip("Bù trừ độ cao nếu lốp xe bị cạ đất (mặc định để 0)")]
    public float groundOffset = 0f;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;

            if (reusedVehicle != null && spawnPoint != null)
            {
                Vector3 finalSpawnPos = spawnPoint.position;

                // --- NÂNG CẤP LASER DÒ ĐƯỜNG ---
                // Nâng tâm bắn lên hẳn 10 mét cho chắc ăn
                Vector3 rayStart = spawnPoint.position + Vector3.up * 10f;
                RaycastHit hit;

                // Bắn tia dài 50 mét. 
                // Thêm QueryTriggerInteraction.Ignore để tia laser XUYÊN QUA mọi khối tàng hình, chỉ chạm vật thể cứng (mặt đường)
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    finalSpawnPos.y = hit.point.y + groundOffset;
                }
                else
                {
                    Debug.LogWarning("Tia laser không tìm thấy mặt đường tại: " + gameObject.name + ". Hãy kiểm tra xem điểm neo có bị đặt ở ngoài map không!");
                }
                // --------------------------------

                reusedVehicle.StartCrossing(finalSpawnPos, spawnPoint.rotation);
            }
            else
            {
                Debug.LogWarning("Chưa gắn Xe hoặc Điểm neo tại bẫy: " + gameObject.name);
            }
        }
    }
}