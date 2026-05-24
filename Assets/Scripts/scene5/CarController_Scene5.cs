using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VR_CarController_Scene5 : MonoBehaviour
{
    [Header("Cài đặt Di chuyển & Vật lý")]
    public float maxSpeed = 20f;
    public float acceleration = 5f;
    public float brakingForce = 15f;
    public float friction = 2f;
    public float turnSpeed = 50f;

    [Header("Gắn UI Hướng dẫn, Taplo & Tai Nạn")]
    public TextMeshProUGUI gearDisplay;
    public GameObject tutorialPanel;
    public GameObject accidentPanel;
    public float resetDelay = 3f;

    [Header("Gắn nút trên tay cầm VR")]
    public InputActionReference buttonD;
    public InputActionReference buttonR;
    public InputActionReference buttonN;
    public InputActionReference buttonHorn;

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

    [Header("Xin nhan")]
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

    // ─── HORN SYSTEM ─────────────────────────────────────────────────────────
    [Header("Horn System")]
    [Tooltip("Số lần bấm còi tối đa trước khi Game Over")]
    public int maxHornWarnings = 3;

    [Tooltip("Cooldown (giây) giữa các lần bấm còi")]
    public float hornCooldown = 1.5f;

    [Header("Horn — Âm thanh")]
    public AudioClip hornSound;
    public AudioClip hornWarning1Sound;   // Tiếng chó sủa xa
    public AudioClip hornWarning2Sound;   // Tiếng chó sủa gần + trẻ khóc
    public AudioClip hornWarning3Sound;   // Hàng xóm la hét
    private AudioSource hornAudioSource;

    [Header("Horn — UI")]
    public GameObject hornWarningIconUI;        // Icon cảnh báo (Image/Panel)
    public TextMeshProUGUI hornWarningCountText; // "Cảnh báo: 1/3"
    public Image hornButtonImage;               // Nút còi trên HUD
    public Image vignetteOverlay;              // Overlay viền đỏ màn hình
    public GameObject hornCooldownIndicator;   // Hiện khi đang cooldown

    [Header("Horn — Warning Colors")]
    public Color hornWarningColor1 = new Color(1f, 0.8f, 0f, 1f);
    public Color hornWarningColor2 = new Color(1f, 0.4f, 0f, 1f);
    public Color hornWarningColor3 = new Color(0.9f, 0.1f, 0.1f, 1f);

    [Header("Horn — Tham chiếu hệ thống")]
    public NeighborReactionManager neighborManager;
    public GameOverManager hornGameOverManager;
    public GameObject gameOverPanel;
    public ScreenShakeController screenShake;
    public HornUIController hornUI;

    // Horn state
    private int hornWarningCount = 0;
    private bool hornOnCooldown = false;
    private bool isInJamZone = false;
    private bool hornLocked = false;
    private bool usedFreeHorn = false; // true sau lần còi hợp lệ đầu tiên di chuyển xe chắn
    // ─────────────────────────────────────────────────────────────────────────

    private string currentGear = "N";
    private float currentSpeed = 0f;
    private bool isCrashed = false;
    private bool sceneStarted = false;
    private bool waitingForGameOverRestart = false;

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

        // Horn setup
        if (hornAudioSource == null)
            hornAudioSource = gameObject.AddComponent<AudioSource>();
        if (hornGameOverManager != null && gameOverPanel != null && hornGameOverManager.gameOverPanel == null)
            hornGameOverManager.gameOverPanel = gameOverPanel;
        ResetHornUI();
        ShowHornButton(false); // Ẩn nút còi cho đến khi vào zone kẹt xe
    }

    // =========================================================================
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
        // Phím 1: ưu tiên dismiss tutorial hoặc restart sau GameOver
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (tutorialPanel != null && tutorialPanel.activeSelf)
            {
                tutorialPanel.SetActive(false);
                sceneStarted = true;
            }
            else if (waitingForGameOverRestart)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }
            else
            {
                SetGearN();
            }
        }

        if (isCrashed) return;

        // -- XIN NHAN --
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

        // -- GƯƠNG VR --
        CheckLookingAtMirrors();

        // -- CÒI (chỉ nhận khi trong zone kẹt xe) --
        if (!hornLocked)
        {
            bool vrHorn = buttonHorn != null && buttonHorn.action.WasPressedThisFrame();
            bool kbHorn = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.H);
            if (vrHorn || kbHorn) TryHorn();
        }

        // -- SỐ --
        if ((buttonD != null && buttonD.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha2)) SetGearD();
        if ((buttonR != null && buttonR.action.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.Alpha3)) SetGearR();
        if (buttonN != null && buttonN.action.WasPressedThisFrame()) SetGearN();

        // -- LÁI XE --
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
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F6)) EnterJamZone();
        if (Input.GetKeyDown(KeyCode.F7)) TryHorn();
        if (Input.GetKeyDown(KeyCode.F8)) ExitJamZone();
        if (Input.GetKeyDown(KeyCode.F9)) { hornWarningCount = 0; hornLocked = false; ResetHornUI(); hornUI?.HideAll(); }
#endif
    }

    // =========================================================================
    // HORN SYSTEM
    // =========================================================================

    /// <summary>Gọi từ TrafficJamTrigger khi xe vào zone kẹt</summary>
    public void EnterJamZone()
    {
#if UNITY_EDITOR
        Debug.Log("[Horn] EnterJamZone called — isInJamZone sẽ = true");
#endif
        // Auto-start nếu player vào zone trước khi bấm 1
        if (!sceneStarted)
        {
            sceneStarted = true;
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
        }
        isInJamZone = true;
        hornLocked = false;
        hornWarningCount = 0;
        usedFreeHorn = false;
        ResetHornUI();
        ShowHornButton(true);
        hornUI?.ShowZoneHint("Khu dân cư — hạn chế còi xe", 3f);
        hornUI?.ShowHornHint(true);
    }

    /// <summary>Gọi từ TrafficJamTrigger khi đường thông (thắng)</summary>
    public void ExitJamZone()
    {
        isInJamZone = false;
        hornLocked = true;
        ShowHornButton(false);
        hornUI?.ShowHornHint(false);
        hornUI?.HideAll();
    }

    /// <summary>Gọi từ nút còi trên UI</summary>
    public void OnHornButtonPressed()
    {
        if (!isInJamZone || hornLocked) return;
        TryHorn();
    }

    private void TryHorn()
    {
        if (hornOnCooldown) return;
#if UNITY_EDITOR
        Debug.Log($"[Horn] TryHorn: isInJamZone={isInJamZone} usedFree={usedFreeHorn} count={hornWarningCount} locked={hornLocked}");
#endif
        if (hornSound != null) hornAudioSource.PlayOneShot(hornSound, 1f);
        StartCoroutine(HornCooldownRoutine());

        // Lần còi hợp lệ duy nhất: di chuyển xe chắn (chỉ 1 lần miễn phạt)
        if (!usedFreeHorn)
        {
            BlockingCar[] blockingCars = FindObjectsByType<BlockingCar>(FindObjectsSortMode.None);
            foreach (BlockingCar bc in blockingCars)
            {
                if (Vector3.Distance(transform.position, bc.transform.position) <= bc.listenRadius)
                {
                    if (bc.MoveAside())
                    {
                        usedFreeHorn = true;
                        return;
                    }
                }
            }
        }

        // Mọi lần còi tiếp theo đều tính vi phạm
        hornWarningCount++;
        switch (hornWarningCount)
        {
            case 1: TriggerHornWarning1(); break;
            case 2: TriggerHornWarning2(); break;
            case 3: TriggerHornWarning3(); break;
        }

        if (hornWarningCount >= maxHornWarnings)
            StartCoroutine(TriggerGameOverAfterDelay(2.5f));
    }

    private IEnumerator HornCooldownRoutine()
    {
        hornOnCooldown = true;
        if (hornCooldownIndicator != null) hornCooldownIndicator.SetActive(true);
        yield return new WaitForSeconds(hornCooldown);
        hornOnCooldown = false;
        if (hornCooldownIndicator != null) hornCooldownIndicator.SetActive(false);
        hornUI?.ShowCooldown(false);
    }

    private void TriggerHornWarning1()
    {
        UpdateHornWarningUI(1, hornWarningColor1, "Canh bao: 1/3");
        if (hornWarning1Sound != null) hornAudioSource.PlayOneShot(hornWarning1Sound, 0.8f);
        if (screenShake != null) screenShake.Shake(0.15f, 0.3f);
        StartCoroutine(FlashWarningIcon(1.5f));
        hornUI?.ShowWarning(1, "Canh bao: 1/3");
    }

    private void TriggerHornWarning2()
    {
        UpdateHornWarningUI(2, hornWarningColor2, "Canh bao: 2/3");
        if (hornWarning2Sound != null) hornAudioSource.PlayOneShot(hornWarning2Sound, 0.8f);
        if (screenShake != null) screenShake.Shake(0.25f, 0.4f);
        SetVignetteAlpha(0.35f);
        StartCoroutine(FadeOutVignette(2.5f));
        if (neighborManager != null) neighborManager.TriggerReaction(2);
        hornUI?.ShowWarning(2, "Canh bao: 2/3"); 
        hornUI?.ShowVignette(0.35f);             
    }

    private void TriggerHornWarning3()
    {
        UpdateHornWarningUI(3, hornWarningColor3, "Canh bao: 3/3 - CUOI CUNG!");
        if (hornWarning3Sound != null) hornAudioSource.PlayOneShot(hornWarning3Sound, 0.8f);
        if (screenShake != null) screenShake.Shake(0.5f, 0.6f);
        StartCoroutine(FlashScreenRed());
        if (neighborManager != null) neighborManager.TriggerReaction(3);
        hornUI?.ShowWarning(3, "Canh bao: 3/3 - CUOI CUNG!");
        hornUI?.ShowVignette(0.7f, true);                    
    }

    // ── Horn UI helpers ───────────────────────────────────────────────────────

    private void UpdateHornWarningUI(int level, Color color, string message)
    {
        if (hornWarningIconUI != null)
        {
            hornWarningIconUI.SetActive(true);
            var img = hornWarningIconUI.GetComponent<Image>();
            if (img != null) img.color = color;
        }
        if (hornWarningCountText != null)
        {
            hornWarningCountText.text = message;
            hornWarningCountText.color = color;
        }
        if (hornButtonImage != null) hornButtonImage.color = color;
    }

    private void ResetHornUI()
    {
        if (hornWarningIconUI != null) hornWarningIconUI.SetActive(false);
        if (hornWarningCountText != null) hornWarningCountText.text = "";
        if (vignetteOverlay != null) SetVignetteAlpha(0f);
        if (hornButtonImage != null) hornButtonImage.color = Color.white;
    }

    private void ShowHornButton(bool show)
    {
        if (hornButtonImage != null) hornButtonImage.gameObject.SetActive(show);
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (vignetteOverlay == null) return;
        var c = vignetteOverlay.color;
        c.a = alpha;
        vignetteOverlay.color = c;
    }

    // ── Horn coroutines ───────────────────────────────────────────────────────

    private IEnumerator FlashWarningIcon(float duration)
    {
        if (hornWarningIconUI == null) yield break;
        var img = hornWarningIconUI.GetComponent<Image>();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (img != null) img.enabled = !img.enabled;
            yield return new WaitForSeconds(0.25f);
            elapsed += 0.25f;
        }
        if (img != null) img.enabled = true;
    }

    private IEnumerator FadeOutVignette(float duration)
    {
        if (vignetteOverlay == null) yield break;
        float startAlpha = vignetteOverlay.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetVignetteAlpha(Mathf.Lerp(startAlpha, 0f, elapsed / duration));
            yield return null;
        }
        SetVignetteAlpha(0f);
    }

    private IEnumerator FlashScreenRed()
    {
        if (vignetteOverlay == null) yield break;
        float t = 0f;
        while (t < 0.2f) { t += Time.deltaTime; SetVignetteAlpha(Mathf.Lerp(0f, 0.7f, t / 0.2f)); yield return null; }
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(FadeOutVignette(1.5f));
    }

    private IEnumerator TriggerGameOverAfterDelay(float delay)
    {
        hornLocked = true;
        yield return new WaitForSeconds(delay);

#if UNITY_EDITOR
        Debug.Log($"[Horn] TriggerGameOver — hornGameOverManager={(hornGameOverManager != null ? "OK" : "NULL")} gameOverPanel={(gameOverPanel != null ? "OK" : "NULL")}");
#endif

        if (hornGameOverManager != null)
        {
            hornGameOverManager.TriggerGameOver(hornWarningCount);
        }
        else
        {
            // Fallback trực tiếp nếu hornGameOverManager chưa gán
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                var cg = gameOverPanel.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
            Time.timeScale = 0f;
        }

        waitingForGameOverRestart = true;
    }

    // =========================================================================
    // XIN NHAN
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
        else
        {
            isLightOn = false;
            blinkTimer = 0f;
        }

        if (leftSignalLight != null) leftSignalLight.SetActive(isLeftSignalOn && isLightOn);
        if (rightSignalLight != null) rightSignalLight.SetActive(isRightSignalOn && isLightOn);
    }

    // =========================================================================
    // GƯƠNG VR
    // =========================================================================
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

    // =========================================================================
    // TAI NẠN
    // =========================================================================
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Road") || isCrashed) return;
        if (currentSpeed < 2f && !collision.gameObject.CompareTag("EnemyMoto")) return;

        isCrashed = true;
        if (crashAudioSource != null) crashAudioSource.Play();

        // Tắt xi nhan
        isLeftSignalOn = false;
        isRightSignalOn = false;
        if (leftSignalLight != null) leftSignalLight.SetActive(false);
        if (rightSignalLight != null) rightSignalLight.SetActive(false);

        // Tắt horn system
        hornLocked = true;
        isInJamZone = false;
        ShowHornButton(false);
        ResetHornUI();

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
    // SỐ
    // =========================================================================
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
    public void OnReachGoal()
    {
        // Khoá lái xe
        currentSpeed = 0f;
        isCrashed = true;       // Dùng lại cờ này để block Update

        // Tắt horn
        hornLocked = true;
        isInJamZone = false;
        ShowHornButton(false);
        hornUI?.HideAll();

        // Tắt âm thanh
        if (gasAudio != null) gasAudio.Stop();
        if (brakeAudio != null) brakeAudio.Stop();
        if (engineAudio != null) engineAudio.Stop();

        // Tắt xi nhan
        isLeftSignalOn = false;
        isRightSignalOn = false;
        if (leftSignalLight != null) leftSignalLight.SetActive(false);
        if (rightSignalLight != null) rightSignalLight.SetActive(false);

    }
    public int GetHornCount() => hornWarningCount;
}