using UnityEngine;

public class WrongWayZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Khi xe ô tô của người chơi chạm vào vùng cấm
        if (other.CompareTag("Player"))
        {
            VR_CarController car = other.GetComponent<VR_CarController>();
            if (car != null)
            {
                // Gọi án phạt
                car.TriggerWrongWay();
            }
        }
    }
}