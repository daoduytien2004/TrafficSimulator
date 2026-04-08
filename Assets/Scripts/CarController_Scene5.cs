using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class VR_CarController_Scene5 : MonoBehaviour
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
    public InputActionReference buttonD;       // Nút A → Số D
    public InputActionReference buttonR;       // Nút B → Số R
    public InputActionReference buttonN;       // Nút Grip → Số N
    public InputActionReference buttonLight;   // Nút X / Y → Bật/tắt đèn
    public InputActionReference buttonHorn;    // Nút VR → Bấm còi (vd: Trigger)

    [Header("Đèn xe")]
    public Light headlightLeft;                   // Kéo Light đèn trái vào đây
    public Light headlightRight;                  // Kéo Light đèn phải vào đây
    public KeyCode headlightKeyboard = KeyCode.L; // Phím test trên máy tính

    [Header("Âm thanh")]
    public AudioSource hornAudioSource;           // AudioSource riêng cho còi
    public AudioSource collisionAudioSource;      // AudioSource riêng cho va chạm
    public AudioClip hornClip;                    // File âm thanh còi xe
    public AudioClip collisionClip;               // File âm thanh va chạm
    public KeyCode hornKeyboard = KeyCode.Mouse0; // Chuột trái để test trên máy
    public float collisionForceThreshold = 3f;    // Lực va chạm tối thiểu để phát âm thanh

    // ── Private ──────────────────────────────────────────────────────────────
    private Rigidbody rb;
    private string currentGear = "N";
    private float currentSpeed = 0f;
    private bool headlightsOn = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Collision hoạt động đúng, không bị xuyên qua vật thể
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Khoá trục X và Z để xe không bị lật
        // Khoá trục Y để xe không bay lên khi va chạm mạnh
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;

        // Tắt đèn lúc khởi động
        SetHeadlights(false);

        UpdateGearDisplay();
    }

    void Update()
    {
        // Nhận lệnh đổi số — VR hoặc phím số để test trên máy
        if ((buttonD != null && buttonD.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha2)) SetGearD();
        if ((buttonR != null && buttonR.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha3)) SetGearR();
        if ((buttonN != null && buttonN.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha1)) SetGearN();

        // Bật/tắt đèn — VR hoặc phím L để test trên máy
        if ((buttonLight != null && buttonLight.action.WasPressedThisFrame()) || Input.GetKeyDown(headlightKeyboard))
            ToggleHeadlights();

        // Còi xe — giữ để bóp còi, nhả thì tắt
        HandleHorn();
    }

    void FixedUpdate()
    {
        // Lấy tín hiệu ga/phanh — Joystick VR hoặc phím WASD
        float joystickVertical = Input.GetAxisRaw("Vertical");
        float joystickHorizontal = Input.GetAxisRaw("Horizontal");

        HandleSpeed(joystickVertical);
        HandleMovement();
        HandleSteering(joystickHorizontal);
    }

    // ── Tính tốc độ ──────────────────────────────────────────────────────────

    void HandleSpeed(float verticalInput)
    {
        if (currentGear != "N")
        {
            if (verticalInput > 0)       // Đạp ga
                currentSpeed += acceleration * Time.fixedDeltaTime;
            else if (verticalInput < 0)  // Đạp phanh
                currentSpeed -= brakingForce * Time.fixedDeltaTime;
            else                         // Nhả ga — xe tự trôi
                currentSpeed -= friction * Time.fixedDeltaTime;
        }
        else
        {
            // Số N — xe tự giảm tốc đến khi dừng hẳn
            currentSpeed -= friction * Time.fixedDeltaTime;
        }

        // Không cho tốc độ âm hoặc vượt max
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
    }

    // ── Di chuyển bằng Rigidbody (collision hoạt động đúng) ──────────────────

    void HandleMovement()
    {
        if (currentGear == "N" || currentSpeed <= 0f) return;

        float moveDirection = (currentGear == "D") ? 1f : -1f;

        // Dùng MovePosition thay vì transform.Translate
        Vector3 newPosition = rb.position + transform.forward * moveDirection * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    // ── Đánh lái bằng Rigidbody ──────────────────────────────────────────────

    void HandleSteering(float horizontalInput)
    {
        // Chỉ bẻ lái khi xe đang lăn bánh
        if (currentSpeed <= 0.1f) return;

        // Khi lùi (R) thì đảo chiều trục lái
        float turnDir = (currentGear == "R") ? -1f : 1f;

        float turnAmount = horizontalInput * turnSpeed * turnDir * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);

        // Dùng MoveRotation thay vì transform.Rotate
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    // ── Đèn xe ───────────────────────────────────────────────────────────────

    void ToggleHeadlights()
    {
        SetHeadlights(!headlightsOn);
    }

    void SetHeadlights(bool isOn)
    {
        headlightsOn = isOn;
        if (headlightLeft) headlightLeft.enabled = isOn;
        if (headlightRight) headlightRight.enabled = isOn;
    }

    // ── Còi xe ───────────────────────────────────────────────────────────────

    void HandleHorn()
    {
        if (hornAudioSource == null || hornClip == null) return;

        // Kiểm tra nút VR hoặc chuột trái đang được giữ
        bool hornPressed =
            (buttonHorn != null && buttonHorn.action.IsPressed()) ||
            Input.GetKey(hornKeyboard);

        if (hornPressed)
        {
            // Phát còi liên tục khi giữ nút (loop)
            if (!hornAudioSource.isPlaying)
            {
                hornAudioSource.clip = hornClip;
                hornAudioSource.loop = true;
                hornAudioSource.Play();
            }
        }
        else
        {
            // Dừng còi khi nhả nút
            if (hornAudioSource.isPlaying)
                hornAudioSource.Stop();
        }
    }

    // ── Âm thanh va chạm (tự động khi Rigidbody va chạm) ────────────────────

    void OnCollisionEnter(Collision collision)
    {
        if (collisionAudioSource == null || collisionClip == null) return;

        // Chỉ phát âm thanh nếu lực va chạm đủ mạnh
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < collisionForceThreshold) return;

        // Âm lượng tỉ lệ với lực va chạm (max 1.0)
        float volume = Mathf.Clamp01(impactForce / (collisionForceThreshold * 5f));

        collisionAudioSource.PlayOneShot(collisionClip, volume);
    }

    // ── Xử lý số ─────────────────────────────────────────────────────────────

    public void SetGearN() { currentGear = "N"; UpdateGearDisplay(); }
    public void SetGearD() { currentGear = "D"; UpdateGearDisplay(); }
    public void SetGearR() { currentGear = "R"; UpdateGearDisplay(); }

    void UpdateGearDisplay()
    {
        if (gearDisplay == null) return;

        gearDisplay.text = currentGear;

        if (currentGear == "D") gearDisplay.color = Color.green;
        else if (currentGear == "R") gearDisplay.color = Color.red;
        else gearDisplay.color = Color.white;
    }
}