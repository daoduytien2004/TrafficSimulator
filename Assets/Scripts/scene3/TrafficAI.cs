using UnityEngine;

public class TrafficAI : MonoBehaviour
{
    [Header("Cài đặt tốc độ")]
    public float moveSpeed = 10f; // Tốc độ chạy thẳng
    public float turnSpeed = 5f;  // Tốc độ cua rẽ

    [Header("Đường đi (Kéo các điểm Waypoint vào đây)")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    void Update()
    {
        // Nếu chưa gắn điểm nào thì xe đứng im
        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // 1. TÍNH TOÁN GÓC QUAY VÀ TỰ ĐỘNG BẺ LÁI VỀ PHÍA ĐIỂM ĐẾN
        Vector3 directionToTarget = targetWaypoint.position - transform.position;

        // Bỏ qua trục Y (độ cao) để xe không bị ngóc đầu lên trời hoặc cắm xuống đất
        directionToTarget.y = 0;

        // 1. TÍNH TOÁN GÓC QUAY (Thêm dấu trừ vào trước directionToTarget để lật ngược trục Z)
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // 2. DI CHUYỂN (Đổi Vector3.forward thành Vector3.back để xe chạy về hướng đầu xe)
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        // 3. KIỂM TRA XEM ĐÃ ĐẾN NƠI CHƯA ĐỂ CHUYỂN ĐIỂM TIẾP THEO
        // Nếu khoảng cách từ xe đến điểm waypoint nhỏ hơn 2 mét -> Đổi mục tiêu
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 2f)
        {
            currentWaypointIndex++;

            // Nếu đi hết danh sách thì quay lại điểm số 0 (chạy vòng tròn)
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }
        }
    }

    // Hàm phụ trợ: Vẽ các đường line màu đỏ trong Scene để anh dễ nhìn thấy đường đi của xe
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 0.5f); // Vẽ cục tròn đỏ ở mỗi điểm

                // Vẽ đường kẻ nối các điểm lại với nhau
                if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                else if (waypoints.Length > 1 && waypoints[0] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
            }
        }
    }
}