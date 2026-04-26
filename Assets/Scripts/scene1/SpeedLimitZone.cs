using UnityEngine;

public class SpeedLimitZone : MonoBehaviour
{
    [Header("Cài đặt Giới hạn Tốc độ (km/h)")]
    public float speedLimit = 60f;

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem đối tượng va chạm có phải là xe của người chơi không
        // Giả sử xe có script VR_CarController gắn trên chính nó hoặc parent
        VR_CarController car = other.GetComponentInParent<VR_CarController>();

        if (car != null)
        {
            // Cập nhật tốc độ giới hạn cho xe
            car.currentSpeedLimit = speedLimit;
            Debug.Log($"[SpeedLimitZone] Xe đã vào đoạn đường giới hạn tốc độ: {speedLimit} km/h");
        }
    }
}
