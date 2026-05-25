using UnityEngine;
using FCG;

public class RedLightDetector : MonoBehaviour
{
    [Header("Đèn Giao Thông")]
    [Tooltip("Kéo cột đèn giao thông (có script TrafficLight) quản lý ngã tư này vào đây.")]
    public TrafficLight trafficLight;

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem đối tượng đi vào có phải xe của người chơi không
        VR_CarController car = other.GetComponentInParent<VR_CarController>();
        if (car != null)
        {
            // Nếu có gán đèn và đèn đang ở trạng thái màu ĐỎ
            if (trafficLight != null && trafficLight.Red.activeSelf)
            {
                Debug.LogWarning($"[TrafficLight] VI PHẠM: Xe người chơi vượt đèn đỏ tại {gameObject.name}!");
                car.TriggerRedLightFailure();
            }
        }
    }
}
