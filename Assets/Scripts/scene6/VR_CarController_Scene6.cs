using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller xe người chơi cho Scene 6.
/// Giống CarController gốc nhưng:
/// - Có hàm LockControl() / UnlockControl() cho Scene6Manager gọi khi xong kịch bản
/// - Không có speed limit / crosswalk logic
/// </summary>
public class VR_CarController_Scene6 : MonoBehaviour
{
    [Header("Cài đặt Di chuyển & Vật lý")]
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    public float brakingForce = 15f;
    public float friction = 2f;
    public float turnSpeed = 50f;

    [Header("UI")]
    public TextMeshProUGUI gearDisplay;
    public TextMeshProUGUI speedDisplay;
    public Slider speedSlider;
    public GameObject tutorialPanel;
    public GameObject accidentPanel;
    public float resetDelay = 3f;

    [Header("Nút VR")]
    public InputActionReference buttonD;
    public InputActionReference buttonR;
    public InputActionReference buttonN;

    [Header("Âm thanh")]
    public AudioSource engineAudio;
    public AudioClip startupClip;
    public AudioClip idleClip;
    public float overlapTime = 0.5f;
    public AudioSource gasAudio;
    public AudioSource brakeAudio;
    public float minPitch = 0.8f;
    public float maxPitch = 2.5f;
    public AudioSource crashAudioSource;

    [Header("Xi-nhan & Gương")]
    public Transform vrHeadset;
    public Transform leftMirror;
    public Transform rightMirror;
    public GameObject leftSignalLight;
    public GameObject rightSignalLight;
    public AudioSource signalAudio;
    public float blinkInterval = 0.5f;

    [HideInInspector] public bool isLeftSignalOn = false;
    [HideInInspector] public bool isRightSignalOn = false;
    [HideInInspector] public bool hasLookedLeftMirror = false;
    [HideInInspector] public bool hasLookedRightMirror = false;

    private float blinkTimer = 0f;
    private bool isLightOn = false;
    private string currentGear = "N";
    private float currentSpeed = 0f;
    private bool isCrashed = false;
    private bool controlLocked = false;

    // =========================================================================
    void Start()
    {
        Time.timeScale = 1f;
        UpdateGearDisplay();
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (accidentPanel != null) accidentPanel.SetActive(false);
        if (engineAudio != null && startupClip != null && idleClip != null)
            StartCoroutine(StartEngineRoutine());
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

    // =========================================================================
    void Update()
    {
        if (isCrashed || controlLocked) return;

        // Xi-nhan
        if (Input.GetKeyDown(KeyCode.Q)) { isLeftSignalOn = !isLeftSignalOn; if (isLeftSignalOn) isRightSignalOn = false; }
        if (Input.GetKeyDown(KeyCode.E)) { isRightSignalOn = !isRightSignalOn; if (isRightSignalOn) isLeftSignalOn = false; }
        HandleTurnSignals();
        CheckLookingAtMirrors();

        // Số
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

        if (speedSlider != null) { speedSlider.maxValue = maxSpeed; speedSlider.value = currentSpeed; }
        if (speedDisplay != null) speedDisplay.text = $"{Mathf.RoundToInt(currentSpeed)} km/h";

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

    // =========================================================================
    void HandleTurnSignals()
    {
        if (isLeftSignalOn || isRightSignalOn)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                isLightOn = !isLightOn;
                if (isLightOn && signalAudio != null) signalAudio.Play();
            }
        }
        else { isLightOn = false; blinkTimer = 0f; }

        if (leftSignalLight != null) leftSignalLight.SetActive(isLeftSignalOn && isLightOn);
        if (rightSignalLight != null) rightSignalLight.SetActive(isRightSignalOn && isLightOn);
    }

    void CheckLookingAtMirrors()
    {
        if (vrHeadset == null) return;
        if (leftMirror != null)
        {
            Vector3 d = (leftMirror.position - vrHeadset.position).normalized;
            if (Vector3.Angle(vrHeadset.forward, d) < 20f) hasLookedLeftMirror = true;
        }
        if (rightMirror != null)
        {
            Vector3 d = (rightMirror.position - vrHeadset.position).normalized;
            if (Vector3.Angle(vrHeadset.forward, d) < 20f) hasLookedRightMirror = true;
        }
    }

    // =========================================================================
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Road") || isCrashed) return;
        if (currentSpeed < 2f && !collision.gameObject.CompareTag("EnemyMoto")) return;

        isCrashed = true;
        if (crashAudioSource != null) crashAudioSource.Play();
        isLeftSignalOn = false; isRightSignalOn = false;
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

    // =========================================================================
    public void LockControl()
    {
        controlLocked = true;
        currentSpeed = 0f;
        if (gasAudio != null) gasAudio.Stop();
        if (brakeAudio != null) brakeAudio.Stop();
        if (engineAudio != null) engineAudio.Stop();
    }

    public void SetGearN() { currentGear = "N"; UpdateGearDisplay(); }
    public void SetGearD() { currentGear = "D"; UpdateGearDisplay(); if (tutorialPanel != null) tutorialPanel.SetActive(false); }
    public void SetGearR() { currentGear = "R"; UpdateGearDisplay(); if (tutorialPanel != null) tutorialPanel.SetActive(false); }

    void UpdateGearDisplay()
    {
        if (gearDisplay == null) return;
        gearDisplay.text = currentGear;
        if (currentGear == "D") gearDisplay.color = Color.green;
        else if (currentGear == "R") gearDisplay.color = Color.red;
        else gearDisplay.color = Color.white;
    }
}
