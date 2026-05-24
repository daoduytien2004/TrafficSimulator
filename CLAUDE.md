# TrafficSimulator - CLAUDE.md

## Tổng quan dự án

**VR Smart Traffic Instructor** — Môi trường diễn tập giao thông VR giúp người học luyện phản xạ lái xe ô tô trước khi thực hành thực tế. Người dùng mục tiêu: người từ 18 tuổi chuẩn bị thi bằng lái hạng B, không cần kinh nghiệm VR, tự luyện độc lập không cần giáo viên.

- **Engine**: Unity (project name: "VR"), URP
- **XR SDK**: XR Interaction Toolkit 2.6.5
- **Ngôn ngữ**: C#
- **Phần cứng mục tiêu**: Meta Quest 2/3, Pico 4, HTC Vive, PCVR
- **Branch chính**: `main`, branch phát triển: `develop`
- **Cơ sở pháp lý**: Luật Trật tự, An toàn Giao thông Đường bộ số 36/2024/QH15 (hiệu lực 01/01/2025)

## 6 Kịch bản (Scenes) — Nội dung giáo dục

| Scene | Tên kịch bản | Luật áp dụng | Trạng thái |
|---|---|---|---|
| 1 | Tutorial cơ bản — điều khiển xe | — | Hoàn chỉnh |
| 2 | Nhường đường người đi bộ | Điều 11, 30 | Hoàn chỉnh |
| 3 | Xi-nhan + kiểm tra gương trước khi rẽ/chuyển làn | Điều 14, 15 | Hoàn chỉnh |
| 4 | Tình huống khuất tầm nhìn (xe đỗ trái phép che khuất) | Điều 18, 19 | Hoàn chỉnh |
| 5 | Kẹt xe — hệ thống còi (bấm còi sai mục đích) | Điều 8 | Hoàn chỉnh |
| **6** | **Nhường đường xe ưu tiên (xe cấp cứu)** | **Điều 22** | **Đang phát triển** |

## Scene 6 — Chi tiết kịch bản xe cấp cứu

**Bối cảnh**: Xe người dùng đang dừng đèn đỏ ở làn trái ngoài cùng, phía trước là vạch dừng, hai bên và phía sau bị vây kín bởi xe khác. Đèn đỏ còn **15 giây**.

**Trigger**: Xe cấp cứu hú còi Spatial Audio tiến sát phía sau.

**Dilemma — hai lựa chọn sai:**
- Đứng yên → cản trở xe ưu tiên → trừ điểm
- Vượt hẳn đèn đỏ qua ngã tư → va chạm xe cắt ngang → tai nạn

**Hành động đúng**: Lách xe lên trên vạch dừng một chút, nghiêng sang phải/trái để tạo khe hở — không vượt qua ngã tư. Người dùng phải nhìn gương để tìm "khoảng trống an toàn".

**Kỹ năng rèn luyện**: Quan sát gương chiếu hậu dưới áp lực tâm lý cao, ra quyết định khi đối mặt tình huống tiến thoái lưỡng nan.

**Scripts cần tạo cho Scene 6**:
- `AmbulanceAI.cs` — xe cấp cứu chạy theo đường có Spatial Audio, còi hú, đèn nháy
- `EmergencyVehicleTrigger.cs` — trigger zone kích hoạt kịch bản, theo dõi hành vi người dùng
- `Scene6Manager.cs` — quản lý trạng thái: WAITING_AT_RED → AMBULANCE_APPROACHING → PLAYER_YIELDED / PLAYER_BLOCKED / PLAYER_RAN_RED

## Cấu trúc thư mục

```
Assets/
├── Scripts/
│   ├── CarController.cs          # Controller xe người chơi (VR_CarController)
│   ├── scene3/
│   │   ├── TrafficAI.cs          # AI xe giao thông theo waypoint
│   │   ├── TurnScenario.cs       # Quản lý kịch bản rẽ + xe máy phạt
│   │   ├── PunishmentMoto.cs     # AI xe máy trừng phạt
│   │   ├── FloatingIndicator.cs  # Biển chỉ hướng nảy lên xuống
│   │   └── FinishZone.cs         # Vùng đích đến
│   └── scene5/
│       ├── CarController_Scene5.cs   # Controller xe scene 5 + hệ thống còi
│       ├── BlockingCar.cs            # Xe NPC lách đường khi bị còi
│       ├── trafficJamTrigger.cs      # Trigger tạo tình huống kẹt xe
│       ├── hornGameOverManager.cs    # Game Over khi còi quá 3 lần
│       ├── neighborManager.cs        # Phản ứng hàng xóm khi bị còi
│       ├── screenShake.cs            # Rung camera khi bấm còi
│       ├── winManager.cs             # Màn hình thắng
│       └── HornUIController.cs       # UI cảnh báo còi trên HUD
├── Fantastic City Generator/Scripts/
│   └── TrafficCar.cs             # NPC traffic với waypoint, collision avoidance, traffic light
├── Scenes/
│   ├── Scene1.unity
│   ├── scene3.unity
│   ├── scene5.unity
│   └── scene6.unity              # Đang xây dựng
└── Samples/XR Interaction Toolkit/2.6.5/
    ├── Starter Assets/
    └── XR Device Simulator/      # Test không có headset
```

## Scripts chính

### `CarController.cs` — `VR_CarController`
Controller chính của xe người chơi.

**Input**:
- VR: `InputActionReference` cho 3 nút D/R/N trên tay cầm
- Keyboard fallback: `1`=N, `2`=D, `3`=R; `Q`=xi-nhan trái, `E`=xi-nhan phải; `Vertical`/`Horizontal` axis

**Hệ thống số**: `N` (Neutral) trôi chậm, `D` (Drive) tiến xanh, `R` (Reverse) lùi đỏ

**Hệ thống xi-nhan**: `isLeftSignalOn` / `isRightSignalOn`, nhấp nháy `blinkInterval` 0.5s, phát âm "tạch", bật một bên tự tắt bên kia

**Kiểm tra gương**: `hasLookedLeftMirror` / `hasLookedRightMirror` — set true khi VR headset nhìn về phía gương (góc < 20°), reset mỗi khi vào TurnScenario mới

**Tai nạn** (`OnCollisionEnter`): bỏ qua tag "Road"; bỏ qua tốc độ thấp (< 2f) trừ xe máy; hiện `accidentPanel`, `timeScale = 0`, reload sau `resetDelay`

### `TurnScenario.cs`
Trigger zone tại điểm rẽ. **4 loại** (`TurnType`): `Left`, `Right`, `Straight`, `Finish`.

Vào zone → reset flag gương → spawn `PunishmentMoto` phía sau. Mỗi frame: đủ điều kiện (xi-nhan + gương) → `SlowDown()`; bắt đầu rẽ mà thiếu điều kiện → `SpeedUpToCrash()`. Ra khỏi zone → xóa xe máy sau 4-5s. `hasPassed = true` ngay từ đầu nếu là `Straight`.

### `PunishmentMoto.cs`
AI xe máy 4 trạng thái: `isFollowing` (bám 8m), `isCrashing` (45f), `isSlowingDown` (2f), `isPassing` (max(carSpeed+10, 25)). Pitch engine 0.8→2.5. Xe đứng yên < 3f và < 15m → tự `PassStraight()`.

### `CarController_Scene5.cs`
Mở rộng từ CarController. Thêm hệ thống còi 3 cảnh báo: mỗi lần bấm còi sai → screen shake + neighbor reaction + UI warning; 3 lần → Game Over.

### `TrafficCar.cs` (Fantastic City Generator)
NPC traffic: waypoint following, collision avoidance (raycast), traffic light detection, horn khi bị chặn >3s, đèn đêm (theo giờ hệ thống), brake smoke particles, `lateralOffset` để lách làn.

### `BlockingCar.cs`
Xe NPC chặn đường. Phát hiện còi qua `listenRadius`, lách sang bên và tăng tốc đến `endPoint`. Tích hợp với `TrafficJamTrigger`.

## Luồng mô phỏng (không phải game — không có win/lose)

Mỗi kịch bản có 3 phần: **Tình huống xảy ra → Học viên phản ứng → Phản hồi giáo dục**.

**Kịch bản 3 (xi-nhan/gương)**:
```
Số N → Số D → Vào TurnScenario → Xe máy xuất hiện phía sau
→ [Xi-nhan + Gương đúng] → Xe máy nhường → Hoàn thành kịch bản
→ [Rẽ thiếu điều kiện] → Mô phỏng va chạm → Panel "Vi phạm Điều 15" + giải thích → Thử lại
→ FinishZone → Chuyển sang kịch bản tiếp theo
```

**Kịch bản 5 (còi xe)**:
```
Vào TrafficJamZone → NPC dừng → BlockingCar chặn đường
→ [Bấm còi đúng lúc] → BlockingCar lách sang → Hoàn thành kịch bản
→ [Bấm còi sai/quá 3 lần] → Panel "Vi phạm Điều 8" + giải thích → Thử lại
→ GoalTrigger → Chuyển sang kịch bản tiếp theo
```

**Kịch bản 6 (xe cấp cứu)**:
```
Dừng đèn đỏ → Xe cấp cứu tiếp cận từ phía sau (còi Spatial Audio)
→ [Đứng yên] → Panel "Vi phạm Điều 22 — cản trở xe ưu tiên" + giải thích → Thử lại
→ [Vượt hẳn qua ngã tư] → Mô phỏng va chạm → Panel "Vi phạm Điều 11 — vượt đèn đỏ" → Thử lại
→ [Lách nhẹ trên vạch + nghiêng sang bên] → Xe cấp cứu đi qua → Hoàn thành → DebriefingScene
```

**DebriefingScene (cuối cùng)**:
```
LearningProgress.GetReport() → Hiện danh sách vi phạm từng kịch bản
→ Điều khoản cụ thể bị vi phạm + số lần
→ Khuyến nghị: "Ôn lại Điều X trước khi thi sát hạch"
```

## Tags Unity quan trọng

- `Player` — xe người chơi
- `Road` — mặt đường (bỏ qua va chạm)
- `EnemyMoto` — xe máy AI (luôn kích hoạt tai nạn dù tốc độ thấp)

## Cấu hình Audio

- `engineAudio`: Startup clip → idle loop, pitch theo tốc độ
- `gasAudio`: Phát khi nhấn ga
- `brakeAudio`: Phát khi phanh (tốc độ > 1)
- `crashAudioSource`: Tiếng va chạm
- `signalAudio`: Tiếng "tạch" xi-nhan
- **Spatial Audio**: Tiếng còi xe cấp cứu (Scene 6), tiếng còi kẹt xe (Scene 5) gắn vị trí 3D

## Git workflow

- `main` — production
- `develop` — tích hợp chính
- Feature branches: `feat/scene-X`
- PR vào `develop` trước, merge `develop` → `main` khi hoàn chỉnh

## Lưu ý kỹ thuật

- `Time.timeScale = 0` khi tai nạn/chiến thắng → dùng `WaitForSecondsRealtime` trong coroutine reset
- TrafficAI dùng `Vector3.back` và `LookRotation(-direction)` vì model xe có trục Z ngược
- TurnScenario spawn xe máy bằng Raycast xuống mặt đường để canh đúng độ cao
- `hasPassed` trong TurnScenario: `true` ngay từ đầu nếu là `Straight`
- GPU Instancing cho vật thể lặp (cây, cột đèn) để giữ 72–90 FPS trên VR
- Static Reference Frame (cabin cố định) để giảm say VR (Motion Sickness)
- Light Baking thay real-time lighting để giảm tải GPU

## Điều khoản pháp lý liên quan trực tiếp đến từng scene

| Scene | Điều khoản |
|---|---|
| 2 (Người đi bộ) | Điều 11, Điều 30 |
| 3 (Xi-nhan/gương) | Điều 14, Điều 15 |
| 4 (Khuất tầm nhìn) | Điều 18, Điều 19 |
| 5 (Còi xe) | Điều 8 (hành vi bị nghiêm cấm) |
| 6 (Xe cấp cứu) | Điều 22 (quyền ưu tiên một số loại xe) |
