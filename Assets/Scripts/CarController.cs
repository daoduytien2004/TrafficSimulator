using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class VR_CarController : MonoBehaviour
{
    [Header("Cài đặt Di chuyển & Vật lý")]
    public float maxSpeed = 20f;
    public float acceleration = 5f;
    public float brakingForce = 15f;
    public float friction = 2f;
    public float turnSpeed = 50f;

    [Header("Gắn UI Hướng dẫn, Taplo & Tai Nạn")]
    public TextMeshProUGUI gearDisplay;
    public TextMeshProUGUI speedDisplay; // Hiển thị "Vận tốc / Giới hạn"
    public Slider speedSlider; // Thanh kim đồng hồ/thanh trượt vận tốc
    public GameObject tutorialPanel;
    public GameObject accidentPanel;
    public GameObject speedingFailPanel; // Panel hiện ra khi bị phạt tốc độ
    public GameObject crosswalkFailPanel; // Panel hiện ra khi đè vạch người đi bộ
    public float resetDelay = 3f;

    [Header("Tốc độ Giới hạn")]
    public float currentSpeedLimit = 0f; // 0 nghĩa là chưa đi qua vùng giới hạn nào

    [Header("Gắn nút trên tay cầm VR")]
    public InputActionReference buttonD;
    public InputActionReference buttonR;
    public InputActionReference buttonN;

    [Header("Âm thanh Hệ thống")]
    public AudioSource engineAudio;
    public AudioClip startupClip;
    public AudioClip idleClip;
    public float overlapTime = 0.5f;
    public AudioSource gasAudio;
    public AudioSource brakeAudio;
    public float minPitch = 0.8f;
    public float maxPitch = 2.5f;

    [Header("Xin Nhan & Gương VR")]
    public Transform vrHeadset;
    public Transform leftMirror;
    public Transform rightMirror;

    [Header("Âm thanh Tai nạn")]
    public AudioSource crashAudioSource; // Loa phát tiếng đâm xe

    [Header("xin nhan")]
    // --- BÌNH MỚI: BIẾN CHO ĐÈN VÀ TIẾNG XI-NHAN ---
    public GameObject leftSignalLight;  // Gắn đèn mũi tên trái
    public GameObject rightSignalLight; // Gắn đèn mũi tên phải
    public AudioSource signalAudio;     // Gắn cái Loa tiếng "tạch"
    public float blinkInterval = 0.5f;  // Tốc độ nhấp nháy (0.5 giây)

    public bool isLeftSignalOn = false;
    public bool isRightSignalOn = false;
    public bool hasLookedLeftMirror = false;
    public bool hasLookedRightMirror = false;

    private float blinkTimer = 0f;
    private bool isLightOn = false;
    // ----------------------------------------------

    private string currentGear = "N";
    private float currentSpeed = 0f;
    private bool isCrashed = false;

    void Start()
    {
        Time.timeScale = 1f;
        UpdateGearDisplay();
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (accidentPanel != null) accidentPanel.SetActive(false);
        if (engineAudio != null && startupClip != null && idleClip != null) StartCoroutine(StartEngineRoutine());

        // Đảm bảo đèn tắt lúc mới vào game
        if (leftSignalLight != null) leftSignalLight.SetActive(false);
        if (rightSignalLight != null) rightSignalLight.SetActive(false);
    }

    IEnumerator StartEngineRoutine()
    {
        engineAudio.clip = startupClip;
        engineAudio.loop = false;
        engineAudio.Play();
        float waitTime = startupClip.length - overlapTime;
        if (waitTime < 0) waitTime = 0;
        yield return new WaitForSeconds(waitTime);
        engineAudio.clip = idleClip;
        engineAudio.loop = true;
        engineAudio.Play();
    }

    void Update()
    {
        if (isCrashed) return;

        // -- BẬT TẮT XIN NHAN BẰNG PHÍM (Có logic tắt trừ chéo) --
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isLeftSignalOn = !isLeftSignalOn;
            if (isLeftSignalOn) isRightSignalOn = false; // Bật trái thì tự tắt phải
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            isRightSignalOn = !isRightSignalOn;
            if (isRightSignalOn) isLeftSignalOn = false; // Bật phải thì tự tắt trái
        }

        // -- CHẠY HIỆU ỨNG NHẤP NHÁY & ÂM THANH --
        HandleTurnSignals();

        // -- CHECK NHÌN GƯƠNG --
        CheckLookingAtMirrors();

        // -- LOGIC LÁI XE --
        if ((buttonD != null && buttonD.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha2)) SetGearD();
        if ((buttonR != null && buttonR.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha3)) SetGearR();
        if ((buttonN != null && buttonN.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha1)) SetGearN();

        float joystickVertical = Input.GetAxisRaw("Vertical");
        float joystickHorizontal = Input.GetAxisRaw("Horizontal");

        if (currentGear != "N")
        {
            if (joystickVertical > 0)
            {
                currentSpeed += acceleration * Time.deltaTime;
                if (gasAudio != null && !gasAudio.isPlaying) gasAudio.Play();
                if (brakeAudio != null && brakeAudio.isPlaying) brakeAudio.Stop();
            }
            else if (joystickVertical < 0)
            {
                if (currentSpeed > 1f && brakeAudio != null && !brakeAudio.isPlaying) brakeAudio.Play();
                currentSpeed -= brakingForce * Time.deltaTime;
                if (gasAudio != null && gasAudio.isPlaying) gasAudio.Stop();
            }
            else
            {
                currentSpeed -= friction * Time.deltaTime;
                if (gasAudio != null && gasAudio.isPlaying) gasAudio.Stop();
                if (brakeAudio != null && brakeAudio.isPlaying && currentSpeed < 1f) brakeAudio.Stop();
            }
        }
        else
        {
            currentSpeed -= friction * Time.deltaTime;
            if (gasAudio != null && gasAudio.isPlaying) gasAudio.Stop();
            if (brakeAudio != null && brakeAudio.isPlaying) brakeAudio.Stop();
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        // --- CẬP NHẬT THANH SLIDER ĐỒNG HỒ ---
        if (speedSlider != null)
        {
            speedSlider.maxValue = maxSpeed;
            speedSlider.value = currentSpeed;
        }

        // --- CẬP NHẬT HIỂN THỊ TỐC ĐỘ / GIỚI HẠN ---
        if (speedDisplay != null)
        {
            int displaySpeedKmH = Mathf.RoundToInt(currentSpeed);
            
            if (currentSpeedLimit > 0)
            {
                // Đã vào vùng giới hạn tốc độ
                speedDisplay.text = $"{displaySpeedKmH} / {Mathf.RoundToInt(currentSpeedLimit)} km/h";

                // Thất bại nếu vượt 5km/h
                if (displaySpeedKmH >= currentSpeedLimit + 5)
                {
                    TriggerSpeedingFailure();
                }
                // Đổi màu đỏ nếu vượt tốc độ nhưng chưa tới 5km/h
                else if (displaySpeedKmH > currentSpeedLimit)
                {
                    speedDisplay.color = Color.red;
                }
                else
                {
                    speedDisplay.color = Color.white;
                }
            }
            else
            {
                // Chưa có giới hạn, chỉ hiển thị vận tốc hiện tại
                speedDisplay.text = $"{displaySpeedKmH} km/h";
                speedDisplay.color = Color.white;
            }
        }

        if (currentGear != "N")
        {
            float moveDirection = (currentGear == "D") ? 1f : -1f;
            transform.Translate(0, 0, moveDirection * currentSpeed * Time.deltaTime);

            if (currentSpeed > 0.1f)
            {
                float turnDir = (currentGear == "R") ? -1f : 1f;
                transform.Rotate(0, joystickHorizontal * turnSpeed * turnDir * Time.deltaTime, 0);
            }
        }

        if (engineAudio != null && engineAudio.clip == idleClip)
        {
            float speedRatio = currentSpeed / maxSpeed;
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
        }
    }

    // --- HÀM XỬ LÝ NHẤP NHÁY XI-NHAN ---
    void HandleTurnSignals()
    {
        // Nếu có bật 1 trong 2 bên
        if (isLeftSignalOn || isRightSignalOn)
        {
            blinkTimer += Time.deltaTime; // Bộ đếm thời gian chạy

            // Đủ 0.5 giây thì đảo trạng thái đèn (Bật thành Tắt, Tắt thành Bật)
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f; // Reset đếm lại
                isLightOn = !isLightOn;

                // Nếu đèn vừa Bật sáng -> Phát tiếng "Tạch"
                if (isLightOn && signalAudio != null)
                {
                    signalAudio.Play();
                }
            }
        }
        else // Nếu tắt cả 2 xi-nhan
        {
            isLightOn = false;
            blinkTimer = 0f;
        }

        // Áp dụng trạng thái tắt/bật cho đúng cái bóng đèn đang được chọn
        if (leftSignalLight != null) leftSignalLight.SetActive(isLeftSignalOn && isLightOn);
        if (rightSignalLight != null) rightSignalLight.SetActive(isRightSignalOn && isLightOn);
    }

    void CheckLookingAtMirrors()
    {
        if (vrHeadset == null) return;
        if (leftMirror != null)
        {
            Vector3 dirToLeft = (leftMirror.position - vrHeadset.position).normalized;
            if (Vector3.Angle(vrHeadset.forward, dirToLeft) < 20f) hasLookedLeftMirror = true;
        }
        if (rightMirror != null)
        {
            Vector3 dirToRight = (rightMirror.position - vrHeadset.position).normalized;
            if (Vector3.Angle(vrHeadset.forward, dirToRight) < 20f) hasLookedRightMirror = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Road") || isCrashed) return;
        if (currentSpeed < 2f && !collision.gameObject.CompareTag("EnemyMoto")) return;

        isCrashed = true;

        if (crashAudioSource != null) crashAudioSource.Play();

        // Bị tai nạn thì tắt hết luôn xi nhan và tiếng
        isLeftSignalOn = false;
        isRightSignalOn = false;
        if (leftSignalLight != null) leftSignalLight.SetActive(false);
        if (rightSignalLight != null) rightSignalLight.SetActive(false);

        if (gasAudio != null) gasAudio.Stop();
        if (brakeAudio != null) brakeAudio.Stop();
        if (engineAudio != null) engineAudio.Stop();
        if (accidentPanel != null) accidentPanel.SetActive(true);
        Time.timeScale = 0f;
        StartCoroutine(ResetGameRoutine());
    }

    IEnumerator ResetGameRoutine()
    {
        yield return new WaitForSecondsRealtime(resetDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void TriggerSpeedingFailure()
    {
        if (isCrashed) return;
        isCrashed = true;

        // Bật panel phạt tốc độ (nếu chưa gán thì bật tạm panel tai nạn)
        if (speedingFailPanel != null) speedingFailPanel.SetActive(true);
        else if (accidentPanel != null) accidentPanel.SetActive(true);

        if (gasAudio != null) gasAudio.Stop();
        if (brakeAudio != null) brakeAudio.Stop();
        if (engineAudio != null) engineAudio.Stop();
        
        Time.timeScale = 0f;
        StartCoroutine(ResetGameRoutine());
    }

    public void TriggerCrosswalkFailure()
    {
        if (isCrashed) return;
        isCrashed = true;

        if (crosswalkFailPanel != null) crosswalkFailPanel.SetActive(true);
        else if (accidentPanel != null) accidentPanel.SetActive(true);

        if (gasAudio != null) gasAudio.Stop();
        if (brakeAudio != null) brakeAudio.Stop();
        if (engineAudio != null) engineAudio.Stop();
        
        Time.timeScale = 0f;
        StartCoroutine(ResetGameRoutine());
    }

    public void SetGearN() { currentGear = "N"; UpdateGearDisplay(); }
    public void SetGearD() { currentGear = "D"; UpdateGearDisplay(); if (tutorialPanel != null) tutorialPanel.SetActive(false); }
    public void SetGearR() { currentGear = "R"; UpdateGearDisplay(); if (tutorialPanel != null) tutorialPanel.SetActive(false); }

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