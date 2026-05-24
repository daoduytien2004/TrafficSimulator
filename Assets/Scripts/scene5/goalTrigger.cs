using UnityEngine;

/// <summary>
/// GoalTrigger — Khi xe player chạm vào collider này thì kích hoạt màn hình thắng.
/// Gắn vào GameObject đích, thêm BoxCollider (Is Trigger = true).
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    [Header("Tham chiếu")]
    public VR_CarController_Scene5 carController;
    public WinManager winManager;

    [Header("Hiệu ứng đến đích (tuỳ chọn)")]
    public GameObject goalEffect;       // Particle, ánh sáng, v.v.
    public AudioSource goalAudioSource;
    public AudioClip goalSound;

    private bool reached = false;

    void OnTriggerEnter(Collider other)
    {
        if (reached) return;
        if (!other.CompareTag("Player")) return;

        reached = true;

        // Hiệu ứng
        if (goalEffect != null) goalEffect.SetActive(true);
        if (goalAudioSource != null && goalSound != null)
            goalAudioSource.PlayOneShot(goalSound);

        // Khoá điều khiển xe
        if (carController != null) carController.OnReachGoal();

        // Hiện màn hình thắng
        if (winManager != null) winManager.TriggerWin(carController);
        else Debug.LogWarning("[GoalTrigger] WinManager chua duoc gan!");
    }

    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 0.9f);
        Gizmos.DrawWireCube(col.center, col.size);
    }
}