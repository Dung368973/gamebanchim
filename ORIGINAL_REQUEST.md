# Original User Request

## 2026-09-12T03:19:23Z

Xây dựng hoàn chỉnh một tựa game bắn chim 2D trên mobile phong cách Shoot'em up (máy bay cổ điển) trong Unity: người chơi bay và tự động bắn liên tục, né tránh và tiêu diệt các đợt chim bay, thu thập vật phẩm nâng cấp, tính điểm và hoàn thiện đầy đủ asset đồ họa, hiệu ứng, UI.

Working directory: c:\Users\Quy set\Documents\skibidi\game-ban-chim\My project
Integrity mode: development

## Requirements

### R1. Player Controller & Auto-Shooting System (Cơ chế người chơi & Tự động bắn)
Điều khiển đối tượng người chơi mượt mà tối ưu cho Mobile (hỗ trợ kéo/chạm Touch drag hoặc chuột/phím ảo), giới hạn trong khung nhìn màn hình. Cơ chế tự động bắn đạn liên tục với tốc độ bắn cấu hình được, hệ thống Bullet Pooling để tối ưu hiệu năng mobile, âm thanh và hiệu ứng bắn đạn.

### R2. Bird Enemy Types & Wave Spawner (Kẻ địch chim & Các đợt xuất hiện)
Hệ thống sinh chim (Spawner) theo từng đợt (Waves) với nhiều hành vi khác nhau:
- Chim cơ bản: bay thẳng theo đàn.
- Chim nhanh / Chim lượn: bay lượn ziczac hoặc lao nhanh hướng về người chơi.
- Chim trâu / Chim đầu đàn: có thanh máu (HP), kích thước lớn hơn và rơi vật phẩm khi bị tiêu diệt.
Tất cả các loại chim có hiệu ứng lông vũ / nổ hạt khi trúng đạn và bị tiêu diệt, tự hủy khi bay vượt quá màn hình.

### R3. Hệ thống Nâng cấp, Máu & Điểm số (Progression, Power-ups & Scoring)
- Vật phẩm rơi ra từ chim (Power-ups): Bắn đạn chùm (Spread shot), tăng tốc độ bắn (Rapid fire), hồi máu/giáp.
- Hệ thống máu người chơi (Lives/Health), điểm số (Score), Combo số lượng chim bắn trúng liên tiếp.
- Vòng lặp trò chơi hoàn chỉnh: Bắt đầu, Tăng dần độ khó theo wave/thời gian, Game Over khi hết máu, và Nút Chơi lại (Restart).

### R4. Asset Đồ họa, Môi trường & Giao diện Mobile (Assets, Parallax & UI)
- Tích hợp/tạo asset 2D chất lượng: Sprite người chơi, các loại chim, đạn, vật phẩm, hiệu ứng hạt (Hit FX, Feather burst).
- Nền cuộn vô tận (Endless scrolling background) tạo cảm giác bay liên tục.
- Giao diện UI Canvas thích ứng màn hình điện thoại (Responsive): Thanh máu, Điểm số, Wave hiện tại, Bảng Game Over với điểm kỷ lục (High Score) và nút Chơi lại.
- Hệ thống âm thanh (SFX) cho tiếng súng, tiếng chim chết, âm thanh nhặt đồ và nhạc nền.

## Acceptance Criteria

### Script Compilation & Runtime Integrity
- [ ] Toàn bộ mã nguồn C# biên dịch thành công 100%, không có lỗi biên dịch (Compilation Errors) trong Unity Editor.
- [ ] Chạy game không phát sinh NullReferenceException hay lỗi console nghiêm trọng.

### Core Gameplay Mechanics
- [ ] Đối tượng người chơi di chuyển mượt mà theo thao tác chạm/chuột và tự động bắn đạn liên tục.
- [ ] Các đợt chim spawn đều đặn từ cạnh trên màn hình, di chuyển xuống với các quỹ đạo khác nhau và tương tác va chạm chuẩn xác với đạn và người chơi.
- [ ] Bắn trúng chim trừ máu/tiêu diệt chim, cộng điểm số và hiển thị hiệu ứng hạt.
- [ ] Nhặt power-up kích hoạt đúng trạng thái nâng cấp đạn/máu.
- [ ] Khi hết máu, hiển thị popup Game Over với điểm số và có thể bấm Restart để chơi lại ngay lập tức.

### Visuals & Scene Setup
- [ ] Scene chính được thiết lập hoàn chỉnh với Camera 2D, Background cuộn mượt mà, Spawner, Canvas UI hiển thị rõ ràng trên tỷ lệ màn hình mobile.

### Git Version Control
- [ ] Mỗi đoạn/tính năng hoàn thành cần thực hiện git commit nhỏ giọt (incremental atomic commits) với thông điệp rõ ràng.

