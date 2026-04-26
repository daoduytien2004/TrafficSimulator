using UnityEngine;
using System.Collections;

public class BlockingCar : MonoBehaviour {
    [Header("Tốc độ xe cản đường")]
    [Tooltip("Tốc độ chậm chạp ban đầu (km/h)")]
    public float annoySpeed = 15f; 
    [Tooltip("Tốc độ khi bỏ chạy (km/h)")]
    public float speedAfterYield = 40f; 

    [Header("Cài đặt Lách xe")]
    public float moveDistance = 2f; 
    [Tooltip("Khoảng cách (mét) xe AI nghe thấy tiếng còi của bạn")]
    public float listenRadius = 25f;
    [Tooltip("Né sang phải nếu true, trái nếu false")]
    public bool moveRight = true;

    [Header("THIẾT LẬP LỘ TRÌNH TỰ ĐỘNG")]
    [Tooltip("Đích đến bạn muốn xe đi tới. Hệ thống AI gốc sẽ đo đường ngã tư ngắn nhất để tới đây! ĐỂ TRỐNG nếu muốn xe đi vòng quanh vô tận.")]
    public Transform endPoint;

    private bool hasMoved = false;
    private FCG.TrafficCar trafficCar;
    private float _currentLateral = 0f;

    void Start()
    {
        trafficCar = GetComponent<FCG.TrafficCar>();
        
        if (trafficCar != null)
        {
            trafficCar.carSetting.limitSpeed = annoySpeed;
            
            // JUMPSTART: Tính năng nổ máy chống đứng im cho Prefab thả tay ngoài map
        if (trafficCar != null)
        {
            if (trafficCar.atualWay == null)
            {
                var allWays = FindObjectsByType<FCG.FCGWaypointsContainer>(FindObjectsSortMode.None);
                float minDist = float.MaxValue;
                FCG.FCGWaypointsContainer bestWay = null;

                foreach (var cw in allWays)
                {
                    if (cw.waypoints == null || cw.waypoints.Count < 2) continue;
                    
                    Vector3 pathDir = (cw.waypoints[cw.waypoints.Count - 1].position - cw.waypoints[0].position).normalized;
                    float alignment = Mathf.Abs(Vector3.Dot(transform.forward, pathDir));

                    if (alignment > 0.3f) 
                    {
                        foreach (var wp in cw.waypoints)
                        {
                            float dist = Vector3.Distance(transform.position, wp.position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                bestWay = cw;
                            }
                        }
                    }
                }
                
                if (bestWay != null)
                {
                    trafficCar.atualWay = bestWay.transform;
                    // Gọi hàm nội bộ để gán lại lốp xe nhằm chống sập nguồn
                    if (trafficCar.wCollider == null || trafficCar.wCollider.Length < 4 || trafficCar.wCollider[0] == null) {
                         trafficCar.Configure();
                    }
                    trafficCar.Init();
                    }
                }
            }
        }
    }

    void Update()
    {
        // Làm hiệu ứng lách sang đường MƯỢT MÀ theo thời gian (nếu bị còi)
        if (trafficCar != null && hasMoved && _currentLateral < moveDistance)
        {
            float maxStep = (moveDistance / 2f) * Time.deltaTime; 
            _currentLateral += maxStep;
            trafficCar.lateralOffset = moveRight ? _currentLateral : -_currentLateral;
        }
    }

    /// <summary>
    /// Được gọi từ CarController_Scene5 khi player bấm còi.
    /// Trả về true nếu xe tiến hành né thành công, false nếu nó đã né rồi.
    /// </summary>
    public bool MoveAside()
    {
        if (hasMoved) return false;
        hasMoved = true;
        
        if (trafficCar != null)
        {
            // Tăng tốc độ rồ ga chạy mất mạng
            trafficCar.carSetting.limitSpeed = speedAfterYield;
            
            // Xóa phanh cưỡng chế 
            trafficCar.ForceResume(); 
        }

        return true;
    }



    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, listenRadius);
        
        Gizmos.color = moveRight ? Color.green : Color.red;
        Vector3 direction = moveRight ? transform.right : -transform.right;
        Gizmos.DrawRay(transform.position, direction * moveDistance);
    }
}
