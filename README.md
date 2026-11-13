# 🎬 KPID CINEMA — Hệ Thống Quản Lý & Đặt Vé Xem Phim Trực Tuyến

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0_LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/WPF-XAML-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="WPF"/>
  <img src="https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/EF%20Core-8.0-6C3483?style=for-the-badge" alt="EF Core"/>
  <img src="https://img.shields.io/badge/Microsoft_SQL_Server-2022-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License"/>
</p>

---

## 📌 1. Giới thiệu dự án

**KPID CINEMA** là ứng dụng đặt vé xem phim và quản lý rạp chiếu phim hiện đại trên nền tảng **WPF (Windows Presentation Foundation)** kết hợp **Entity Framework Core 8** và **Microsoft SQL Server**. 

Dự án được xây dựng với mục tiêu mang đến trải nghiệm điện ảnh chuẩn quốc tế:
- **Giao diện người dùng (UI/UX) chuẩn Taste-Skill:** Thiết kế Dark Cinema Theme sang trọng, tối ưu độ tương phản, font chữ điện ảnh *Bebas Neue*, các hiệu ứng đổ bóng và chuyển động mượt mà.
- **Hệ thống xử lý nghiệp vụ thông minh:** Thuật toán chống chừa ghế trống đơn lẻ (No Isolated Single Gap) theo tiêu chuẩn các rạp lớn (CGV, Galaxy), tự động gợi ý giá vé giờ vàng, thanh toán trực tuyến qua VNPAY / VietQR, và cơ chế bảo vệ giao dịch chống **Race Condition** toàn diện.

---

## 📸 2. Giao diện phần mềm (Screenshots)


### 🌟 2.1. Màn hình Trang chủ (Customer Home)
*Khám phá danh sách phim đang chiếu / sắp chiếu, lọc theo thể loại, tìm kiếm không dấu và xem thông tin chi tiết.*

<img width="1286" height="843" alt="CustomerHome" src="https://github.com/user-attachments/assets/59c1ab75-e5e4-4f8f-b3bd-c2510243e5ce" />


---

### 🎟️ 2.2. Màn hình Chọn suất chiếu & Sơ đồ ghế (Seat Selection)
*Lựa chọn ngày chiếu, suất chiếu theo từng loại phòng (Standard, IMAX, VIP), sơ đồ ghế tương tác trực quan kèm quy định chống chừa ghế lẻ và giới hạn tối đa 8 vé.*

<img width="1169" height="773" alt="SeatSelection" src="https://github.com/user-attachments/assets/049b0f36-dd38-4b9c-a5d9-31a2c79a73b7" />


---

### 💳 2.3. Màn hình Chọn bắp nước & Thanh toán (Checkout)
*Tùy chọn combo bắp nước kèm kiểm tra tồn kho tức thì, hỗ trợ thanh toán linh hoạt qua Tiền mặt, quét mã VietQR tự động hoặc cổng thanh toán trực tuyến VNPAY Sandbox.*
<img width="814" height="764" alt="Checkout" src="https://github.com/user-attachments/assets/5c129e0b-15a4-4bfc-bd8f-56a56f87703f" />


---

### 📱 2.4. Vé xem phim điện tử & Mã QR Code (Electronic Ticket)
*Vé điện tử tạo tự động kèm mã QR Code độ phân giải cao phục vụ soát vé tại rạp, tích hợp tính năng In vé vật lý.*

<img width="1318" height="815" alt="ElectronicTicket" src="https://github.com/user-attachments/assets/555054c8-9356-48a3-8f34-8f5c74004b15" />


---

### 🛡️ 2.5. Phân hệ Quản trị (Admin) & Soát vé (Staff)
*Bảng điều khiển quản lý phim, phòng chiếu, xung đột lịch chiếu, thực đơn bắp nước, báo cáo doanh thu và màn hình kiểm soát vé thời gian thực.*

<img width="1186" height="792" alt="Admin" src="https://github.com/user-attachments/assets/bbf59f6b-6296-465f-84e1-3d448d500c91" />


---

## 🚀 3. Các tính năng nổi bật

### 🍿 Phân hệ Khách hàng (Customer & Guest)
- **Trải nghiệm không cần tài khoản:** Khách vãng lai có thể đặt vé nhanh chóng chỉ với Tên và Số điện thoại; hệ thống tự động lưu trữ và hỗ trợ tra cứu lại vé bất kỳ lúc nào.
- **Tìm kiếm thông minh:** Tìm kiếm tên phim, đạo diễn, mô tả hỗ trợ cả **tiếng Việt có dấu và không dấu** (Unaccented Search: gõ *"lat"* vẫn tìm ra *"Lật Mặt"*).
- **Phân loại phim chuẩn xác:** Lọc theo phim *Đang chiếu* hoặc *Sắp chiếu* dựa trên ngày khởi chiếu thực tế và lịch phát hành.
- **Sơ đồ ghế thông minh (Interactive Matrix):**
  - Phân chia rõ ràng Ghế Thường (A–B) và Ghế VIP (C–E) kèm phụ thu tự động.
  - **Thuật toán Business Rule:** Tuyệt đối không cho phép khách chọn ghế để lại đúng 1 ghế trống đơn lẻ trong hàng.
  - **Giới hạn số lượng vé:** Mỗi lần đặt vé được chọn tối đa **8 vé** nhằm chống đầu cơ vé.
- **Thanh toán đa kênh:**
  - Tiền mặt tại quầy.
  - Chuyển khoản **VietQR** (tự sinh mã QR ngân hàng kèm số tiền và nội dung chuyển khoản động).
  - Cổng thanh toán quốc gia **VNPAY Sandbox** (xác thực chữ ký số SHA512 và cập nhật kết quả tự động).

### 🎫 Phân hệ Nhân viên (Staff)
- **Soát vé điện tử siêu tốc:** Quét mã QR từ camera / máy quét hoặc nhập trực tiếp mã vé.
- Kiểm tra tính hợp lệ của vé, thông tin suất chiếu, phòng chiếu, ghế ngồi.
- Hiển thị cảnh báo tức thì nếu vé đã được sử dụng trước đó.

### ⚙️ Phân hệ Quản trị viên (Admin)
- **Quản lý danh mục phim:** Thêm, sửa, chuyển đổi trạng thái phim, phân loại nhãn tuổi (P, K, T13, T16, T18), tải poster.
- **Quản lý suất chiếu & Phòng chiếu:**
  - **Thuật toán kiểm tra xung đột (Anti-Collision Scheduling):** Tự động phát hiện và ngăn chặn việc xếp lịch trùng phòng chiếu hoặc trùng khung giờ.
  - **Quy định trần giá vé:** Giới hạn giá vé gốc của phim $\le 200.000$ VNĐ.
  - Tự động gợi ý tăng 20% giá vé trong khung giờ vàng (18h – 22h) và cuối tuần.
- **Quản lý bắp nước (F&B):** Quản lý thực đơn, đơn giá, kiểm soát số lượng tồn kho theo thời gian thực.
- **Báo cáo doanh thu trực quan:** Thống kê doanh thu theo mốc thời gian, tỷ trọng vé xem phim và đồ ăn uống.

---

## 🔒 4. Kiến trúc giải quyết Race Condition (Đồng thời)

Để đảm bảo hệ thống hoạt động ổn định khi nhiều máy trạm/khách hàng thao tác cùng một thời điểm:
1. **Chống đặt trùng ghế (Double-Booking):**
   - Đặt chỉ mục duy nhất cấp CSDL: `UNIQUE NONCLUSTERED INDEX UQ_Ticket_Showtime_Seat ON Ticket(ShowtimeId, SeatCode)`.
   - Kết hợp `Database Transaction (Serializable/RepeatableRead)` kiểm tra lại trạng thái ghế ngay trước khi lưu đơn.
2. **Chống bán quá tồn kho bắp nước (Lost Update):**
   - Kiểm tra số lượng tồn kho nguyên tử ngay trong phiên giao dịch trước khi trừ kho.
3. **Chống gian lận soát vé cùng lúc:**
   - Thực thi câu lệnh cập nhật nguyên tử:
     ```sql
     UPDATE Booking SET IsCheckedIn = 1 WHERE BookingId = @id AND IsCheckedIn = 0;
     ```
     Ngăn chặn 100% việc 2 nhân viên ở 2 cổng soát vé chấp nhận cùng 1 mã vé tại cùng 1 tích tắc.

---

## 🛠️ 5. Công nghệ & Thư viện sử dụng

| Công nghệ / Thư viện | Phiên bản | Vai trò trong dự án |
| :--- | :---: | :--- |
| **C# / .NET** | `12` / `8.0 (LTS)` | Ngôn ngữ và nền tảng phát triển chính |
| **WPF (Windows Presentation Foundation)** | `8.0` | Giao diện đồ họa máy tính với XAML |
| **Microsoft.EntityFrameworkCore** | `8.0.8` | ORM kết nối và thao tác Cơ sở dữ liệu |
| **Microsoft.EntityFrameworkCore.SqlServer** | `8.0.8` | Driver cơ sở dữ liệu Microsoft SQL Server |
| **Microsoft.EntityFrameworkCore.Design** | `8.0.8` | Công cụ thiết kế và migration EF Core |
| **Microsoft.Extensions.Configuration.Json** | `8.0.0` | Đọc cấu hình từ file `appsettings.json` |
| **QRCoder** | `1.8.1` | Thư viện tạo mã QR Code vé điện tử offline tốc độ cao |
| **Microsoft SQL Server** | `2022 / Express` | Hệ quản trị cơ sở dữ liệu quan hệ |

---

## ⚙️ 6. Hướng dẫn cài đặt & Chạy dự án

### Yêu cầu hệ thống:
- Hệ điều hành: Windows 10 / Windows 11 (64-bit)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) trở lên
- Microsoft SQL Server 2019/2022 hoặc SQL Server Express
- Visual Studio 2022 (với workload *.NET Desktop Development*) hoặc Visual Studio Code / JetBrains Rider

### Các bước cài đặt:

1. **Clone repository về máy:**
   ```bash
   git clone https://github.com/<your-username>/CinemaTicketBookingSystem.git
   cd CinemaTicketBookingSystem
   ```

2. **Cấu hình Cơ sở dữ liệu:**
   - Mở SQL Server Management Studio (SSMS).
   - Mở và thực thi file script tạo database `CinemaManagementDB.sql` (hoặc tạo database `CinemaManagementDB`).
   - Mở file `CinemaTicketBookingSystem/appsettings.json` và cập nhật chuỗi kết nối:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "server=.\\SQLEXPRESS;uid=sa;pwd=12345;database=CinemaManagementDB;TrustServerCertificate=True;"
       }
     }
     ```

3. **Khôi phục thư viện và Build dự án:**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Khởi chạy ứng dụng:**
   ```bash
   dotnet run --project CinemaTicketBookingSystem/CinemaTicketBookingSystem.csproj
   ```

---

## 🔑 7. Tài khoản dùng thử mặc định (Demo Credentials)

| Vai trò | Tên đăng nhập | Mật khẩu | Quyền hạn |
| :--- | :---: | :---: | :--- |
| **Quản trị viên (Admin)** | `admin` | `123` | Toàn quyền quản trị phim, suất chiếu, bắp nước, báo cáo |
| **Nhân viên (Staff)** | `staff1` | `123` | Quản lý và soát vé điện tử |
| **Khách hàng (Customer)** | *Đăng ký mới* | *Tự đặt* | Đặt vé, chọn ghế, lịch sử vé cá nhân |
| **Khách vãng lai (Guest)** | *Không cần đăng nhập* | — | Bấm *"Tiếp tục với tư cách Khách"* tại màn hình đăng nhập |

---

## 👨‍💻 8. Tác giả & Đóng góp

- Dự án phục vụ môn học **PRN212 — Lập trình ứng dụng trên Windows (C# / WPF)**.
- **Nhóm phát triển KPID CINEMA Team:**
  - Thành viên thực hiện dự án theo bảng phân công công việc.

---

<p align="center">
  <sub>Built with ❤️ by KPID CINEMA Team • 2026</sub>
</p>

