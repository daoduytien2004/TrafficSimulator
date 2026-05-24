using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Nút kích hoạt bằng cách nhìn vào (Gaze).
/// Nhìn vào nút đủ gazeTime giây → tự động gọi OnActivated.
/// Hiển thị vòng đếm ngược (fillImage) để người dùng biết tiến độ.
/// </summary>
public class GazeButton : MonoBehaviour
{
    [Header("Cài đặt Gaze")]
    public float gazeTime = 2f;
    public Transform vrCamera;

    [Header("Visual phản hồi")]
    [Tooltip("Image kiểu Filled (360°) làm vòng đếm ngược")]
    public Image fillImage;
    public Color normalColor  = new Color(1f, 1f, 1f, 0.3f);
    public Color progressColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);

    [Header("Sự kiện")]
    public UnityEvent OnActivated;

    private float gazeTimer = 0f;
    private bool activated  = false;

    void OnEnable()
    {
        gazeTimer = 0f;
        activated = false;
        SetFill(0f);
    }

    void Update()
    {
        if (activated) return;
        if (vrCamera == null) return;

        if (IsGazing())
        {
            gazeTimer += Time.unscaledDeltaTime;
            SetFill(gazeTimer / gazeTime);

            if (gazeTimer >= gazeTime)
            {
                activated = true;
                SetFill(1f);
                OnActivated?.Invoke();
            }
        }
        else
        {
            gazeTimer = 0f;
            SetFill(0f);
        }
    }

    private bool IsGazing()
    {
        // Bắn ray từ camera về phía trước, kiểm tra có trúng collider của nút này không
        Ray ray = new Ray(vrCamera.position, vrCamera.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, 10f)) return false;
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }

    private void SetFill(float t)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = t;
        fillImage.color = Color.Lerp(normalColor, progressColor, t);
    }
}
