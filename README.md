# 🦅 Game Bắn Chim 2D (Shoot 'Em Up Arcade)

[![Unity](https://img.shields.io/badge/Unity-6000.5.x-blue.svg?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%2017.5-orange.svg)](https://unity.com/srp/universal-render-pipeline)
[![Input System](https://img.shields.io/badge/Input%20System-New%20Input%20System-green.svg)](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)
[![Platform](https://img.shields.io/badge/Platform-Mobile%20%7C%20PC-lightgrey.svg)](#)

Một tựa game 2D bắn chim phong cách cổ điển (**Shoot 'Em Up / Vertical Arcade Shooter**), được phát triển trên nền tảng **Unity 6** với **Universal Render Pipeline (URP)**. Trò chơi được tối ưu hóa giao diện cho tỉ lệ màn hình điện thoại (Portrait 9:16 - 1080x1920), đồng thời hỗ trợ trải nghiệm chơi mượt mà trên máy tính PC/Desktop.

---

## 🎮 Giới thiệu Trò chơi

Trong vai người điều khiển phi thuyền bảo vệ không phận, bạn sẽ đối mặt với các đợt chim xâm lược với đủ mọi chủng loại và chiến thuật khác nhau. Trò chơi kết hợp cơ chế né tránh đạn, bắn hạ kẻ địch, thu thập các vật phẩm nâng cấp (Power-ups), và tích lũy combo điểm số cao nhất có thể.

---

## ✨ Tính năng Nổi bật

### 1. Cơ chế Người chơi & Tự động Bắn (Player & Auto-Shooting)
- **Điều khiển cảm ứng tối ưu cho Mobile**: Hỗ trợ kéo vuốt mượt mà (Touch Drag) với khoảng bù công thái học (Ergonomic Touch Offset), giúp ngón tay không che khuất phi thuyền.
- **Hỗ trợ PC/Desktop**: Điều khiển trực quan bằng rê chuột hoặc sử dụng bàn phím (`W`, `A`, `S`, `D` hoặc các phím mũi tên).
- **Hệ thống Tự động Bắn (Continuous Auto-Fire)**: Tốc độ xả đạn nhanh, liên tục và tự động căn thời gian.
- **Tối ưu bộ nhớ với Bullet Pooling**: Quản lý đạn với kiến trúc `ObjectPool<T>`, hoàn toàn không cấp phát rác (Zero GC Allocations) trong suốt trận đấu.
- **Giới hạn khung nhìn an toàn**: Phi thuyền luôn được neo trong khung nhìn camera, không bao giờ bị bay lạc ra ngoài màn hình.

### 2. Hệ thống Kẻ địch Đa dạng (4 Chủng loại Chim)
- **Chim Cơ Bản (Basic Sparrow)**: Bay theo đội hình đàn thẳng đứng từ trên xuống với vận tốc đồng đều.
- **Chim Nhanh (Fast Falcon)**: Quỹ đạo bay lượn sóng ziczac điều hòa $x(t) = x_0 + A\sin(\omega t + \phi)$, bất ngờ tăng tốc lao nhanh về phía phi thuyền.
- **Chim Trâu (Tank Eagle)**: Giáp dày, kích thước lớn, có thanh máu (World-Space HP Bar) hiển thị động trên đầu; khi bị tiêu diệt chắc chắn rơi ra vật phẩm nâng cấp.
- **Trùm Phượng Hoàng (Boss Phoenix)**: Xuất hiện ở các đợt sóng đặc biệt với thanh máu Boss hiển thị trên đỉnh màn hình, bắn đạn chùm và cơ chế chiến đấu hoành tráng.
- **Độ khó Tăng tiến (Wave Spawner)**: Giảm dần thời gian chờ giữa các đợt spawn và tăng tốc độ bay của chim theo từng đợt Wave.

### 3. Vật phẩm Nâng cấp (Power-Ups)
- 🔱 **Spread Shot (Bắn Tỏa)**: Nâng cấp hỏa lực bắn tỏa 3 hoặc 5 luồng đạn với góc bắn mở rộng.
- ⚡ **Rapid Fire (Tăng Tốc Bắn)**: Nhân đôi tốc độ xả đạn trong một khoảng thời gian nhất định.
- ❤️ **Health Recovery (Hồi Máu)**: Hồi phục sinh lực cho phi thuyền.
- 🛡️ **Energy Shield (Khiên Năng Lượng)**: Tạo lớp giáp từ trường hấp thụ 1 lần sát thương bất kỳ, kèm thanh đo năng lượng khiên.
- **Hiệu ứng Nam châm**: Vật phẩm trôi bồng bềnh và tự động hút về phía người chơi khi ở cự ly gần ($r < 1.6\text{ m}$).

### 4. Giao diện HUD & Điểm số Thời gian thực (Real-time HUD)
- **Hiển thị song song**:
  - **Điểm hiện tại (`SCORE`)**: Chữ trắng đậm, cập nhật theo từng phát bắn trúng và hệ số Combo.
  - **Điểm kỷ lục (`BEST`)**: Chữ vàng kim (#FFD700) nổi bật, đọc từ `PlayerPrefs` và tự động nhảy tăng trực tiếp khi người chơi phá vỡ kỷ lục trong trận đấu.
- **Hệ số Combo**: Thưởng điểm theo chuỗi tiêu diệt liên tiếp từ $1.0\times$ lên đến $3.0\times$, có thanh đếm ngược thời gian duy trì combo.
- **Màn hình Game Over & Pause**: Popup Game Over tổng kết điểm, tự động gắn cờ **★ NEW RECORD! ★** khi đạt kỷ lục mới và nút **RESTART** chơi lại tức thì.

### 5. Đồ họa & Hiệu ứng (Visuals & Particles)
- **Hiệu ứng Hạt (Particle FX)**: Hiệu ứng vỡ lông vũ (Feather Burst) khi chim bị tiêu diệt và tia lửa va chạm (Hit Sparks) khi trúng đạn.
- **Nền Parallax Cuộn Vô Tận**: 3 lớp nền (Sao xa, Mây trôi, Dãy núi) cuộn với tốc độ khác nhau tạo chiều sâu không gian 2.5D.
- **Thích ứng SafeArea (Tai thỏ / Đảo động)**: Tự động căn chỉnh Canvas tránh các vùng khuyết đỉnh và mép viền trên màn hình điện thoại.

### 6. Âm thanh Tự tổng hợp (Procedural Audio Engine)
- Không cần các file âm thanh nặng cồng kềnh, toàn bộ 8 hiệu ứng âm thanh (SFX) và nhạc nền 16-bar Synthwave BGM đều được sinh trực tiếp bằng thuật toán tổng hợp sóng âm thanh (Procedural Synthesis) trong mã nguồn C#.

---

## 📁 Cấu trúc Thư mục Dự án

Dự án Unity được đặt trực tiếp tại thư mục gốc của repository:

```text
gamebanchim/
├── Assets/                                # Tài nguyên và mã nguồn chính của Unity
│   ├── Editor/                            # Các công cụ hỗ trợ Editor và tạo Scene tự động
│   │   ├── ProceduralSpriteGenerator.cs   # Trình tạo ảnh 2D Procedural
│   │   └── SceneSetupHelper.cs            # Tự động lắp ráp toàn bộ SampleScene
│   ├── Prefabs/                           # Prefabs: Bullet, PowerUp, Chim các loại
│   ├── Scenes/                            # Scene chính của game: SampleScene.unity
│   ├── Scripts/                           # Toàn bộ mã nguồn gameplay C#
│   │   ├── Combat/                        # Xử lý bắn, đạn, va chạm, sát thương
│   │   ├── Core/                          # GameManager, ScoreManager, AudioManager, EventBus
│   │   ├── Enemies/                       # Logic di chuyển, thanh máu và Spawner của chim
│   │   ├── Environment/                   # Nền Parallax, FX Manager, Boundary Cleaner
│   │   ├── Player/                        # Điều khiển tàu bay, máu, bắn đạn
│   │   ├── Tests/                         # Bộ kiểm thử E2E tích hợp
│   │   └── UI/                            # Quản lý HUD, Game Over, Pause, SafeArea
│   ├── Settings/                          # Cấu hình URP (Universal Render Pipeline)
│   └── Sprites/                           # Bộ Sprite 2D tự tạo (tàu, đạn, chim, nền)
├── Packages/                              # Khai báo các Unity Package (URP, Input System, ...)
├── ProjectSettings/                       # Cài đặt dự án (Physics2D, Tags, Layers, Graphics, ...)
├── Tests/                                 # Bộ kiểm thử độc lập (Adversarial Tests)
├── .gitignore                             # Cấu hình bỏ qua các thư mục tạm (Library, Temp, Logs...)
├── PROJECT.md                             # Tài liệu kiến trúc kỹ thuật chi tiết
└── README.md                              # Tài liệu hướng dẫn trò chơi (file này)
```

---

## 🚀 Hướng dẫn Cài đặt & Khởi chạy

### Yêu cầu Hệ thống
- **Unity Editor**: Phiên bản **Unity 6 (6000.5.x)** trở lên.
- **Render Pipeline**: **Universal Render Pipeline (URP)** (đã được cấu hình sẵn trong project).

### Các bước mở dự án:
1. **Clone repository về máy tính**:
   ```bash
   git clone https://github.com/Dung368973/gamebanchim.git
   ```
2. **Mở qua Unity Hub**:
   - Mở **Unity Hub**.
   - Nhấn **Add** (Thêm dự án từ đĩa) ➔ Chọn trực tiếp thư mục `gamebanchim`.
   - Chọn phiên bản Unity Editor (Unity 6 / 6000.5.x).
3. **Mở Scene và Chơi**:
   - Trong cửa sổ `Project`, điều hướng đến: `Assets/Scenes/SampleScene.unity` và nhấn đúp để mở.
   - Nhấn nút **Play** (▶) ở thanh công cụ phía trên để trải nghiệm game.

> [!TIP]
> **Tạo/Lắp ráp lại Scene tự động:**
> Nếu bạn muốn tạo lại một Scene hoàn chỉnh nguyên bản từ đầu, chỉ cần vào thanh Menu của Unity Editor:
> Chọn **Tools ➔ Build Main Scene**. Hệ thống sẽ tự động tổng hợp sprite, prefab, thiết lập camera, UI và lưu lại Scene chuẩn chỉ trong vài giây.

---

## ⌨️ Phím & Thao tác Điều khiển

| Thiết bị | Thao tác | Chức năng |
| :--- | :--- | :--- |
| **Mobile** | Vuốt / Chạm kéo ngón tay | Di chuyển phi thuyền theo ngón tay |
| **PC / Laptop** | Rê chuột trái (Drag) | Di chuyển phi thuyền theo chuột |
| **PC / Laptop** | Phím `W`, `A`, `S`, `D` hoặc `Mũi tên` | Di chuyển phi thuyền 8 hướng |
| **Tất cả** | Tự động (Auto) | Phi thuyền tự động bắn đạn liên tục |
| **Tất cả** | Nút `❚❚` hoặc phím `P` | Tạm dừng (Pause) / Tiếp tục (Resume) |

---

## 📜 Bản quyền & Đóng góp

Dự án được phát triển với tinh thần mã nguồn mở và chia sẻ kiến thức làm game trên Unity. Mọi đóng góp (Pull Request, Issue) đều được chào đón!

* **Tác giả**: [Dung368973](https://github.com/Dung368973)
* **Repository**: [https://github.com/Dung368973/gamebanchim](https://github.com/Dung368973/gamebanchim)
