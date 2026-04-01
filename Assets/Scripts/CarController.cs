using UnityEngine;
using UnityEngine.InputSystem; // Thư viện nhận nút bấm VR
using TMPro;

public class VR_CarController : MonoBehaviour
{
    [Header("Cài đặt Di chuyển & Vật lý")]
    public float maxSpeed = 20f;       // Tốc độ tối đa xe có thể đạt được
    public float acceleration = 5f;    // Lực ga (Tăng tốc từ từ khi giữ W)
    public float brakingForce = 15f;   // Lực phanh (Giảm tốc cực nhanh khi giữ S)
    public float friction = 2f;        // Ma sát (Xe tự trôi chậm lại khi nhả tay ra)
    public float turnSpeed = 50f;      // Tốc độ bẻ lái

    [Header("Gắn hiển thị Chữ (TMP)")]
    public TextMeshProUGUI gearDisplay;

    [Header("Gắn nút trên tay cầm VR")]
    public InputActionReference buttonD; // Nút A
    public InputActionReference buttonR; // Nút B
    public InputActionReference buttonN; // Nút Grip

    private string currentGear = "N";
    private float currentSpeed = 0f;   // Biến lưu trữ tốc độ thực tế của xe

    void Start()
    {
        UpdateGearDisplay();
    }

    void Update()
    {
        // 1. NHẬN LỆNH ĐỔI SỐ (Dùng nút tay cầm VR HOẶC dùng phím số 1, 2, 3 để test trên máy)
        if ((buttonD != null && buttonD.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha2)) SetGearD();
        if ((buttonR != null && buttonR.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha3)) SetGearR();
        if ((buttonN != null && buttonN.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha1)) SetGearN();

        // 2. LẤY TÍN HIỆU GA/PHANH (Tay cầm VR hoặc phím W S A D)
        float joystickVertical = Input.GetAxisRaw("Vertical");
        float joystickHorizontal = Input.GetAxisRaw("Horizontal");

        // 3. XỬ LÝ GIA TỐC VÀ PHANH (Chỉ tính toán khi xe đã vào số D hoặc R)
        if (currentGear != "N")
        {
            if (joystickVertical > 0) // Đạp ga (Giữ W hoặc đẩy Joystick lên)
            {
                currentSpeed += acceleration * Time.deltaTime; // Tăng tốc từ từ
            }
            else if (joystickVertical < 0) // Đạp phanh (Giữ S hoặc kéo Joystick xuống)
            {
                currentSpeed -= brakingForce * Time.deltaTime; // Giảm tốc độ cực nhanh
            }
            else // Nhả ga (Không bấm gì)
            {
                currentSpeed -= friction * Time.deltaTime; // Xe tự động trôi chậm lại do ma sát
            }
        }
        else
        {
            // Nếu xe đang ở số N (Mo), vòng tua máy rỗng, xe tự giảm tốc đến khi dừng hẳn
            currentSpeed -= friction * Time.deltaTime;
        }

        // Ép tốc độ: Không bao giờ được chạy lùi (dưới 0) và không vượt quá giới hạn tối đa
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        // 4. ÁP DỤNG DI CHUYỂN DỰA VÀO SỐ VÀ TỐC ĐỘ ĐÃ TÍNH
        if (currentGear != "N")
        {
            // Xác định hướng đi: D là tiến (1), R là lùi (-1)
            float moveDirection = (currentGear == "D") ? 1f : -1f;
            transform.Translate(0, 0, moveDirection * currentSpeed * Time.deltaTime);

            // 5. XỬ LÝ ĐÁNH LÁI (Chỉ cho phép bẻ vô lăng khi xe đang lăn bánh để mô phỏng đời thực)
            if (currentSpeed > 0.1f)
            {
                // Khi lùi (R) thì đảo chiều trục lái
                float turnDir = (currentGear == "R") ? -1f : 1f;
                transform.Rotate(0, joystickHorizontal * turnSpeed * turnDir * Time.deltaTime, 0);
            }
        }
    }

    // --- CÁC HÀM XỬ LÝ SỐ ---
    public void SetGearN() { currentGear = "N"; UpdateGearDisplay(); }
    public void SetGearD() { currentGear = "D"; UpdateGearDisplay(); }
    public void SetGearR() { currentGear = "R"; UpdateGearDisplay(); }

    void UpdateGearDisplay()
    {
        if (gearDisplay != null)
        {
            gearDisplay.text = currentGear;
            if (currentGear == "D") gearDisplay.color = Color.green;
            else if (currentGear == "R") gearDisplay.color = Color.red;
            else gearDisplay.color = Color.white;
        }
    }
}