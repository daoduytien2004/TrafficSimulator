using UnityEngine;

public class SolidLineZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Khi xe ô tô chạm vào vạch
        if (other.CompareTag("Player"))
        {
            VR_CarController car = other.GetComponent<VR_CarController>();
            if (car != null)
            {
                car.TriggerSolidLineViolation();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Khi xe ô tô đã lách ra khỏi vạch
        if (other.CompareTag("Player"))
        {
            VR_CarController car = other.GetComponent<VR_CarController>();
            if (car != null)
            {
                car.ResetLineTouch();
            }
        }
    }
}