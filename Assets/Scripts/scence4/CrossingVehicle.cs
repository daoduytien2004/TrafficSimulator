using UnityEngine;
using System.Collections; // Thêm thư viện này để đếm thời gian

public class CrossingVehicle : MonoBehaviour
{
    [Tooltip("Tốc độ xe máy băng qua ngã tư")]
    public float speed = 15f;

    private bool isMoving = false;

    void Start()
    {
        // Khi mới vào game, xe máy sẽ tự động tàng hình ẩn đi
        gameObject.SetActive(false);
    }

    // --- BÌNH MỚI: HÀM GỌI XE MÁY ĐẾN ĐIỂM MỚI ---
    public void StartCrossing(Vector3 startPos, Quaternion startRot)
    {
        // 1. Dịch chuyển xe đến vị trí ngã tư mới
        transform.position = startPos;
        transform.rotation = startRot;

        // 2. Hiện hình xe lên và cho vặn ga
        gameObject.SetActive(true);
        isMoving = true;

        // 3. Reset lại bộ đếm giờ (Xóa các lệnh tắt xe cũ nếu có)
        StopAllCoroutines();

        // 4. Bắt đầu đếm ngược 7 giây để ẩn xe
        StartCoroutine(HideAfterWait());
    }

    void Update()
    {
        if (isMoving)
        {
            // Liên tục phóng thẳng
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    // BỘ ĐẾM GIỜ TÀNG HÌNH
    IEnumerator HideAfterWait()
    {
        yield return new WaitForSeconds(7f); // Đi hết 7 giây (qua khỏi ngã tư)
        isMoving = false; // Bóp phanh
        gameObject.SetActive(false); // Tàng hình đi, đợi ngã tư tiếp theo gọi
    }
}