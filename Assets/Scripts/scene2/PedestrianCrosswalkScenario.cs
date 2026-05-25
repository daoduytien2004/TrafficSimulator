using UnityEngine;
using System.Collections;
using FCG;

public class PedestrianCrosswalkScenario : MonoBehaviour
{
    [Header("Cài đặt Người đi bộ (Vật lý)")]
    public Transform pedestrianModel;        // Kéo Model 3D người đi bộ vào đây
    public Transform startPoint;             // Điểm xuất phát (bên này vỉa hè)
    public Transform endPoint;               // Điểm đích đến (bên kia vỉa hè)
    public float walkSpeed = 2.5f;           // Tốc độ đi bộ
    public Vector3 rotationOffset = new Vector3(0, 0, 0); // Bù trừ góc xoay nếu model đi lùi (vd: Y=180)

    [Header("Cài đặt Đèn Giao Thông (Tuỳ chọn)")]
    public TrafficLight trafficLight;        // Kéo Đèn Giao Thông vào đây để tự động hoá
    
    [Header("Cảm biến xe lại gần (Fallback)")]
    public float triggerDistance = 30f;      // Xe cách vạch 30m thì người bắt đầu bước xuống đường (khi không gán đèn)

    [Header("Tín hiệu AR (Tuỳ chọn)")]
    public GameObject arWarningLight;        
    public GameObject arSafeLight;           

        public bool isPedestrianCrossing = false; 
    private VR_CarController car;
    private bool hasFinishedCrossing = false;
    private bool isWalking = false;
    private Animator[] pedAnims;

    private float lastProjection = 0f;
    private bool projectionInitialized = false;

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

        // Kích hoạt người đi bộ bước qua đường
        bool shouldStartCrossing = false;
        if (trafficLight != null)
        {
            // Có đèn giao thông: người dân qua đường khi ĐÈN ĐỎ VÀ XE ĐÃ ĐẾN GẦN (để tránh đi xong trước khi xe kịp tới)
            shouldStartCrossing = !isWalking && !hasFinishedCrossing && trafficLight.Red.activeSelf && distToCrosswalk <= triggerDistance;
        }
        else
        {
            // Không có đèn giao thông: người dân qua đường khi xe đến gần (logic cũ)
            shouldStartCrossing = !isWalking && distToCrosswalk <= triggerDistance;
        }

        if (shouldStartCrossing)
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

        // HỆ THỐNG PHÁT HIỆN LẠNG LÁCH / VƯỢT QUA VẠCH KHI ĐÈN ĐỎ HOẶC NGƯỜI ĐANG QUA ĐƯỜNG
        bool isCrossingForbidden = false;
        if (trafficLight != null)
        {
            isCrossingForbidden = trafficLight.Red.activeSelf; // Đèn đỏ = cấm vượt qua vạch kẻ
        }
        else
        {
            isCrossingForbidden = isPedestrianCrossing; // Không có đèn = cấm vượt khi người đang đi
        }

        if (isCrossingForbidden && startPoint != null && endPoint != null)
        {
            // Vector dọc theo vạch kẻ đường (từ điểm xuất phát sang điểm đích)
            Vector3 crosswalkVec = endPoint.position - startPoint.position;
            crosswalkVec.y = 0f;
            crosswalkVec.Normalize();

            // Vector vuông góc với vạch kẻ đường = Hướng đi của đường lộ
            Vector3 roadDir = new Vector3(-crosswalkVec.z, 0f, crosswalkVec.x);

            // Vector từ vạch kẻ đường tới xe
            Vector3 crosswalkToCar = car.transform.position - transform.position;
            crosswalkToCar.y = 0f;

            // Chiếu vị trí xe lên hướng đường lộ
            float currentProjection = Vector3.Dot(crosswalkToCar, roadDir);

            if (!projectionInitialized)
            {
                lastProjection = currentProjection;
                projectionInitialized = true;
            }
            else
            {
                // Nếu xe đi qua vạch (đổi dấu của hình chiếu)
                if (Mathf.Sign(currentProjection) != Mathf.Sign(lastProjection) && distToCrosswalk < 15f)
                {
                    if (trafficLight != null && trafficLight.Red.activeSelf)
                    {
                        Debug.LogWarning("[TrafficLight] LỖI VI PHẠM: VƯỢT ĐÈN ĐỎ!");
                        car.TriggerRedLightFailure();
                    }
                    else
                    {
                        Debug.LogWarning("[Right-of-way] LỖI VI PHẠM: CỐ TÌNH LẠNG LÁCH VƯỢT QUA KHI NGƯỜI ĐI BỘ ĐANG QUA ĐƯỜNG!");
                        car.TriggerCrosswalkFailure();
                    }
                }
                lastProjection = currentProjection;
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
            if (Vector3.Distance(currentPosFlat, endPosFlat) < 0.6f)
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
