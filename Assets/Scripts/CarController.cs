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
    public TextMeshProUGUI speedDisplay;
    public Slider speedSlider;
    public GameObject tutorialPanel;
    public GameObject wrongWayPanel;
    public GameObject accidentPanel;
    public GameObject speedingFailPanel; // Panel hiện ra khi bị phạt tốc độ
    public GameObject crosswalkFailPanel; // Panel hiện ra khi đè vạch người đi bộ
    public GameObject ranRedPanel; // Panel hiện ra khi vượt đèn đỏ
    public float resetDelay = 3f;

    [Header("Cài đặt Phạt Đè Vạch")]
    public GameObject lineWarningPanel;
    public TextMeshProUGUI lineWarningText;
    public GameObject lineLossPanel;
    private int solidLineViolationCount = 0;
    private bool isCurrentlyOnLine = false;

    // --- BÌNH MỚI: BỘ ĐẾM THỜI GIAN ÂN HẠN ĐỂ KHÔNG BỊ PHẠT ĐÚP ---
    private float lineCooldownTimer = 0f;
    // --------------------------------------------------------------

    [Header("Tốc độ Giới hạn")]
    public float currentSpeedLimit = 0f;

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
    public AudioSource crashAudioSource;

    [Header("xin nhan")]
    public GameObject leftSignalLight;
    public GameObject rightSignalLight;
    public AudioSource signalAudio;
    public float blinkInterval = 0.5f;

    public bool isLeftSignalOn = false;
    public bool isRightSignalOn = false;
    public bool hasLookedLeftMirror = false;
    public bool hasLookedRightMirror = false;

    private float blinkTimer = 0f;
    private bool isLightOn = false;

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

        // --- CHẠY BỘ ĐẾM THỜI GIAN ÂN HẠN ĐÈ VẠCH ---
        if (lineCooldownTimer > 0)
        {
            lineCooldownTimer -= Time.deltaTime;
        }
        // --------------------------------------------

        if (Input.GetKeyDown(KeyCode.Q))
        {
            isLeftSignalOn = !isLeftSignalOn;
            if (isLeftSignalOn) isRightSignalOn = false;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            isRightSignalOn = !isRightSignalOn;
            if (isRightSignalOn) isLeftSignalOn = false;
        }

        HandleTurnSignals();
        CheckLookingAtMirrors();

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

        if (speedSlider != null)
        {
            speedSlider.maxValue = maxSpeed;
            speedSlider.value = currentSpeed;
        }

        if (speedDisplay != null)
        {
            int displaySpeedKmH = Mathf.RoundToInt(currentSpeed);

            if (currentSpeedLimit > 0)
            {
                speedDisplay.text = $"{displaySpeedKmH} / {Mathf.RoundToInt(currentSpeedLimit)} km/h";

                if (displaySpeedKmH >= currentSpeedLimit + 5)
                {
                    TriggerSpeedingFailure();
                }
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

    // --- CÁC HÀM XỬ LÝ VI PHẠM & TAI NẠN ---

    private void TriggerSpeedingFailure()
    {
        if (isCrashed) return;
        isCrashed = true;

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

    public void TriggerRedLightFailure()
    {
        if (isCrashed) return;
        isCrashed = true;

        if (ranRedPanel != null) ranRedPanel.SetActive(true);
        else if (accidentPanel != null) accidentPanel.SetActive(true);

        if (gasAudio != null) gasAudio.Stop();
        if (brakeAudio != null) brakeAudio.Stop();
        if (engineAudio != null) engineAudio.Stop();

        Time.timeScale = 0f;
        StartCoroutine(ResetGameRoutine());
    }

    public void TriggerWrongWay()
    {
        if (isCrashed) return;
        isCrashed = true;

        if (wrongWayPanel != null) wrongWayPanel.SetActive(true);

        if (gasAudio != null) gasAudio.Stop();
        if (brakeAudio != null) brakeAudio.Stop();
        if (engineAudio != null) engineAudio.Stop();

        Time.timeScale = 0f;
        StartCoroutine(ResetGameRoutine());
    }

    // --- HÀM XỬ LÝ ĐÈ VẠCH LIỀN ĐÃ ĐƯỢC NÂNG CẤP ---
    public void TriggerSolidLineViolation()
    {
        if (isCrashed || isCurrentlyOnLine || lineCooldownTimer > 0f) return;

        isCurrentlyOnLine = true;
        solidLineViolationCount++;
        lineCooldownTimer = 3f;

        if (lineWarningText != null)
        {
            lineWarningText.text = "Đè vạch liền: " + solidLineViolationCount + "/3";
        }

        if (solidLineViolationCount >= 3)
        {
            isCrashed = true;

            if (lineLossPanel != null) lineLossPanel.SetActive(true);
            if (lineWarningPanel != null) lineWarningPanel.SetActive(false);

            if (gasAudio != null) gasAudio.Stop();
            if (brakeAudio != null) brakeAudio.Stop();
            if (engineAudio != null) engineAudio.Stop();

            Time.timeScale = 0f;
            StartCoroutine(ResetGameRoutine());
        }
        else
        {
            StopCoroutine("ShowLineWarningRoutine");
            StartCoroutine(ShowLineWarningRoutine());
        }
    }

    IEnumerator ShowLineWarningRoutine()
    {
        if (lineWarningPanel != null) lineWarningPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        if (lineWarningPanel != null) lineWarningPanel.SetActive(false);
    }

    public void ResetLineTouch()
    {
        isCurrentlyOnLine = false;
    }
    // ----------------------------------------

    void HandleTurnSignals()
    {
        if (isLeftSignalOn || isRightSignalOn)
        {
            blinkTimer += Time.deltaTime;

            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                isLightOn = !isLightOn;

                if (isLightOn && signalAudio != null)
                {
                    signalAudio.Play();
                }
            }
        }
        else
        {
            isLightOn = false;
            blinkTimer = 0f;
        }

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

        // Check if hitting a pedestrian (even at low speed)
        bool isPedestrian = collision.gameObject.CompareTag("Pedestrian") ||
                            collision.gameObject.name.Contains("Person") ||
                            collision.gameObject.name.Contains("Ch02") ||
                            collision.gameObject.name.Contains("Ch22") ||
                            collision.gameObject.name.Contains("Aj") ||
                            collision.gameObject.name.Contains("The Boss") ||
                            collision.transform.GetComponentInParent<Animator>() != null;

        if (!isPedestrian && currentSpeed < 2f && !collision.gameObject.CompareTag("EnemyMoto")) return;

        isCrashed = true;

        if (crashAudioSource != null) crashAudioSource.Play();

        isLeftSignalOn = false;
        isRightSignalOn = false;
        if (leftSignalLight != null) leftSignalLight.SetActive(false);
        if (rightSignalLight != null) rightSignalLight.SetActive(false);

        if (gasAudio != null) gasAudio.Stop();
        if (brakeAudio != null) brakeAudio.Stop();
        if (engineAudio != null) engineAudio.Stop();

        if (isPedestrian && crosswalkFailPanel != null)
        {
            crosswalkFailPanel.SetActive(true);
        }
        else if (accidentPanel != null)
        {
            accidentPanel.SetActive(true);
        }

        Time.timeScale = 0f;
        StartCoroutine(ResetGameRoutine());
    }

    IEnumerator ResetGameRoutine()
    {
        yield return new WaitForSecondsRealtime(resetDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
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