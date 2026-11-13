# LEGO 2K Drive Clone (Low-Poly / Voxel Edition - Unity)

Bản clone **LEGO 2K Drive** phong cách Low-Poly / Voxel bằng Unity C#, có thế giới mở, Garage tự lắp xe và đua xe dành cho người chơi thật qua 4 slot đăng ký (không có NPC).

## ESC, camera và đăng ký đua

- Khi chơi, con trỏ được ẩn và khóa vào giữa màn hình. Di chuột để nhìn quanh; `V` đổi góc nhìn thứ nhất/thứ ba. **Chuột trái phá block, chuột phải đặt block** theo dấu cộng ở giữa màn hình.
- `ESC` mở menu và thả chuột; bấm **Tiếp tục** hoặc `ESC` để khóa lại. Khi đang mở túi đồ, Garage, phòng multiplayer hoặc bảng đăng ký, `ESC` đóng bảng đó. Menu chặn di chuyển, nhảy, xây block và điều khiển xe. Offline tạm dừng game; online cuộc đua vẫn tiếp tục.
- Đi bộ vào đúng vòng tròn sáng xanh để mở bảng **4 slot**. Bấm **Xác nhận tham gia** để giữ một slot, đợi bạn bè đăng ký rồi bấm **Bắt đầu đua**. Chỉ những người đã xác nhận được đưa vào xe, mỗi người có vị trí xuất phát riêng; slot trống không sinh xe NPC. Offline có thể đua một mình.
- Bấm **Hủy slot**, đi ra khỏi vòng hoặc rời phòng để trả slot. `J` hoặc `R` khi đua đưa bạn về ngoài vòng đăng ký; muốn đua tiếp phải vào vòng và xác nhận lại.
- Multiplayer cần chạy/deploy cả bản server mới trong `MultiplayerServer` cùng client mới. Server cũ không hỗ trợ cấp slot; thay đổi này chưa tự deploy lên Railway. Chạy local: `node MultiplayerServer/server.js`, sau đó nhập `ws://localhost:8080` trong bảng `P`.

Kiểm thử server: `cd MultiplayerServer` rồi `npm test` (Node 22+ để chạy cả kiểm thử WebSocket). Trong Unity: menu **LEGO 2K Drive → Validate ESC, Camera and Race Slots** kiểm tra các luồng gameplay trong Play Mode rồi tự dừng.

---

## Cách Mở & Chạy Game Trong Unity Hub (Chỉ Mất 1 Bước)

1. Mở **Unity Hub** $\rightarrow$ Nhấn **Add** (hoặc **Open**) $\rightarrow$ Chọn thư mục `demo` trong repository này.
2. Mở project bằng phiên bản Unity bất kỳ bạn đang có trong máy (Unity 2021.3 LTS, 2022.3 LTS, hoặc Unity 6 đều tương thích).
3. Ngay khi Unity mở lên:
   * Script Editor [Lego2KDriveSceneSetupEditor.cs](Assets/Editor/Lego2KDriveSceneSetupEditor.cs) sẽ **tự động tạo và mở Scene** `Assets/Scenes/Lego2KDriveMain.unity`.
   * Nhấn nút **Play (▶)** ở giữa trên cùng (hoặc chọn menu **`LEGO 2K Drive -> Play Game Now!`**) để vào thế giới mở; đi tới vòng xanh để đăng ký đua.
   * *(Kể cả khi bạn đang ở một Scene trống bất kỳ và nhấn **Play**, cơ chế `[RuntimeInitializeOnLoadMethod]` trong [LegoRaceManagerAndHUD.cs](Assets/Scripts/UI/LegoRaceManagerAndHUD.cs) cũng sẽ tự động dựng toàn bộ đường đua, xe và HUD).*

---

## Các Cơ Chế Cốt Lõi Đã Triển Khai

1. **Biến Hình Tức Thì Theo 3 Địa Hình (Shapeshifting):**
   * **Đường Nhựa (`Street`):** Tự động biến thành **Street Supercar** (gầm thấp, cánh gió lớn).
   * **Đồi Đất Cát (`OffRoad`):** Tự động biến thành **Off-Road Monster Buggy** (gầm cao, 4 bánh gai khổng lồ, khung chống lật).
   * **Sông Nước (`Water`):** Tự động biến thành **Hydro Speedboat** (tàu cao tốc 2 thân lướt sóng, thu bánh xe, kích hoạt 2 động cơ phản lực nước).
2. **Hệ Thống Gạch Văng & Tự Hồi Phục (Voxel Brick Damage & Repair):**
   * Khi đâm tường mạnh, bị đối thủ húc lúc đang Boost, hoặc trúng tên lửa/mìn: Từng khối gạch LEGO bên ngoài sẽ **văng ra** và nảy trên mặt đất.
   * Nếu mất hết toàn bộ gạch vỏ ngoài: Xe **nổ tung toàn bộ mảnh gạch (WRECKED!)** và tự lắp ghép hồi sinh lại trên đường đua sau 1.8 giây.
   * **Đâm vỡ vật cản LEGO trên đường** (hàng rào gạch, thùng gạch): Các mảnh gạch văng ra và **tự động gắn lại vào xe để hồi máu + tăng Boost** (đúng cơ chế đặc trưng của LEGO 2K Drive).
3. **4 Loại Vũ Khí / Power-Up:**
   * **Homing Brick Missile:** Tên lửa gạch tự động khóa mục tiêu đối thủ phía trước và bắn văng 6 mảnh gạch của họ.
   * **Energy Brick Shield:** Khiên năng lượng bảo vệ xe trong 5.5 giây và húc văng gạch của bất kỳ xe nào chạm vào.
   * **Hazard Brick Mine:** Thả bẫy gạch phía sau khiến xe cán phải bị xoay vòng và văng gạch.
   * **Hyper Warp Boost:** Siêu tăng tốc bất tử tạm thời.
4. **LEGO Garage Brick Builder (`[TAB]`):**
   * Nhấn `TAB` bất kỳ lúc nào để mở Garage 3D, xoay camera quanh xe, chọn chỉnh sửa từng dạng (**Street / Off-Road / Boat**), chọn kích thước gạch, màu sắc và đặt/xóa từng viên gạch trực tiếp lên xe.

---

## Điều Khiển (Controls)

| Phím | Chức năng |
| :--- | :--- |
| `W / S` hoặc `↑ / ↓` | Tăng tốc / Phanh & Lùi |
| `A / D` hoặc `← / →` | Đánh lái Trái / Phải |
| `Left Shift` | **Drift / Power-Slide** (Ôm cua gắt + nạp đầy thanh Boost + kích hoạt Mini-Turbo) |
| `Space` | **Nhảy (Jump)** qua chướng ngại vật /Khi trong Garage: Đặt viên gạch |
| `E` hoặc `Left Ctrl` | **Kích hoạt Boost (Nitro)** (Có thể húc văng gạch xe đối thủ) |
| `F` hoặc `Enter` | **Sử dụng Vũ khí Power-Up** |
| `1` / `2` / `3` | Thử biến hình thủ công sang Xe Đua Đường Nhựa / Xe Địa Hình / Tàu Cao Tốc |
| `TAB` | Mở / Đóng **LEGO Garage Brick Builder** (Tự chế tạo xe) |
| `C` | Nhìn phía sau (Look Back) |
| `R` / `J` khi đua | Rời cuộc đua, về khu đăng ký slot |
| `ESC` | Mở/đóng menu, thả/khóa chuột; đóng bảng đang mở |
| `V` khi đi bộ | Đổi góc nhìn thứ nhất/thứ ba |
| `E` / `F` trong vòng xanh | Mở bảng xác nhận slot đua |
| `B` / `I` | Mở/đóng túi đồ |
| `P` | Mở/đóng phòng multiplayer |

