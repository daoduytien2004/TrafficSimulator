using System.Collections;
using UnityEngine;

/// <summary>
/// Rung camera khi bấm còi.
/// Gắn vào GameObject "Main Camera" hoặc camera con bên trong VR rig.
/// </summary>
public class ScreenShakeController : MonoBehaviour
{
    [Header("Giới hạn rung")]
    [Tooltip("Cường độ rung tối đa cho phép (tránh quá mạnh trong VR)")]
    public float maxIntensity = 0.5f;

    private Vector3 originalLocalPos;
    private Coroutine shakeRoutine;

    void Start()
    {
        originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// Gọi từ VR_CarController.
    /// intensity: cường độ rung (0.1 nhẹ — 0.5 mạnh)
    /// duration: thời gian rung (giây)
    /// </summary>
    public void Shake(float intensity, float duration)
    {
        intensity = Mathf.Clamp(intensity, 0f, maxIntensity);

        // Nếu đang rung thì dừng rung cũ, bắt đầu rung mới
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Giảm dần cường độ theo thời gian
            float currentIntensity = Mathf.Lerp(intensity, 0f, elapsed / duration);

            // Offset ngẫu nhiên xung quanh vị trí gốc
            float offsetX = Random.Range(-1f, 1f) * currentIntensity;
            float offsetY = Random.Range(-1f, 1f) * currentIntensity;

            transform.localPosition = originalLocalPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Reset về vị trí gốc
        transform.localPosition = originalLocalPos;
        shakeRoutine = null;
    }

    /// <summary>Dừng rung ngay lập tức (gọi khi tai nạn / game over)</summary>
    public void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        transform.localPosition = originalLocalPos;
    }
}