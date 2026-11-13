# TÀI LIỆU Ý TƯỞNG THIẾT KẾ: HỆ THỐNG TRADING XE ĐUA BẰNG TOKEN & CƠ CHẾ ĐÀO QUẶNG (KINETIC MINING)

> **Tóm tắt dự án:** Game Đua Xe Thuần Túy (Pure Arcade Racing) tích hợp tài sản NFT và kinh tế Token, nơi độ hiếm của xe được quyết định bởi vật liệu chế tác, và vật liệu được khai thác thông qua cơ chế va chạm tốc độ cao ngay trên đường đua.

---

## 1. Cơ Chế Tính Toán Độ Hiếm Của Xe & Cạnh Tranh Giá Cả

### 1.1. Cấu Trúc Phân Rã Linh Kiện (Modular Car Architecture)
Một chiếc xe đua hoàn chỉnh được bóc tách thành **5 module độc lập**, mỗi module đóng góp trọng số ($W_i$) khác nhau vào giá trị tổng thể:

| Linh kiện | Trọng số ($W_i$) | Ảnh hưởng gameplay |
| :--- | :---: | :--- |
| **Động cơ (Engine)** | **30%** | Tốc độ tối đa (Top Speed) và gia tốc (Acceleration). |
| **Khung xe (Chassis)** | **25%** | Độ bền (Durability), khả năng chống chịu va đập, trọng lượng xe. |
| **Hệ thống dẫn động / Bánh (Wheels)** | **15%** | Độ bám đường, khả năng drift, thời gian bắt tốc độ sau cua. |
| **Hệ thống tản nhiệt / Nitro (Booster)** | **15%** | Dung lượng bình Nitro, tốc độ hồi Nitro, thời gian duy trì Boost. |
| **Skin / Ngoại trang / Decal (Visuals)** | **15%** | Hiệu ứng hạt (Particle VFX), vệt sáng nitro, độ độc bản thẩm mỹ. |

---

### 1.2. Công Thức Tính Điểm Độ Hiếm (Car Rarity Score - CRS)
Điểm số của từng linh kiện $R_{\text{part}}$ được xác định bởi cấp độ vật liệu (Material Tier) và độ hiếm ngoại trang (Skin Tier):

$$R_{\text{part}} = (\text{Material Tier} \times M_{\text{weight}}) + (\text{Skin Tier} \times S_{\text{weight}})$$

Điểm độ hiếm tổng hợp của chiếc xe (**CRS**) được tính theo công thức:

$$\text{CRS} = \left( \sum_{i=1}^{n} W_i \times R_{\text{part}, i} \right) \times (1 + \text{Bonus}_{\text{Synergy}}) + \text{Bonus}_{\text{Serial}}$$

* **Bonus Synergy (Cộng hưởng đồng bộ set):**
  * Lắp nguyên bộ từ cùng một loại vật liệu (ví dụ: Full Diamond từ khung đến bánh) $\rightarrow$ Thưởng thêm **+15% đến +25% CRS**, đồng thời kích hoạt hiệu ứng hào quang đặc biệt.
  * Thúc đẩy người chơi săn lùng và giao dịch các mảnh linh kiện còn thiếu trên sàn Marketplace.
* **Bonus Serial / Gen:**
  * Xe thuộc lứa đúc đầu tiên (Gen-0) hoặc có số thứ tự đẹp (#001, #777) được cộng thêm điểm sưu tầm, bảo chứng giá trị cho nhà đầu tư sớm.

---

### 1.3. Bảng Thuộc Tính Vật Liệu & Meta Đua Xe (Racing Archetypes)
Tránh việc xe đắt tiền chỉ đơn thuần là "Pay-to-Win", mỗi vật liệu được thiết kế phục vụ một trường phái đua riêng biệt:

| Vật liệu | Đặc tính vật lý | Ảnh hưởng chỉ số đua | Phong cách / Lối chơi phù hợp |
| :--- | :--- | :--- | :--- |
| **Sắt (Iron)** | Nặng, Cứng cáp | Tăng độ bền, húc xe khác mạnh, giảm độ nảy va đập. | Đua đường gồ ghề, húc chướng ngại vật, xe cản đường. |
| **Vàng (Gold)** | Nặng, Dẻo, Quý hiếm | Tăng **% Bonus Token/Thưởng** sau trận, hiệu năng đua ở mức khá. | Farm Token (dành cho người chơi cày kinh tế). |
| **Kim Cương (Diamond)** | Siêu cứng, Siêu nhẹ | Tối đa hóa **Top Speed** và **Gia tốc**, độ bền cực cao. | Đua đường thẳng (Drag race), giải đấu tốc độ cao. |
| **Lục Bảo (Emerald)** | Dẫn truyền năng lượng | Tăng tốc độ hồi nạp Nitro, drift giữ vận tốc tốt nhất. | Đua đường đèo nhiều khúc cua gấp (Technical Drift). |

---

## 2. Cơ Chế Đào Quặng Trong Game Đua Xe (Kinetic Mining)

### 2.1. Triết Lý Thiết Kế
* **Không dùng cơ chế đào tĩnh:** Người chơi không xuống xe cầm cuốc đào (gây mất nhịp độ đua tốc độ cao).
* **Khai thác bằng động năng (Kinetic Harvesting):** Người chơi sử dụng chính tốc độ và lực đâm va của xe để phá vỡ các khối quặng địa chất phân bố trên đường đua.

---

### 2.2. Ngưỡng Tốc Độ Đâm Va & Phần Thưởng

| Loại quặng | Ngưỡng tốc độ yêu cầu | Phần thưởng khi đâm vỡ | Hiệu ứng hỗ trợ tức thì khi đua |
| :--- | :---: | :--- | :--- |
| **Quặng Sắt** | $\ge 35\text{ km/h}$ | 1 Sắt (Nguyên liệu cơ bản) + Ít Token | Hồi 1 mảnh giáp HP, nạp +15% Nitro. |
| **Quặng Vàng** | $\ge 65\text{ km/h}$ | 1 Vàng (Nguyên liệu hiếm) + Token trung bình | Hồi 2 mảnh giáp HP, nạp +35% Nitro. |
| **Quặng Kim Cương** | $\ge 95\text{ km/h}$ *(hoặc đang bật Nitro)* | 1 Kim Cương (Linh kiện Epic) + Lượng lớn Token | **Hồi đầy 100% thanh Nitro ngay lập tức** để tiếp tục bứt tốc. |
| **Quặng Lục Bảo** | $\ge 80\text{ km/h}$ *(hoặc đang Drift)* | 1 Lục Bảo (Linh kiện Legend) + Lượng lớn Token | Kích hoạt Drift Surge, nạp +60% Nitro. |

---

### 2.3. Quy Tắc Va Chạm (Shatter vs. Deflection)
1. **Đâm thành công (Đủ tốc độ):**
   * Khối quặng nổ tung thành các mảnh tinh thể phát sáng văng ra vật lý (`Rigidbody`).
   * Âm thanh gạch vỡ giòn giã, hiện thông báo nhận tài nguyên và Token trên HUD.
   * Khối quặng biến mất và tự hồi sinh (respawn) sau một khoảng thời gian (6 – 8 giây).
2. **Đâm thất bại (Tốc độ quá thấp):**
   * Khối quặng quá cứng không vỡ.
   * Xe bị lực phản hồi hất nảy ngược lại, giảm tốc độ nghiêm trọng.
   * Hiện cảnh báo: *"TỐC ĐỘ QUÁ THẤP! CẦN >95 KM/H HOẶC BẬT NITRO ĐỂ PHÁ KIM CƯƠNG!"*.

---

## 3. Kiến Trúc Map Khai Thác 10 Người: Hẻm Núi Khổng Lồ (Grand Canyon Quarry Circuit)

### 3.1. Thiết Kế Không Gian & Độ Dốc Ngoằn Ngoèo (Verticality & Canyon Topography)
Đường đua không phải là mặt phẳng bằng phẳng thông thường, mà là một **Đại Hẻm Núi (Grand Abyss Canyon)** khổng lồ với quy mô hoành tráng:
* **Địa hình:** Các vách đá sa thạch cao chọc trời, đường đua uốn lượn ngoằn ngoèo hình xoắn ốc (corkscrew), bám quanh sườn vách đá dựng đứng và đan xen qua các hẻm sâu không đáy.
* **Cảm giác thị giác:** Đứng từ trên đỉnh có thể nhìn sâu hoắm xuống vực thẳm mù sương phía dưới, nơi lấp lánh ánh sáng phát quang của các mỏ khoáng thạch cổ xưa.

---

### 3.2. Phân Tầng Độ Khó Tiếp Cận: Vị Trí Càng Hiểm – Quặng Càng Quý

Nguyên tắc cốt lõi: **"Vị trí xe càng khó với tới, độ rủi ro càng cao thì quặng càng siêu hiếm"**.

```
[ TẦNG THƯỢNG: ĐƯỜNG CHÍNH / VÁCH CAO ] 
  ├── Đặc điểm: Làn đường rộng, có rào chắn, dễ lái.
  └── Quặng: SẮT (Iron) ──> Dễ đâm trúng, an toàn tuyệt đối.
           │
[ TẦNG TRUNG: GỜ VÁCH ĐÁ & CUA NGOẰN NGOÈO ]
  ├── Đặc điểm: Lòng đường hẹp, cua tay áo sát mép vực, không rào chắn.
  └── Quặng: VÀNG (Gold) ──> Đòi hỏi kỹ thuật drift bám mép cua.
           │
[ ĐÁY HẺM NÚI & MỎM ĐÁ CHEO LEO: TỬ ĐỊA KHO BÁU ]
  ├── Đặc điểm: Rơi tự do, dốc nhảy không trung (Mega Ramps), trụ đá chênh vênh.
  └── Quặng: KIM CƯƠNG & LỤC BẢO ──> Vị trí cực kỳ hiểm hóc, cần kỹ năng bay chuẩn xác.
```

#### A. Tầng Thượng (Upper Canyon Rim - Mặt đường an toàn):
* Đường đua chính dành cho các tay đua ưu tiên vị trí và thứ hạng ổn định.
* Quặng **Sắt (Iron)** rải rác ở hai bên lề đường thẳng, bất kỳ xe nào chạy qua cũng có thể húc vỡ dễ dàng.

#### B. Tầng Trung (Mid-Cliff Ledges - Cung đường ngoằn ngoèo mạo hiểm):
* Các đoạn đường đèo hẹp men theo vách đá, một bên là vách núi sừng sững, một bên là vực thẳm.
* Quặng **Vàng (Gold)** mọc sát mép rìa vực. Nếu vào cua quá đà sẽ văng xe xuống vực; nếu rụt rè sẽ không với tới quặng.

#### C. Lòng Vực & Các Vị Trí "Khó Với Tới" (The Abyss Bottom & Stunt Pillars):
* Dưới đáy vực hoặc trên các **cột đá tự nhiên cô lập (Needle Rock Pillars)** giữa lòng hẻm núi:
  * Nơi tọa lạc của **Quặng Kim Cương (Diamond)** và **Lục Bảo (Emerald)**.
  * **Cách tiếp cận:**
    1. **Mega Stunt Jump (Cú nhảy sinh tử):** Người chơi phải kích hoạt Nitro đạt tốc độ tối đa $\ge 120\text{ km/h}$, phóng khỏi bàn đạp dốc đá nhô ra vực để bay lơ lửng trên không trung và húc trúng khối Kim Cương treo lơ lửng giữa 2 vách núi.
    2. **Deep Dive Shortcut (Đường tắt lao dốc đáy vực):** Một nhánh đường phụ dốc đứng lao thẳng xuống đáy hẻm núi tăm tối, uốn lượn qua các khe hẹp đầy quặng quý. Dưới đáy vực sẽ có **Bàn Đẩy Phản Lực (Geyser / Jump Pad)** phóng vọt xe trở lại đường đua chính ở tầng trên.

---

### 3.3. Cơ Chế Rủi Ro & Phần Thưởng Cực Hạn (Extreme High Risk - High Reward)
* **Cái giá của sự tham lam (Risk):**
  * Căn góc nhảy hụt, thiếu tốc độ hoặc va chạm với xe đối thủ trên không $\rightarrow$ Rơi thẳng xuống vực thẳm.
  * Xe sẽ bị nổ tung gạch (WRECKED!), tốn 2.5–3 giây hồi sinh về lại đường đua chính $\rightarrow$ Mất cơ hội tranh giải Top 1.
* **Phần thưởng cho tay lái cừ khôi (Reward):**
  * Húc trúng khối Kim Cương giữa không trung: Nhận lượng Token $CAR khổng lồ + Thanh Nitro lập tức nạp đầy 100%, tạo lực phản lực đẩy xe bay thẳng tới đoạn đường tiếp theo.
* **Tính cạnh tranh trong phòng 10 người:**
  * Mỗi vòng đua, mỏ quặng đáy vực chỉ có 1 khối duy nhất. Người chơi phải quan sát đối thủ: Nếu thấy 2–3 xe phía trước đã lao xuống vực tranh nhau, liệu mình có nên mạo hiểm lao theo hay giữ đường an toàn để bứt phá lên Top 1? Điều này tạo ra chiều sâu chiến thuật cực lớn.

---

## 4. Kiến Trúc Web3 & Vòng Lặp Kinh Tế (Tokenomics)

### 4.1. Chuẩn Token & NFT
1. **$CAR / $DRIFT (ERC-20 Token):**
   * Token tiện ích dùng để trả phí tham gia giải đua, phí tinh luyện quặng (Smelting fee), phí sửa chữa xe và giao dịch sàn.
2. **Quặng & Linh Kiện Cơ Bản (ERC-1155):**
   * Semi-fungible tokens: Cho phép giao dịch gộp (Batch transfer) hàng trăm quặng chỉ tốn một lần gas phí.
3. **Chiếc Xe Đua Hoàn Chỉnh (ERC-721 Dynamic NFT):**
   * Lưu trữ metadata gồm: `Rarity Score`, `Engine_ID`, `Chassis_ID`, `Durability` (Độ bền), `Total_Wins` (Số trận thắng).
   * Xe có thành tích cao trong các giải đấu lớn sẽ có giá trị bán lại trên Marketplace cao hơn xe thông thường.

---

### 4.2. Vòng Lặp Kinh Tế Khép Kín (Closed-Loop Economy)

```
        ┌──────────────────────────────────────────────────────────┐
        │                                                          │
        ▼                                                          │
1. THAM GIA ĐUA XE (Track Race)                                   │
   - Đua tranh thứ hạng (PVP 10 người)                             │
   - Húc vỡ quặng địa chất bằng tốc độ cao                         │
        │                                                          │
        ▼                                                          │
2. THU THẬP TÀI NGUYÊN                                            │
   - Quặng Sắt, Vàng, Kim Cương, Lục Bảo                           │
        │                                                          │
        ▼                                                          │
3. TINH LUYỆN & CHẾ TÁC (Crafting - Token Sink)                   │
   - Đốt (Burn) Quặng + Tiêu hao $CAR Token                       │
   - Đúc linh kiện mới (ERC-1155) hoặc Nâng cấp linh kiện cũ       │
        │                                                          │
        ▼                                                          │
4. GHÉP XE HOÀN CHỈNH (Mint Car NFT - ERC-721)                     │
   - Tính toán Car Rarity Score (CRS)                              │
   - Kích hoạt Synergy bonus nếu đủ set đồng chất                  │
        │                                                          │
        ▼                                                          │
5. CƠ CHẾ HAO MÒN & SỬA CHỮA (Deflationary Sink)                  │
   - Đua xe và va chạm làm giảm Độ bền (Durability)               │
   - Phải tiêu tốn chính Quặng đào được + Token để phục hồi       │
   - Không sửa: Xe bị giảm 20% - 40% hiệu năng đua                 │
        │                                                          │
        └──────────────────────────────────────────────────────────┘
```

---

## 5. Lộ Trình Phát Triển Đề Xuất (Roadmap)

1. **Giai đoạn 1 (Gameplay Prototype):**
   * Hoàn thiện vật lý đâm va phá quặng với các ngưỡng tốc độ ($35, 65, 80, 95\text{ km/h}$).
   * Hiệu ứng văng mảnh gạch vỡ và âm thanh va chạm lực.
2. **Giai đoạn 2 (Garage & Assembly):**
   * Cho phép người chơi tháo lắp các module động cơ, khung xe làm từ các vật liệu khác nhau.
   * Tính toán và hiển thị trực tiếp điểm số Car Rarity Score (CRS).
3. **Giai đoạn 3 (Smart Contracts & Trading):**
   * Triển khai hợp đồng thông minh ERC-1155 cho nguyên liệu và ERC-721 Dynamic NFT cho xe đua.
   * Xây dựng sàn giao dịch Marketplace cho linh kiện và xe nguyên chiếc.

