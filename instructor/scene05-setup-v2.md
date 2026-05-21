# Hướng dẫn Setup Scene 5 — Traffic Jam (v2)

> Phiên bản này áp dụng cho setup **kẹt xe từ đầu scene** + **đường dài trong thành phố**.

---

## Tổng quan kịch bản

Scene bắt đầu → xe NPC đã đứng kẹt sẵn trước mặt player → player bấm còi →
xe BlockingCar lách sang bên → player đi qua → đến GoalTrigger → hoàn thành kịch bản.
Nếu bấm còi quá 3 lần sai mục đích → hiện panel vi phạm Điều 8 → thử lại.

---

## Sơ đồ object trong Hierarchy

```
[Player_Car]
    └── VR_CarController_Scene5
            ├── hornGameOverManager → [GameOverManager]
            ├── neighborManager    → [NeighborManager]
            ├── screenShake        → [Main Camera]
            └── hornUI             → [HornHUD]

[TrafficJamZone]  ← BoxCollider (Is Trigger = true)
    └── TrafficJamTrigger
            ├── carController         → [Player_Car]
            ├── managedBlockingCars[] → [1–3 xe có BlockingCar script]
            ├── jamPoint              → [Empty Object "JamPoint"]
            └── triggerOnStart        ✓ (tick vào)

[JamPoint]  ← Empty Object đặt giữa khu vực kẹt

[CarContainer]  ← folder chứa toàn bộ xe NPC
    ├── xe BlockingCar (1–3 xe chặn đường)
    │       ├── FCG.TrafficCar
    │       └── BlockingCar  (startFrozen ✓)
    └── xe NPC thường (số còn lại)
            └── FCG.TrafficCar

[Lane_01], [Lane_02]...  ← FCGWaypointsContainer định tuyến xe NPC

[GoalTrigger]  ← BoxCollider (Is Trigger = true) ở cuối đường
    └── GoalTrigger
            ├── carController → [Player_Car]
            └── winManager    → [WinManager]
```

---

## BƯỚC 1 — Kiểm tra lỗi Console trước khi làm gì

1. Mở Unity → mở Scene 5
2. Nhấn **Play**
3. Kiểm tra Console (góc dưới Unity) — nếu có dòng đỏ thì fix trước
4. Nhấn **Play** lần nữa để dừng

**Lỗi hay gặp ngay lúc này:**

| Lỗi | Fix |
|---|---|
| `There are N audio listeners` | Xem BƯỚC 2 |
| `wCollider chưa được setup` | Click xe đó → FCG TrafficCar → **Generate WheelColliders** |
| `NullReferenceException` | Có ô Inspector nào đang để trống — xem BƯỚC 3 |

---

## BƯỚC 2 — Fix AudioListener (nếu có warning âm thanh)

1. Trong Hierarchy, gõ vào ô tìm kiếm: `t:AudioListener`
2. Giữ lại **đúng 1 cái** trên `Main Camera` bên trong `XR Origin`
3. Với các cái còn lại: click chọn → Inspector → **Audio Listener** → dấu ⋮ → **Remove Component**
4. **Ctrl + S** lưu scene

---

## BƯỚC 3 — Kiểm tra Player_Car có đủ tham chiếu

Click **Player_Car** → Inspector → cuộn đến section **"Horn — Tham chiếu hệ thống"**:

| Field | Gán vào object nào |
|---|---|
| `Neighbor Manager` | Object tên `NeighborManager` |
| `Horn Game Over Manager` | Object tên `GameOverManager` |
| `Screen Shake` | `Main Camera` có script `ScreenShakeController` |
| `Horn UI` | Object `HornHUD` có script `HornUIController` |

> **Cách gán:** Tìm object trong Hierarchy → giữ chuột kéo thả vào ô trống trong Inspector.

---

## BƯỚC 4 — Kiểm tra đường đi cho xe NPC (FCGWaypointsContainer)

FCG đã tự tạo sẵn các container đường khi generate thành phố — **không cần tạo tay**.

**Kiểm tra container đã có chưa:**

```
Hierarchy → ô tìm kiếm → gõ: t:FCGWaypointsContainer
```

- Có kết quả → đường đã sẵn sàng, sang Bước tiếp theo
- Không có kết quả → thành phố chưa được generate, cần chạy FCG trước

**Gán đường tự động cho tất cả xe (1 click):**

```
Menu trên cùng → Tools → "Auto Assign Waypoints cho tất cả BlockingCar"
```

Tool sẽ tự tìm container gần nhất cùng chiều và gán vào `atualWay` của từng xe.

> Nếu xe vẫn báo **"Không tìm thấy waypoint"**: click xe đó → xoay Y thêm 180° → chạy lại tool.

**Kiểm tra nhanh:** Nhấn Play → xe NPC bắt đầu di chuyển = đã đúng.

---

## BƯỚC 5 — Setup xe BlockingCar (xe chặn đường)

Xe BlockingCar là xe đứng ngay trước mặt player và lách ra khi bị còi.
Chỉ cần **1–3 xe** có component này, không phải tất cả.

**Tìm xe có BlockingCar:**

```
Hierarchy → ô tìm kiếm → gõ: t:BlockingCar
```

**Với từng xe tìm được:**

1. Click xe → Inspector → component **Blocking Car**
2. Tick vào **Start Frozen** ✓ — xe đứng yên chờ trigger kích hoạt
3. Kiểm tra **Listen Radius** = `25` (khoảng cách nghe thấy còi)
4. Chọn **Move Right**: `true` = lách phải, `false` = lách trái
5. Ô **End Point**: để trống — TrafficJamTrigger sẽ tự set khi kích hoạt

> **Với xe NPC thường** (không cần lách khi bị còi): xóa component BlockingCar đi
> Inspector → BlockingCar → dấu ⋮ → **Remove Component**

---

## BƯỚC 6 — Setup TrafficJamTrigger (kẹt xe từ đầu scene)

1. Click **TrafficJamZone** trong Hierarchy
2. Inspector → **TrafficJamTrigger** → điền:

| Field | Giá trị |
|---|---|
| `Car Controller` | Kéo **Player_Car** vào |
| `Detection Radius` | `500` (bao phủ toàn bộ khu vực xe NPC) |
| `Jam Duration` | `30` |
| `Managed Blocking Cars` | Nhấn **+**, kéo 1–3 xe có BlockingCar vào |
| `Jam Point` | Kéo object **JamPoint** vào |
| **`Trigger On Start`** | **Tick ✓** — kẹt xe ngay khi scene bắt đầu |

**Tạo JamPoint (nếu chưa có):**

```
Hierarchy → chuột phải → Create Empty → đặt tên "JamPoint"
Dùng công cụ Move (phím W) → kéo ra đặt giữa khu vực xe đang kẹt trong Scene view
Kéo object JamPoint vào ô Jam Point của TrafficJamTrigger
```

**Kiểm tra BoxCollider của TrafficJamZone:**
- Scene view phải thấy hộp xanh lá bao phủ khu vực xe
- Inspector → **Box Collider** → **Size** và **Center** nếu cần chỉnh

---

## BƯỚC 7 — Tạo GoalTrigger (đích đến)

1. Hierarchy → chuột phải → **Create Empty** → đặt tên `GoalTrigger`
2. Di chuyển ra **cuối đoạn đường** trong Scene view
3. Inspector → **Add Component** → **Box Collider**
   - Tick **Is Trigger** ✓
   - **Size**: X=10, Y=3, Z=5 (chỉnh cho vừa chiều rộng đường)
4. **Add Component** → **Goal Trigger**
5. Gán:
   - `Car Controller` → kéo **Player_Car** vào
   - `Win Manager` → kéo object **WinManager** vào

---

## BƯỚC 8 — Test hoàn chỉnh

Nhấn **Play**, kiểm tra theo thứ tự:

| Kết quả mong đợi | Nếu không đúng |
|---|---|
| Xe NPC đứng yên hoặc bò chậm ngay khi bắt đầu | `triggerOnStart` chưa tick, hoặc `detectionRadius` quá nhỏ |
| Console in `[HornSystem] Đã vào zone kẹt xe` ngay lúc đầu | `carController` chưa được gán trong TrafficJamTrigger |
| Nhấn `Space` → xe BlockingCar lách sang bên | `listenRadius` quá nhỏ hoặc xe ngoài tầm |
| Nhấn `Space` 3 lần khi không có BlockingCar gần → Game Over | `hornGameOverManager` chưa gán |
| Lái đến cuối đường → màn hình hoàn thành | `winManager` chưa gán trong GoalTrigger |

**Phím test nhanh (không cần lái xe):**

| Phím | Tác dụng |
|---|---|
| `F7` | Bấm còi thủ công |
| `F8` | Thoát jam zone (xe NPC chạy lại) |
| `F9` | Reset đếm còi về 0 |

---

## Lỗi thường gặp & cách fix

| Lỗi trong Console | Nguyên nhân | Fix |
|---|---|---|
| `[TrafficJam] Tim thay 0 xe` | `detectionRadius` quá nhỏ | Tăng lên `500` |
| `[BlockingCar] wCollider chưa setup` | Xe chưa Configure | Click xe → FCG TrafficCar → **Generate WheelColliders** |
| `[BlockingCar] Không tìm thấy waypoint` | Xe quay ngược chiều đường | Xoay xe 180° |
| `WinManager chua duoc gan` | GoalTrigger thiếu reference | Kéo WinManager vào ô `winManager` |
| `GameOverManager chua duoc gan` | CarController thiếu reference | Kéo GameOverManager vào ô `hornGameOverManager` |
| Xe BlockingCar chạy lung tung từ đầu | `startFrozen` chưa tick | Tick **Start Frozen** ✓ trong Inspector |
| Còi bật ngay từ đầu nhưng xe không lách | BlockingCar ngoài `listenRadius` | Tăng `listenRadius` lên `30–50` |

---

## Tóm tắt checklist trước khi báo cáo

- [ ] Console không có dòng đỏ khi Play
- [ ] Xe NPC đứng yên / bò chậm ngay khi scene bắt đầu
- [ ] Bấm `Space` → xe BlockingCar lách ra
- [ ] Bấm `Space` 3 lần sai → panel vi phạm Điều 8 hiện ra
- [ ] Lái đến GoalTrigger → màn hình hoàn thành kịch bản
