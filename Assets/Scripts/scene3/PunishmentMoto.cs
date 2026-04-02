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

    [Header("Âm thanh xe máy")]
    public AudioSource motoEngineAudio; // Loa gắn trên xe máy

    public void SetTarget(Transform car, float offset)
    {
        targetCar = car;
        sideOffset = offset;
        isFollowing = true;
        lastCarPos = car.position;
        laneForward = transform.forward;
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

        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // --- ĐIỀU CHỈNH TIẾNG PÔ XE THEO TỐC ĐỘ ---
        if (motoEngineAudio != null)
        {
            // Tốc độ càng cao, tiếng máy càng thanh và gắt (Pitch từ 0.8 đến 2.5)
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