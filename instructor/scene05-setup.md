# Hướng dẫn Setup Scene 5 — Traffic Jam

## Tổng quan chức năng Scene 5

Người chơi lái xe vào vùng kẹt xe → các xe NPC dừng lại → người chơi bấm còi →
xe phía trước lách sang bên → người chơi đi qua → đến đích → màn hình thắng.
Nếu bấm còi quá 3 lần không có lý do → Game Over.

---

## Sơ đồ kết nối các object

```
[Player_Car]
    └── VR_CarController_Scene5
            ├── hornGameOverManager → [GameOverManager]
            ├── neighborManager    → [NeighborManager]
            ├── screenShake        → [Camera có ScreenShakeController]
            └── hornUI             → [HornHUD có HornUIController]

[TrafficJamZone]  ← BoxCollider (Is Trigger = true) đặt trên đường
    └── TrafficJamTrigger
            ├── carController         → [Player_Car]
            ├── managedBlockingCars[] → [các xe BlockingCar muốn dồn vào]
            └── jamPoint              → [Empty Object đặt giữa zone kẹt]

[CarContainer]  ← folder chứa toàn bộ xe NPC
    └── mỗi xe có:
            ├── FCG.TrafficCar  (atualWay phải được gán)
            └── BlockingCar     (endPoint để trống hoặc gán điểm đích)

[GoalTrigger]  ← BoxCollider (Is Trigger = true) đặt ở cuối đường
    └── GoalTrigger
            ├── carController → [Player_Car]
            └── winManager    → [WinManager]
```

---

## BƯỚC 0 — Kiểm tra lỗi trước tiên

1. Mở Unity, mở **Scene5**
2. Nhìn góc dưới Unity — nếu có icon đỏ → click mở **Console**
3. Nhấn **Play**
4. Nhấn **F6** (test vào jam zone), **F7** (test bấm còi)
5. Nhấn **Play** lần nữa để dừng
6. Kiểm tra Console có dòng đỏ không

---

## BƯỚC 1 — Fix AudioListener (nếu có warning âm thanh)

**Triệu chứng:** Console spam "There are N audio listeners" hoặc "There are no audio listeners"

**Fix:**
1. Trong Hierarchy, tìm kiếm: `t:AudioListener`
2. Giữ lại **đúng 1 cái** — cái nằm trên `Main Camera` bên trong XR Origin
3. Với 3 cái còn lại: click chọn → Inspector → component **Audio Listener** → dấu ⋮ → **Remove Component**
4. Nếu xóa hết rồi (0 cái): click `Main Camera` trong XR Origin → **Add Component** → `Audio Listener`

---

## BƯỚC 2 — Kiểm tra Player_Car có đủ tham chiếu chưa

1. Click **Player_Car** trong Hierarchy
2. Inspector → cuộn xuống section **"Horn — Tham chiếu hệ thống"**
3. Đảm bảo 4 ô này **không để trống (None)**:

| Field | Gán vào object nào |
|---|---|
| `Neighbor Manager` | Object tên `NeighborManager` trong Hierarchy |
| `Horn Game Over Manager` | Object tên `GameOverManager` trong Hierarchy |
| `Screen Shake` | Camera có script `ScreenShakeController` |
| `Horn UI` | Object `HornHUD` có script `HornUIController` |

**Cách gán:** Tìm object trong Hierarchy → giữ chuột kéo thả vào ô trống trong Inspector

---

## BƯỚC 3 — Gán Waypoint cho toàn bộ xe NPC (1 click)

Script `AutoAssignWaypoints` đã được tạo sẵn trong `Assets/Editor/`.

1. Đợi Unity compile xong
2. Menu trên cùng → **Tools** → **"Auto Assign Waypoints cho tất cả BlockingCar"**
3. Xem kết quả — nếu "Thất bại > 0": tìm xe đó trong Console → xoay xe 180° → chạy lại
4. Nhấn **Ctrl + S** để lưu scene

> **Lưu ý:** Nếu tất cả xe đã "Bỏ qua (đã có waypoint)" thì không cần làm gì thêm.

---

## BƯỚC 4 — Setup TrafficJamTrigger

1. Click **TrafficJamZone** trong Hierarchy
2. Inspector → component **TrafficJamTrigger** → điền:

| Field | Giá trị |
|---|---|
| `Car Controller` | Kéo **Player_Car** vào |
| `Detection Radius` | `500` |
| `Jam Duration` | `30` |
| `Managed Blocking Cars` | Click **+**, kéo 5–10 xe từ CarContainer vào |
| `Jam Point` | Tạo Empty Object đặt giữa đường → kéo vào đây |

**Tạo JamPoint:**
- Hierarchy → chuột phải → **Create Empty** → đặt tên `JamPoint`
- Dùng công cụ Move (phím W) kéo ra giữa đoạn đường kẹt trong Scene view
- Kéo object `JamPoint` vào ô `Jam Point` của TrafficJamTrigger

**Kiểm tra BoxCollider của TrafficJamZone:**
- Nhìn Scene view phải thấy hộp màu xanh lá bao phủ đoạn đường kẹt
- Nếu chưa đúng: Inspector → **Box Collider** → chỉnh **Size** và **Center**

---

## BƯỚC 5 — Tạo GoalTrigger (đích đến)

Scene5 mặc định chưa có GoalTrigger — cần tạo mới:

1. Hierarchy → chuột phải → **Create Empty** → đặt tên `GoalTrigger`
2. Di chuyển ra **cuối đoạn đường** trong Scene view
3. Inspector → **Add Component** → **Box Collider**
   - Tick ô **Is Trigger**
   - Chỉnh **Size** để bao phủ hết chiều rộng đường (ví dụ: X=10, Y=3, Z=5)
4. **Add Component** → **Goal Trigger**
5. Điền:
   - `Car Controller` → kéo **Player_Car** vào
   - `Win Manager` → kéo object **WinManager** vào

---

## BƯỚC 6 — Test hoàn chỉnh

Nhấn **Play**, thử theo thứ tự:

| Phím | Kết quả mong đợi |
|---|---|
| `1` | Số N |
| `2` rồi `↑` (W) | Xe chạy tiến |
| `F6` | Console in: `[TrafficJam] Tim thay X xe` + xe NPC dừng |
| `F7` | Console in: `[BlockingCar] Né sang phải/trái` |
| `F8` | Xe NPC chạy lại |
| `F9` | Reset đếm còi về 0 |

---

## Lỗi thường gặp & cách fix

| Cảnh báo trong Console | Nguyên nhân | Fix |
|---|---|---|
| `wCollider chưa được setup` | Xe chưa Configure | Chọn xe → Inspector → FCG TrafficCar → click **Generate WheelColliders** |
| `Không tìm thấy waypoint` | Xe quay ngược chiều đường | Xoay xe 180° → chạy lại Auto Assign |
| `WinManager chua duoc gan` | GoalTrigger thiếu reference | Kéo WinManager vào ô winManager của GoalTrigger |
| `GameOverManager chua duoc gan` | CarController thiếu reference | Kéo GameOverManager vào ô hornGameOverManager của Player_Car |
| `[TrafficJam] Tim thay 0 xe` | detectionRadius quá nhỏ hoặc trigger đặt sai chỗ | Tăng detectionRadius hoặc di chuyển TrafficJamZone lại gần xe NPC |

---

## Các script liên quan

| Script | Vị trí | Chức năng |
|---|---|---|
| `CarController_Scene5.cs` | `Assets/Scripts/scene5/` | Điều khiển xe player, hệ thống còi |
| `BlockingCar.cs` | `Assets/Scripts/scene5/` | Xe NPC lách đường khi bị còi |
| `trafficJamTrigger.cs` | `Assets/Scripts/scene5/` | Trigger tạo tình huống kẹt xe |
| `goalTrigger.cs` | `Assets/Scripts/scene5/` | Trigger đích đến |
| `hornGameOverManager.cs` | `Assets/Scripts/scene5/` | Màn hình Game Over khi còi quá 3 lần |
| `neighborManager.cs` | `Assets/Scripts/scene5/` | Phản ứng hàng xóm khi bị còi |
| `screenShake.cs` | `Assets/Scripts/scene5/` | Rung camera khi bấm còi |
| `winManager.cs` | `Assets/Scripts/scene5/` | Màn hình thắng khi đến đích |
| `HornUIController.cs` | `Assets/UI/` | UI cảnh báo còi xe trên HUD |
| `AutoAssignWaypoints.cs` | `Assets/Editor/` | Tool tự động gán waypoint cho 100+ xe |
