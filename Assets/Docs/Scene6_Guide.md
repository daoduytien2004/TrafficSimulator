# Hướng dẫn làm Scene 6 — Nhường đường xe cấp cứu
> Dành cho người mới, chưa biết nhiều về Unity. Đọc từng bước, đừng bỏ qua.

---

## Mục lục
1. [Kịch bản là gì?](#1-kịch-bản-là-gì)
2. [Chuẩn bị trước khi bắt đầu](#2-chuẩn-bị-trước-khi-bắt-đầu)
3. [Tạo Scene mới](#3-tạo-scene-mới)
4. [Thêm Tags mới](#4-thêm-tags-mới)
5. [Đặt xe người chơi (Player)](#5-đặt-xe-người-chơi-player)
6. [Tạo xe cấp cứu (Ambulance)](#6-tạo-xe-cấp-cứu-ambulance)
7. [Đặt Waypoints cho xe cấp cứu](#7-đặt-waypoints-cho-xe-cấp-cứu)
8. [Tạo Scene6Manager](#8-tạo-scene6manager)
9. [Tạo Trigger Zones](#9-tạo-trigger-zones)
10. [Tạo UI Panels](#10-tạo-ui-panels)
11. [Gắn tất cả vào Inspector](#11-gắn-tất-cả-vào-inspector)
12. [Test thử](#12-test-thử)
13. [Checklist cuối cùng](#13-checklist-cuối-cùng)
14. [Lỗi thường gặp](#14-lỗi-thường-gặp)

---

## 1. Kịch bản là gì?

**Tình huống**: Người chơi đang dừng đèn đỏ. Đột nhiên xe cấp cứu hú còi phía sau.

**Có 3 cách phản ứng:**

| Hành động | Kết quả |
|---|---|
| Đứng yên không nhúc nhích | ❌ Phạt — Vi phạm Điều 22 |
| Đạp ga vượt hẳn qua ngã tư | ❌ Phạt — Vi phạm Điều 11 |
| Lách nhẹ lên trước + nghiêng sang bên | ✅ Đúng — Xe cấp cứu đi qua được |

**Mục tiêu dạy**: Người học biết nhường đường xe ưu tiên mà không vi phạm đèn đỏ.

---

## 2. Chuẩn bị trước khi bắt đầu

### 2.1 Kiểm tra 4 script đã có chưa

Vào **Project panel** (góc dưới trái Unity), tìm thư mục:
```
Assets > Scripts > scene6
```

Bạn cần thấy 4 file này:
- `AmbulanceAI.cs`
- `Scene6Manager.cs`
- `EmergencyVehicleTrigger.cs`
- `VR_CarController_Scene6.cs`

> Nếu chưa có → chạy lại Claude để tạo script trước.

### 2.2 Chuẩn bị asset xe cấp cứu

Bạn cần một **3D model xe cấp cứu** (ambulance). Có thể:
- Dùng xe NPC đang có trong scene, đổi màu trắng + thêm sọc đỏ
- Tải từ Unity Asset Store (tìm "ambulance free")
- Tạm thời dùng một hộp (Cube) màu trắng để test

---

## 3. Tạo Scene mới

### Bước 3.1 — Tạo scene

1. Trên thanh menu trên cùng, bấm: **File → New Scene**
2. Chọn **Basic (URP)** → bấm **Create**
3. Bấm **File → Save As**
4. Đặt tên: `scene6`
5. Lưu vào thư mục: `Assets/Scenes/`

### Bước 3.2 — Thêm scene vào Build Settings

1. Bấm **File → Build Settings**
2. Kéo file `scene6.unity` từ Project panel vào ô **Scenes In Build**
3. Đóng cửa sổ Build Settings

---

## 4. Thêm Tags mới

> **Tag** là nhãn dán lên GameObject để script nhận diện. Scene 6 cần tag `Ambulance`.

### Bước 4.1 — Mở Tag Manager

1. Bấm **Edit → Project Settings**
2. Trong danh sách bên trái, bấm **Tags and Layers**

### Bước 4.2 — Thêm tag Ambulance

1. Tìm mục **Tags** ở trên cùng
2. Bấm dấu **+** (dấu cộng) ở góc phải
3. Gõ vào: `Ambulance`
4. Bấm **Enter** hoặc bấm ra ngoài để lưu

> Các tag `Player` và `Road` đã có sẵn từ scene trước, không cần thêm.

---

## 5. Đặt xe người chơi (Player)

> Nếu bạn đã có xe player từ scene trước, có thể copy nó sang.

### Bước 5.1 — Copy xe từ scene 5 (nếu có)

1. Mở **scene5.unity** (double-click vào file)
2. Trong **Hierarchy** (bảng bên trái), tìm GameObject xe player
3. Bấm chuột phải → **Copy**
4. Mở lại **scene6.unity**
5. Bấm chuột phải trong Hierarchy → **Paste**

### Bước 5.2 — Đổi script controller

1. Chọn GameObject xe player trong Hierarchy
2. Nhìn vào **Inspector** (bảng bên phải)
3. Tìm component `VR_CarController_Scene5` (hoặc `VR_CarController`)
4. Bấm **dấu ba chấm (⋮)** cạnh tên component → **Remove Component**
5. Kéo script **`VR_CarController_Scene6.cs`** từ Project panel vào Inspector
   > Hoặc bấm **Add Component** → gõ `VR_CarController_Scene6`

### Bước 5.3 — Đặt vị trí xe

1. Chọn xe player trong Hierarchy
2. Trong Inspector, tìm **Transform**
3. Đặt Position: `X=0, Y=0.5, Z=0`
   > Y=0.5 để xe không lún xuống đất
4. Đặt Rotation: `X=0, Y=0, Z=0`

### Bước 5.4 — Gán tag Player

1. Chọn xe player trong Hierarchy
2. Ở trên cùng Inspector, bấm vào ô **Tag** (mặc định là "Untagged")
3. Chọn **Player**

---

## 6. Tạo xe cấp cứu (Ambulance)

### Bước 6.1 — Tạo GameObject xe cấp cứu

**Nếu có model 3D**:
1. Kéo file model từ Project panel vào Hierarchy

**Nếu chưa có model (dùng tạm Cube để test)**:
1. Trong Hierarchy, bấm chuột phải → **3D Object → Cube**
2. Đặt tên: `Ambulance`
3. Đổi Scale: `X=2, Y=1.5, Z=4` (hình dạng giống xe)

### Bước 6.2 — Đặt vị trí ban đầu

> Xe cấp cứu sẽ bị ẩn khi bắt đầu, script tự hiện khi cần. Ta đặt ở vị trí spawn (Waypoint 0) để dễ nhìn khi thiết kế, sau đó script tự ẩn.

1. Position: `X=0, Y=0.5, Z=-30` (phía sau player 30m)

### Bước 6.3 — Gán tag Ambulance

1. Chọn `Ambulance` trong Hierarchy
2. Trong Inspector → ô **Tag** → chọn **Ambulance**

### Bước 6.4 — Thêm script AmbulanceAI

1. Chọn `Ambulance` trong Hierarchy
2. Bấm **Add Component** ở cuối Inspector
3. Gõ `AmbulanceAI` → chọn script

### Bước 6.5 — Thêm AudioSource cho còi hú

1. Chọn `Ambulance` trong Hierarchy
2. Bấm **Add Component** → gõ `Audio Source` → chọn
3. Trong component AudioSource vừa thêm:
   - **Spatial Blend**: kéo thanh trượt về **1** (3D hoàn toàn)
   - **Min Distance**: `5`
   - **Max Distance**: `50`
   - **Loop**: bật (check)

### Bước 6.6 — Tạo đèn nháy

> Đèn nháy là 2 ô màu đỏ và 2 ô màu xanh đặt trên nóc xe.

1. Chọn `Ambulance` trong Hierarchy
2. Bấm chuột phải vào `Ambulance` → **3D Object → Cube**
   - Đặt tên: `RedLight1`
   - Scale: `X=0.3, Y=0.1, Z=0.3`
   - Position (local): `X=-0.4, Y=0.8, Z=0.5`
   - Đổi material thành màu đỏ phát sáng
3. Làm tương tự cho `RedLight2` (X=0.4), `BlueLight1` (X=-0.4, Z=-0.5), `BlueLight2` (X=0.4, Z=-0.5) với màu xanh dương

> Tạm thời để test, bạn chỉ cần 1 RedLight và 1 BlueLight cũng được.

---

## 7. Đặt Waypoints cho xe cấp cứu

> Waypoint là các điểm đường dẫn — xe cấp cứu sẽ di chuyển từ điểm này sang điểm kia.

### Bước 7.1 — Tạo WaypointContainer

1. Trong Hierarchy, bấm chuột phải → **Create Empty**
2. Đặt tên: `WaypointContainer`
3. Position: `X=0, Y=0, Z=0`

### Bước 7.2 — Tạo 4 Waypoints

Bấm chuột phải vào `WaypointContainer` → **Create Empty** (làm 4 lần):

| Tên | Position | Ý nghĩa |
|---|---|---|
| `Waypoint_0` | `X=0, Y=0, Z=-30` | Điểm spawn xe cấp cứu (phía sau player 30m) |
| `Waypoint_1` | `X=0, Y=0, Z=-5` | Ngay sau đuôi xe player (5m sau vạch dừng) |
| `Waypoint_2` | `X=0, Y=0, Z=5` | Giữa ngã tư |
| `Waypoint_3` | `X=0, Y=0, Z=30` | Thoát khỏi ngã tư |

> **Lưu ý về trục Z**: Z âm = phía sau player, Z dương = phía trước (hướng ngã tư). Nếu scene của bạn dùng trục khác (X hoặc Z ngược), điều chỉnh lại cho phù hợp.

### Bước 7.3 — Gán Waypoints vào AmbulanceAI

1. Chọn `Ambulance` trong Hierarchy
2. Trong Inspector tìm component **AmbulanceAI**
3. Tìm field **Waypoints** → bấm mũi tên để mở rộng
4. Đổi **Size** thành `4`
5. Kéo từng Waypoint từ Hierarchy vào từng ô:
   - Element 0: kéo `Waypoint_0` vào
   - Element 1: kéo `Waypoint_1` vào
   - Element 2: kéo `Waypoint_2` vào
   - Element 3: kéo `Waypoint_3` vào

---

## 8. Tạo Scene6Manager

### Bước 8.1 — Tạo GameObject

1. Trong Hierarchy, bấm chuột phải → **Create Empty**
2. Đặt tên: `Scene6Manager`
3. Position: `X=0, Y=0, Z=0` (vị trí không quan trọng)

### Bước 8.2 — Gán script

1. Chọn `Scene6Manager` trong Hierarchy
2. Bấm **Add Component** → gõ `Scene6Manager` → chọn

### Bước 8.3 — Tạo StopLine Transform

> Đây là điểm đánh dấu vị trí vạch dừng xe — để tính xem player tiến xa bao nhiêu.

1. Trong Hierarchy, bấm chuột phải → **Create Empty**
2. Đặt tên: `StopLine`
3. Đặt Position đúng với vị trí vạch dừng thực tế trong scene của bạn
   > Ví dụ: `X=0, Y=0, Z=0` nếu player đứng tại Z=0

---

## 9. Tạo Trigger Zones

> Trigger Zone là vùng vô hình — khi một vật thể đi vào thì script sẽ phản ứng.

### Zone 1 — Kích hoạt xe cấp cứu (đặt phía sau player)

**Bước 9.1** — Tạo GameObject:
1. Trong Hierarchy → chuột phải → **Create Empty**
2. Đặt tên: `AmbulanceTriggerZone`
3. Position: `X=0, Y=1, Z=-15` (giữa khoảng cách W0 và W1, phía sau player)

**Bước 9.2** — Thêm Box Collider:
1. Chọn `AmbulanceTriggerZone`
2. Bấm **Add Component** → gõ `Box Collider` → chọn
3. Trong Box Collider:
   - **Is Trigger**: bật ✅ (quan trọng!)
   - **Size**: `X=8, Y=3, Z=2` (đủ rộng để xe cấp cứu chạy qua)
4. Bấm **Add Component** → gõ `EmergencyVehicleTrigger` → chọn
5. Trong script EmergencyVehicleTrigger:
   - **Is Ambulance Trigger**: bật ✅

### Zone 2 — Phát hiện player vượt ngã tư

**Bước 9.3** — Tạo GameObject:
1. Trong Hierarchy → chuột phải → **Create Empty**
2. Đặt tên: `IntersectionZone`
3. Position: `X=0, Y=1, Z=8` (bên kia vạch dừng, giữa ngã tư)

**Bước 9.4** — Thêm Box Collider:
1. Chọn `IntersectionZone`
2. Bấm **Add Component** → `Box Collider`
3. Trong Box Collider:
   - **Is Trigger**: bật ✅
   - **Size**: `X=10, Y=3, Z=8` (bao toàn bộ khu vực ngã tư)
4. Bấm **Add Component** → `EmergencyVehicleTrigger`
5. Trong script:
   - **Is Ambulance Trigger**: tắt ❌ (không check)

---

## 10. Tạo UI Panels

> Panels là các bảng thông báo hiện lên khi có sự kiện xảy ra.

### Bước 10.1 — Tạo Canvas

1. Trong Hierarchy → chuột phải → **UI → Canvas**
2. Chọn `Canvas` → trong Inspector:
   - **Render Mode**: `World Space` (vì đây là VR)
   - Đặt Canvas vào phía trước mặt player (trước kính lái)
   - Scale: `X=0.01, Y=0.01, Z=0.01`
   - Position: `X=0, Y=1.5, Z=1` (so với vị trí player)

> Nếu không biết dùng World Space Canvas, tạm thời dùng **Screen Space - Overlay** để test trước.

### Bước 10.2 — Tạo Panel_Blocked (Vi phạm Điều 22)

1. Bấm chuột phải vào `Canvas` → **UI → Panel**
2. Đặt tên: `Panel_Blocked`
3. Bấm chuột phải vào `Panel_Blocked` → **UI → Text - TextMeshPro**
4. Đặt tên: `MessageText`
5. Chọn `MessageText` → trong Inspector → ô **Text**: gõ:
   ```
   Vi phạm Điều 22
   Cản trở xe ưu tiên đang làm nhiệm vụ!
   
   Khi nghe tiếng còi xe cấp cứu,
   bạn phải lách sang bên để nhường đường.
   ```
6. Đổi màu chữ: màu trắng. Màu nền Panel: đỏ tối (alpha 80%)
7. **Tắt Panel này**: bỏ check ô nhỏ cạnh tên trong Inspector (để mặc định ẩn)

### Bước 10.3 — Tạo Panel_RanRed (Vi phạm Điều 11)

Làm giống bước 10.2, nhưng:
- Tên: `Panel_RanRed`
- Nội dung:
  ```
  Vi phạm Điều 11
  Vượt đèn đỏ, gây tai nạn!
  
  Nhường đường không có nghĩa là vượt ngã tư.
  Chỉ cần lách nhẹ trên vạch dừng là đủ.
  ```
- Màu nền: đỏ đậm

### Bước 10.4 — Tạo Panel_Win

Làm giống bước 10.2, nhưng:
- Tên: `Panel_Win`
- Nội dung:
  ```
  Hoàn thành kịch bản!
  
  Bạn đã nhường đường đúng cách.
  Lách nhẹ lên trước, nghiêng sang bên
  — xe cấp cứu đã đi qua được.
  ```
- Màu nền: xanh lá tối

### Bước 10.5 — Thêm nút Thử lại vào Panel_Blocked và Panel_RanRed

1. Bấm chuột phải vào `Panel_Blocked` → **UI → Button - TextMeshPro**
2. Đặt tên: `RetryButton`
3. Bấm vào `RetryButton` → trong Inspector tìm **On Click ()**
4. Bấm dấu **+**
5. Kéo `Scene6Manager` từ Hierarchy vào ô **None (Object)**
6. Bấm dropdown **No Function** → chọn `SceneManager → LoadScene (int)`
   > Hoặc tạo một hàm public `RestartScene()` trong Scene6Manager và gọi từ đây.

---

## 11. Gắn tất cả vào Inspector

Đây là bước quan trọng nhất — gắn các GameObject với nhau để script biết cái gì liên quan cái gì.

### 11.1 — Gắn cho Scene6Manager

1. Chọn `Scene6Manager` trong Hierarchy
2. Trong Inspector, tìm component **Scene6Manager** và gắn từng field:

| Field trong Inspector | Kéo cái gì vào |
|---|---|
| Ambulance | Kéo `Ambulance` từ Hierarchy |
| Player Transform | Kéo xe player từ Hierarchy |
| Stop Line Transform | Kéo `StopLine` từ Hierarchy |
| Red Light Countdown Text | Kéo TextMeshPro đếm ngược đèn đỏ từ Canvas |
| Panel Blocked | Kéo `Panel_Blocked` từ Hierarchy |
| Panel Ran Red | Kéo `Panel_RanRed` từ Hierarchy |
| Panel Win | Kéo `Panel_Win` từ Hierarchy |
| Panel Tutorial | Kéo panel hướng dẫn (nếu có) |
| Min Lateral Offset | Nhập: `1.2` |
| Max Forward Distance | Nhập: `3.5` |
| Red Light Duration | Nhập: `15` |
| Reset Delay | Nhập: `4` |
| Next Scene Index | Nhập index của scene tiếp theo |

### 11.2 — Gắn cho AmbulanceAI

1. Chọn `Ambulance` trong Hierarchy
2. Trong component **AmbulanceAI**:

| Field | Gắn vào |
|---|---|
| Waypoints → Size | `4` |
| Waypoints → Element 0..3 | Kéo lần lượt Waypoint_0..3 |
| Move Speed | `8` |
| Siren Audio | Kéo AudioSource trên xe cấp cứu |
| Siren Clip | Kéo file âm thanh còi hú |
| Red Light 1 | Kéo `RedLight1` |
| Red Light 2 | Kéo `RedLight2` (nếu có) |
| Blue Light 1 | Kéo `BlueLight1` |
| Blue Light 2 | Kéo `BlueLight2` (nếu có) |
| Blocked Detection Distance | `4` |
| Blocked Timeout | `8` |
| Scene 6 Manager | Kéo `Scene6Manager` |

### 11.3 — Gắn cho cả 2 EmergencyVehicleTrigger

**Zone 1 (AmbulanceTriggerZone)**:
1. Chọn `AmbulanceTriggerZone`
2. Trong **EmergencyVehicleTrigger**:
   - **Is Ambulance Trigger**: ✅ bật
   - **Scene 6 Manager**: kéo `Scene6Manager` vào

**Zone 2 (IntersectionZone)**:
1. Chọn `IntersectionZone`
2. Trong **EmergencyVehicleTrigger**:
   - **Is Ambulance Trigger**: ❌ tắt
   - **Scene 6 Manager**: kéo `Scene6Manager` vào

### 11.4 — Gắn cho VR_CarController_Scene6

1. Chọn xe player trong Hierarchy
2. Trong component **VR_CarController_Scene6**, gắn tương tự như CarController các scene trước:
   - Gear Display, Speed Display, Speed Slider
   - Tutorial Panel, Accident Panel
   - Engine Audio, Gas Audio, Brake Audio
   - VR Headset transform
   - Left/Right Mirror transforms
   - Left/Right Signal Lights
   - Signal Audio

---

## 12. Test thử

### Bước 12.1 — Test cơ bản

1. Bấm nút **Play** (tam giác ▶) ở trên cùng Unity
2. Dùng keyboard:
   - `2` → vào số D
   - `W` → tăng ga (tiến lên)
   - `A`/`D` → lái trái/phải
3. Đứng yên tại chỗ và chờ xem xe cấp cứu có xuất hiện không

### Bước 12.2 — Test nhanh bằng phím tắt

Trong `VR_CarController_Scene6.cs`, bạn có thể tạm thêm vào `Update()` để test nhanh:

```csharp
// Test: bấm F6 để kích hoạt kịch bản ngay lập tức
if (Input.GetKeyDown(KeyCode.F6))
{
    FindObjectOfType<Scene6Manager>()?.OnAmbulanceApproaching();
    Debug.Log("[TEST] Kích hoạt xe cấp cứu!");
}
```

Sau khi test xong, xóa đoạn code này đi.

### Bước 12.3 — Kiểm tra từng kết quả

**Test kết quả 1 — Đứng yên (bị phạt Điều 22)**:
- Bấm Play
- Bấm F6 để kích hoạt (nếu có thêm phím test)
- Không làm gì cả, chờ 8 giây
- Kết quả mong đợi: Panel_Blocked hiện ra

**Test kết quả 2 — Vượt đèn đỏ**:
- Bấm Play
- Kích hoạt kịch bản
- Bấm `2` → `W` → lao thẳng qua ngã tư
- Kết quả mong đợi: Panel_RanRed hiện ra

**Test kết quả 3 — Nhường đúng cách**:
- Bấm Play
- Kích hoạt kịch bản
- Bấm `2` → `W` nhích lên một chút (~1-2m) + `D` để nghiêng sang phải
- Đứng im chờ
- Kết quả mong đợi: Panel_Win hiện ra

---

## 13. Checklist cuối cùng

Trước khi nộp hoặc build, kiểm tra tất cả mục sau:

### Scene Setup
- [ ] Scene `scene6.unity` đã được lưu vào `Assets/Scenes/`
- [ ] Scene đã được thêm vào Build Settings
- [ ] Không có lỗi màu đỏ trong Console (Window → General → Console)

### Tags
- [ ] Tag `Ambulance` đã được tạo
- [ ] Xe cấp cứu đã gán tag `Ambulance`
- [ ] Xe player đã gán tag `Player`

### GameObjects
- [ ] `Scene6Manager` có script và đã gán đủ fields
- [ ] `Ambulance` có script `AmbulanceAI`, đủ waypoints, có AudioSource 3D
- [ ] `AmbulanceTriggerZone` có BoxCollider (Is Trigger = ON) + script
- [ ] `IntersectionZone` có BoxCollider (Is Trigger = ON) + script
- [ ] `StopLine` đặt đúng vị trí vạch dừng

### UI
- [ ] `Panel_Blocked` tắt mặc định, nội dung đúng
- [ ] `Panel_RanRed` tắt mặc định, nội dung đúng
- [ ] `Panel_Win` tắt mặc định, nội dung đúng
- [ ] Nút Thử lại hoạt động đúng

### Audio
- [ ] Âm thanh còi hú có file âm thanh được gán
- [ ] AudioSource của còi hú là Spatial Blend = 1 (3D)

### Test
- [ ] Kết quả "Đứng yên" → Panel_Blocked ✅
- [ ] Kết quả "Vượt đèn đỏ" → Panel_RanRed ✅
- [ ] Kết quả "Nhường đúng" → Panel_Win ✅
- [ ] Scene tự reset sau khi thất bại ✅

---

## 14. Lỗi thường gặp

### Lỗi: Xe cấp cứu không xuất hiện
**Nguyên nhân**: Script tự ẩn `gameObject.SetActive(false)` khi Start.
**Cách sửa**: Kiểm tra xem `AmbulanceTriggerZone` có trigger đúng không. Bật Console (`Window → General → Console`) và xem có dòng `[Ambulance] StartApproaching` không.

### Lỗi: "NullReferenceException" đỏ trong Console
**Nguyên nhân**: Bạn quên gắn một field nào đó trong Inspector.
**Cách sửa**: Đọc tên field trong thông báo lỗi, tìm trong Inspector và gắn vào.

### Lỗi: Player bị coi là "vượt đèn đỏ" dù chỉ nhích lên một chút
**Nguyên nhân**: `maxForwardDistance` trong Scene6Manager quá nhỏ.
**Cách sửa**: Chọn `Scene6Manager` → tăng giá trị `Max Forward Distance` lên `4` hoặc `5`.

### Lỗi: Xe cấp cứu đi qua nhưng vẫn không ra Panel_Win
**Nguyên nhân**: `minLateralOffset` quá lớn, player chưa lách đủ ngang.
**Cách sửa**: Chọn `Scene6Manager` → giảm `Min Lateral Offset` xuống `0.8`.

### Lỗi: Âm thanh còi hú nghe như phát từ không gian, không có cảm giác 3D
**Nguyên nhân**: AudioSource chưa set Spatial Blend = 1.
**Cách sửa**: Chọn `Ambulance` → tìm component `Audio Source` → kéo thanh **Spatial Blend** về **1**.

### Lỗi: Panel hiện ra nhưng không tắt sau khi reset
**Nguyên nhân**: `Time.timeScale = 0` nên scene bị đóng băng trước khi reset chạy xong.
**Cách sửa**: Đảm bảo coroutine dùng `WaitForSecondsRealtime` thay vì `WaitForSeconds` (đã được viết đúng trong script).

---

## Ghi chú về trục tọa độ

> Nếu xe player trong scene của bạn đứng theo hướng khác (không phải hướng trục Z), bạn cần điều chỉnh.

Mở `Scene6Manager.cs`, tìm hàm `OnAmbulancePassed()`:

```csharp
// Nếu xe player đứng hướng Z (tiến về Z dương) — mặc định:
float lateralMove = Mathf.Abs(playerTransform.position.x - startX);
float forwardMove = playerTransform.position.z - stopLineZ;

// Nếu xe player đứng hướng X (tiến về X dương):
float lateralMove = Mathf.Abs(playerTransform.position.z - startZ);
float forwardMove = playerTransform.position.x - stopLineX;
```

Chọn đoạn code phù hợp với scene của bạn và thay vào.

---

*Hướng dẫn này dành cho Scene 6 — VR Smart Traffic Instructor*
*Cập nhật: 2026-05-22*
