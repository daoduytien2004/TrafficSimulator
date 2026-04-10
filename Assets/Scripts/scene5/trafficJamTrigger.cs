using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FCG;

/// <summary>
/// TrafficJamTrigger v3 — tự động tìm xe NPC trong bán kính zone.
/// Không cần kéo tay xe NPC vào Inspector.
/// Gắn vào GameObject rỗng giữa đường, thêm BoxCollider (Is Trigger = true).
/// </summary>
public class TrafficJamTrigger : MonoBehaviour
{
    [Header("Tham chiếu")]
    public VR_CarController_Scene5 carController;

    [Header("Vùng tìm xe NPC")]
    [Tooltip("Bán kính (m) tìm xe NPC xung quanh zone. Chỉnh to nếu đường rộng.")]
    public float detectionRadius = 25f;

    [Tooltip("Chỉ dừng xe NPC đang đi cùng chiều với player")]
    public bool onlyStopNearbyLane = true;

    [Header("Thời gian")]
    public float jamDuration = 30f;

    [Header("Đèn giao thông (tuỳ chọn)")]
    public GameObject redLight;
    public GameObject greenLight;

    [Header("Debug")]
    public bool showGizmos = true;

    private bool triggered = false;
    private List<TrafficCar> stoppedCars = new List<TrafficCar>();

    // =========================================================================
    void Start()
    {
        if (redLight != null) redLight.SetActive(false);
        if (greenLight != null) greenLight.SetActive(true);
    }

    // =========================================================================
    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        stoppedCars = FindNearbyNPCs();
        Debug.Log($"[TrafficJam] Tim thay {stoppedCars.Count} xe NPC trong ban kinh {detectionRadius}m");

        foreach (var car in stoppedCars)
            car.ForceStop();

        if (redLight != null) redLight.SetActive(true);
        if (greenLight != null) greenLight.SetActive(false);

        if (carController != null) carController.EnterJamZone();

        StartCoroutine(JamRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (carController != null) carController.ExitJamZone();
    }

    // =========================================================================
    private List<TrafficCar> FindNearbyNPCs()
    {
        var result = new List<TrafficCar>();
        var allCars = FindObjectsByType<TrafficCar>(FindObjectsSortMode.None);

        foreach (var car in allCars)
        {
            float dist = Vector3.Distance(transform.position, car.transform.position);
            if (dist > detectionRadius) continue;

            if (onlyStopNearbyLane && carController != null)
            {
                Vector3 carForward = car.transform.forward;
                Vector3 playerForward = carController.transform.forward;
                float dot = Vector3.Dot(carForward, playerForward);
                if (dot < 0.3f) continue;
            }

            result.Add(car);
        }

        return result;
    }

    // =========================================================================
    private IEnumerator JamRoutine()
    {
        float elapsed = 0f;
        while (elapsed < jamDuration)
        {
            elapsed += 2f;
            yield return new WaitForSeconds(2f);

            // Bắt thêm xe mới lái vào zone trong lúc đang kẹt
            var newCars = FindNearbyNPCs();
            foreach (var car in newCars)
            {
                if (!stoppedCars.Contains(car))
                {
                    car.ForceStop();
                    stoppedCars.Add(car);
                    Debug.Log($"[TrafficJam] Xe moi vao zone: {car.name}");
                }
            }
        }

        // Thong duong
        foreach (var car in stoppedCars)
            if (car != null) car.ForceResume();

        stoppedCars.Clear();

        if (redLight != null) redLight.SetActive(false);
        if (greenLight != null) greenLight.SetActive(true);

        if (carController != null) carController.ExitJamZone();

        Debug.Log("[TrafficJam] Duong da thong!");
    }

    // =========================================================================
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
            Gizmos.DrawWireCube(col.center, col.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        // Vong tron vang = vung tim xe NPC
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.12f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}