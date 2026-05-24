using UnityEngine;

/// <summary>
/// Gắn vào GameObject IntersectionZone.
/// Khi player (tag "Player") vào vùng này → báo Scene6Manager vi phạm đèn đỏ.
/// IntersectionZone được bật/tắt bởi Scene6Manager theo trạng thái đèn:
///   Đèn đỏ → SetActive(true)  → Trigger hoạt động
///   Đèn xanh → SetActive(false) → Trigger tắt, NPC qua tự do
/// </summary>
public class IntersectionZoneTrigger : MonoBehaviour
{
    public Scene6Manager manager;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            manager?.OnPlayerRanRedLight();
    }
}
