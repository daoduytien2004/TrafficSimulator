using UnityEngine;

public class FloatingIndicator : MonoBehaviour
{
    public float floatAmplitude = 0.5f; // Quãng đường nảy lên xuống (mét)
    public float floatFrequency = 4f;   // Tốc độ nảy nhanh hay chậm

    private Vector3 startPos;

    // Dùng OnEnable để mỗi khi biển báo được bật lên, nó sẽ lấy lại đúng vị trí gốc
    void OnEnable()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Tính toán vị trí Y mới theo hàm Sin để tạo độ nhấp nhô mượt mà
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;

        // Cập nhật vị trí
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}