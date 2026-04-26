using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý phản ứng hàng xóm khi người chơi bấm còi.
/// Gắn vào một GameObject rỗng tên "NeighborManager" trong scene.
/// </summary>
public class NeighborReactionManager : MonoBehaviour
{
    [Header("Đèn nhà hàng xóm")]
    [Tooltip("Các GameObject đèn trong nhà — bật lên khi bị đánh thức")]
    public GameObject[] neighborLights;

    [Header("Bóng người hàng xóm (tuỳ chọn)")]
    [Tooltip("Các bóng người xuất hiện ở cửa sổ — kích hoạt ở Warning 3")]
    public GameObject[] neighborSilhouettes;

    [Header("Âm thanh phản ứng")]
    public AudioSource neighborAudioSource;
    public AudioClip dogBarkFar;       // Warning 1: chó sủa xa
    public AudioClip dogBarkNear;      // Warning 2: chó sủa gần + trẻ khóc
    public AudioClip neighborShout;    // Warning 3: hàng xóm la hét

    [Header("Cài đặt")]
    [Tooltip("Delay (giây) trước khi đèn bật sau mỗi cảnh báo")]
    public float lightTurnOnDelay = 0.6f;

    void Start()
    {
        // Tắt hết đèn và bóng người lúc đầu
        SetAllLights(false);
        SetAllSilhouettes(false);
    }

    /// <summary>
    /// Gọi từ VR_CarController với level = 1, 2, hoặc 3
    /// </summary>
    public void TriggerReaction(int warningLevel)
    {
        switch (warningLevel)
        {
            case 1:
                PlaySound(dogBarkFar);
                // Chưa bật đèn ở cảnh báo 1 — chỉ âm thanh
                break;

            case 2:
                PlaySound(dogBarkNear);
                // Bật một nửa số đèn
                StartCoroutine(TurnOnLightsGradually(0, neighborLights.Length / 2));
                break;

            case 3:
                PlaySound(neighborShout);
                // Bật toàn bộ đèn + hiện bóng người
                StartCoroutine(TurnOnLightsGradually(0, neighborLights.Length));
                StartCoroutine(ShowSilhouettesAfterDelay(0.8f));
                break;
        }
    }

    /// <summary>Tắt hết khi reset / game over</summary>
    public void ResetAll()
    {
        StopAllCoroutines();
        SetAllLights(false);
        SetAllSilhouettes(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (neighborAudioSource != null && clip != null)
            neighborAudioSource.PlayOneShot(clip);
    }

    private IEnumerator TurnOnLightsGradually(int from, int to)
    {
        to = Mathf.Min(to, neighborLights.Length);
        for (int i = from; i < to; i++)
        {
            yield return new WaitForSeconds(lightTurnOnDelay);
            if (neighborLights[i] != null)
                neighborLights[i].SetActive(true);
        }
    }

    private IEnumerator ShowSilhouettesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetAllSilhouettes(true);
    }

    private void SetAllLights(bool active)
    {
        foreach (var light in neighborLights)
            if (light != null) light.SetActive(active);
    }

    private void SetAllSilhouettes(bool active)
    {
        foreach (var s in neighborSilhouettes)
            if (s != null) s.SetActive(active);
    }
}