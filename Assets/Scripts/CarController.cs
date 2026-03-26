using UnityEngine;

public class CarController : MonoBehaviour
{
    public float speed = 15f; 
    public float turnSpeed = 100f;

    void Update()
    {
        float vertical = Input.GetAxis("Vertical");   // W, S hoặc Mũi tên lên/xuống
        float horizontal = Input.GetAxis("Horizontal"); // A, D hoặc Mũi tên trái/phải

        float move = vertical * speed * Time.deltaTime;
        
        // Tính toán hướng xoay: Nếu đang lùi (vertical < 0), hướng xoay sẽ đảo ngược
        float direction = (vertical < 0) ? -1f : 1f;
        float turn = horizontal * turnSpeed * Time.deltaTime * direction;

        // Di chuyển tiến/lùi
        transform.Translate(0, 0, move);
        
        // Chỉ xoay khi xe đang thực sự di chuyển
        if (Mathf.Abs(move) > 0.01f) 
        {
            transform.Rotate(0, turn, 0);
        }
    }
}