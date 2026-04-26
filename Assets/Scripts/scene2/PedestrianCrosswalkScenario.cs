using UnityEngine;
using System.Collections;

public class PedestrianCrosswalkScenario : MonoBehaviour
{
    [Header("Cài đặt Người đi bộ (Vật lý)")]
    public Transform pedestrianModel;        // Kéo Model 3D người đi bộ vào đây
    public Transform startPoint;             // Điểm xuất phát (bên này vỉa hè)
    public Transform endPoint;               // Điểm đích đến (bên kia vỉa hè)
    public float walkSpeed = 2.5f;           // Tốc độ đi bộ
    public Vector3 rotationOffset = new Vector3(0, 0, 0); // Bù trừ góc xoay nếu model đi lùi (vd: Y=180)

    [Header("Cảm biến xe lại gần")]
    public float triggerDistance = 30f;      // Xe cách vạch 30m thì người bắt đầu bước xuống đường

    [Header("Tín hiệu AR (Tuỳ chọn)")]
    public GameObject arWarningLight;        
    public GameObject arSafeLight;           

    public bool isPedestrianCrossing = false; 
    private VR_CarController car;
    private bool hasFinishedCrossing = false;
    private bool isWalking = false;
    private Animator[] pedAnims;

    void Start()
    {
        // Tắt đèn AR ban đầu
        if (arWarningLight != null) arWarningLight.SetActive(false);
        if (arSafeLight != null) arSafeLight.SetActive(false);
        
        car = FindObjectOfType<VR_CarController>();

        // Đặt người đi bộ vào hệ tọa độ XZ của vạch xuất phát, giữ nguyên chiều cao Y ban đầu
        if (pedestrianModel != null && startPoint != null)
        {
            Vector3 startPos = startPoint.position;
            startPos.y = pedestrianModel.position.y; // Giữ nguyên độ cao bàn chân hiện tại
            pedestrianModel.position = startPos;
            
            pedAnims = pedestrianModel.GetComponentsInChildren<Animator>();
            if (pedAnims != null)
            {
                foreach (Animator anim in pedAnims)
                {
                    anim.enabled = false; // Tắt não đi bộ lúc ban đầu
                }
            }
        }
    }

    void Update()
    {
        if (car == null || hasFinishedCrossing) return;

        float distToCrosswalk = Vector3.Distance(transform.position, car.transform.position);

        // Kích hoạt người đi bộ bước qua đường khi xe chạy tới gần
        if (!isWalking && distToCrosswalk <= triggerDistance)
        {
            isWalking = true;
            isPedestrianCrossing = true; // Trạng thái: cấm xe đi qua
            if (arWarningLight != null) arWarningLight.SetActive(true);

            if (pedAnims != null)
            {
                foreach (Animator anim in pedAnims)
                {
                    anim.enabled = true; // Bật não để vung tay chân
                }
            }
        }

        // Di chuyển người đi bộ mỗi frame
        if (isWalking && pedestrianModel != null && endPoint != null)
        {
            // Chỉ di chuyển theo chiều ngang (X, Z). Khóa cứng chiều cao Y để không lún đất
            Vector3 targetPos = new Vector3(endPoint.position.x, pedestrianModel.position.y, endPoint.position.z);
            pedestrianModel.position = Vector3.MoveTowards(pedestrianModel.position, targetPos, walkSpeed * Time.deltaTime);

            // Cho NPC quay mặt về đích (Khóa trục Y)
            Vector3 lookPos = endPoint.position;
            lookPos.y = pedestrianModel.position.y;
            pedestrianModel.LookAt(lookPos);
            
            // Xoay bù thêm phần góc bị ngược (Cho phép gõ 180 ở ô ngoài để lật ngược nếu bị đi lùi)
            if (rotationOffset != Vector3.zero)
            {
                pedestrianModel.Rotate(rotationOffset, Space.Self);
            }

            // Kiểm tra xem người đã đi sang bờ bên kia chưa (chỉ xét X và Z)
            Vector3 currentPosFlat = new Vector3(pedestrianModel.position.x, 0, pedestrianModel.position.z);
            Vector3 endPosFlat = new Vector3(endPoint.position.x, 0, endPoint.position.z);
            if (Vector3.Distance(currentPosFlat, endPosFlat) < 0.1f)
            {
                isWalking = false;
                isPedestrianCrossing = false; // Người đã sang đường xong, bãi bỏ lệnh cấm
                hasFinishedCrossing = true;

                // Tắt nháy đỏ, bật nháy xanh dọn đường cho xe đi
                if (arWarningLight != null) arWarningLight.SetActive(false);
                if (arSafeLight != null) arSafeLight.SetActive(true);

                if (pedAnims != null)
                {
                    foreach (Animator anim in pedAnims)
                    {
                        anim.enabled = false; // Tắt não để đứng lại bên kia đường
                    }
                }
                
                Debug.Log("[Right-of-way] Hành khách đã qua đường an toàn. Xe được phép di chuyển!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        VR_CarController triggerCar = other.GetComponentInParent<VR_CarController>();

        if (triggerCar != null)
        {
            // NẾU CHẠM VẠCH TRONG KHI NGƯỜI VẪN ĐANG QUA ĐƯỜNG -> THẤT BẠI
            if (isPedestrianCrossing)
            {
                Debug.LogWarning("[Right-of-way] LỖI VI PHẠM: ĐÈ VẠCH KHÔNG NHƯỜNG ĐƯỜNG CHO NGƯỜI ĐI BỘ!");
                triggerCar.TriggerCrosswalkFailure();
            }
        }
    }
}
