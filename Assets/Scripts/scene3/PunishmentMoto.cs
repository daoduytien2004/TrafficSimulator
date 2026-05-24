using UnityEngine;

public class PunishmentMoto : MonoBehaviour
{
    private Transform targetCar;
    public float currentSpeed = 0f;

    private bool isCrashing = false;
    private bool isFollowing = false;
    private bool isSlowingDown = false;
    private bool isPassing = false;

    private float sideOffset = 0f;
    public float followDistance = 8f;

    private Vector3 lastCarPos;
    private float carRealSpeed = 0f;

    private Vector3 laneForward;
    private Vector3 laneRight; // BÌNH MỚI: Thêm trục ngang để lách

    [Header("Âm thanh xe máy")]
    public AudioSource motoEngineAudio;

    public void SetTarget(Transform car, float offset)
    {
        targetCar = car;
        sideOffset = offset;
        isFollowing = true;
        lastCarPos = car.position;

        laneForward = transform.forward;
        laneRight = transform.right; // Lưu lại hướng ngang của làn đường
    }

    void Update()
    {
        if (targetCar == null || Time.deltaTime == 0f) return;

        carRealSpeed = Vector3.Distance(targetCar.position, lastCarPos) / Time.deltaTime;
        lastCarPos = targetCar.position;

        if (isFollowing)
        {
            Vector3 offsetFromCar = targetCar.position - transform.position;
            float zDistance = Vector3.Dot(offsetFromCar, laneForward);

            if (carRealSpeed < 3f && zDistance < 15f)
            {
                PassStraight();
            }
            else
            {
                float distanceError = zDistance - followDistance;
                float targetSpeed = carRealSpeed + (distanceError * 1.5f);
                if (zDistance < 5f) targetSpeed = Mathf.Min(targetSpeed, carRealSpeed * 0.8f);

                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 3f);
                currentSpeed = Mathf.Clamp(currentSpeed, 2f, carRealSpeed + 10f);
            }

            // --- BÌNH MỚI: TỰ ĐỘNG DUY TRÌ KHOẢNG CÁCH NGANG ---
            // Tính độ hở ngang hiện tại giữa ô tô và xe máy
            float currentHorizontalGap = Vector3.Dot(transform.position - targetCar.position, laneRight);

            // Nếu ô tô đi loạng choạng ép sát vào xe máy (Khoảng cách bị thu hẹp)
            // sideOffset là khoảng cách an toàn. Mathf.Abs dùng để lấy số dương cho dễ so sánh
            if (Mathf.Abs(currentHorizontalGap) < Mathf.Abs(sideOffset))
            {
                // Tự động đẩy chiếc xe máy dạt ra xa theo phương ngang để không cạ vào đít xe
                float pushDirection = Mathf.Sign(sideOffset); // Xác định đang ở trái hay phải
                transform.position += laneRight * pushDirection * Time.deltaTime * 3f;
            }
            // ---------------------------------------------------

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(laneForward), Time.deltaTime * 5f);
        }
        else if (isCrashing)
        {
            currentSpeed = 45f;
        }
        else if (isSlowingDown)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 2f, Time.deltaTime * 3f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(laneForward), Time.deltaTime * 5f);
        }
        else if (isPassing)
        {
            float passSpeed = Mathf.Max(carRealSpeed + 10f, 25f);
            currentSpeed = Mathf.Lerp(currentSpeed, passSpeed, Time.deltaTime * 2f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(laneForward), Time.deltaTime * 5f);
        }

        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.World); // Ép đi thẳng theo thế giới thực

        if (motoEngineAudio != null)
        {
            motoEngineAudio.pitch = Mathf.Lerp(0.8f, 2.5f, currentSpeed / 45f);
        }
    }

    public void SlowDown()
    {
        if (isFollowing)
        {
            isFollowing = false;
            isSlowingDown = true;
        }
    }

    public void SpeedUpToCrash()
    {
        if (isCrashing || isSlowingDown || isPassing) return;
        isFollowing = false;
        isCrashing = true;

        Vector3 targetPos = targetCar.position;
        targetPos.y = transform.position.y;
        targetPos += targetCar.forward * 1.5f;

        Vector3 crashDir = (targetPos - transform.position).normalized;
        if (Vector3.Dot(transform.forward, crashDir) > -0.2f)
        {
            transform.rotation = Quaternion.LookRotation(crashDir);
        }
    }

    public void PassStraight()
    {
        if (isCrashing) return;
        isFollowing = false;
        isPassing = true;
    }
}