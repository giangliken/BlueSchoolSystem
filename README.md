# BlueSchool System - Hệ thống Quản lý Đào tạo Đa nền tảng (Web & Mobile App)

![BlueSchool Banner](https://via.placeholder.com/1000x300?text=BlueSchool+System+Banner) > **Đồ án Chuyên ngành Công nghệ Thông tin - HUTECH** > **Nhóm thực hiện:** TOWKTEAM

![.NET](https://img.shields.io/badge/.NET%20Core-9.0-purple)
![Flutter](https://img.shields.io/badge/Flutter-Dart-blue)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)
![Firebase](https://img.shields.io/badge/Firebase-Realtime-orange)

## 📖 Giới thiệu (Overview)

[cite_start]**BlueSchool System** là một hệ sinh thái phần mềm toàn diện nhằm giải quyết bài toán quản lý đào tạo tín chỉ tại các trường đại học[cite: 84, 87]. [cite_start]Hệ thống giúp thay thế các quy trình thủ công, giảm thiểu sai sót dữ liệu và ngăn chặn gian lận trong điểm danh nhờ tích hợp các công nghệ hiện đại như AI và GPS[cite: 83, 85].

[cite_start]Hệ thống bao gồm 2 thành phần chính[cite: 21, 115]:
1.  **Website quản trị (BlueSchool):** Dành cho Admin và Cán bộ đào tạo phát triển trên nền tảng ASP.NET Core MVC.
2.  **Ứng dụng di động (BlueNet):** Dành cho Giảng viên và Sinh viên phát triển bằng Flutter.

## 🚀 Tính năng nổi bật (Key Features)

### 📱 Mobile App (BlueNet) - Dành cho Giảng viên & Sinh viên
* [cite_start]**Điểm danh thông minh:** Hỗ trợ 3 phương thức: Quét mã QR, Bluetooth (BLE) và Nhận diện khuôn mặt (AI Face Recognition) kết hợp định vị GPS (bán kính 200m)[cite: 24, 100, 150].
* [cite_start]**Thời khóa biểu & Lịch thi:** Xem lịch học, lịch thi chi tiết theo tuần/tháng[cite: 259, 260].
* [cite_start]**Kết quả học tập:** Xem bảng điểm chi tiết, điểm trung bình và tiến độ tích lũy[cite: 260].
* [cite_start]**Tương tác Real-time:** Chat nhóm lớp học phần, nhận thông báo tức thì từ giảng viên/nhà trường[cite: 107, 255].
* [cite_start]**Đăng ký học phần:** Sinh viên thực hiện đăng ký môn học trực tiếp trên ứng dụng[cite: 264].

### 💻 Web Admin (BlueSchool) - Dành cho Quản trị viên
* [cite_start]**Quản lý đào tạo:** Quản lý Khoa, Ngành, Lớp, Môn học, Học kỳ[cite: 197, 201].
* [cite_start]**Quản lý nhân sự & Người học:** CRUD Sinh viên, Giảng viên, phân công giảng dạy[cite: 161, 172].
* [cite_start]**Xếp lịch tự động:** Hỗ trợ mở lớp và xếp lịch học tự động[cite: 203, 205].
* [cite_start]**Thống kê & Báo cáo:** Xuất danh sách sinh viên, bảng điểm, theo dõi nhật ký hệ thống[cite: 247, 741].

## 🛠️ Công nghệ sử dụng (Tech Stack)

[cite_start]Dựa trên chương 4 của báo cáo, dự án sử dụng các công nghệ sau [cite: 1355-1390]:

| Thành phần | Công nghệ | Chi tiết |
| :--- | :--- | :--- |
| **Backend** | ASP.NET Core Web API | .NET 9.0, Entity Framework Core |
| **Frontend Web** | ASP.NET Core MVC | Bootstrap 5, jQuery/AJAX |
| **Mobile App** | Flutter | Ngôn ngữ Dart, hỗ trợ Android & iOS |
| **Database** | SQL Server 2022 | Lưu trữ dữ liệu nghiệp vụ (RDBMS) |
| **Realtime** | Firebase | Realtime Database & Cloud Messaging (FCM) |
| **AI & Location** | FaceNet 512 & Geolocator | Nhận diện khuôn mặt & Định vị GPS |
| **Deploy/Dev** | Cloudflare Tunnel | Hỗ trợ HTTPS và Public Domain cho Localhost |

## 📸 Hình ảnh demo (Screenshots)

| Trang chủ Mobile | Điểm danh Bluetooth | Thời khóa biểu |
| :---: | :---: | :---: |
| <img src="path/to/home_mobile.png" width="200"> | <img src="path/to/bluetooth_checkin.png" width="200"> | <img src="path/to/schedule.png" width="200"> |

| Web Admin - Dashboard | Quản lý Sinh viên |
| :---: | :---: |
| <img src="path/to/web_dashboard.png" width="400"> | <img src="path/to/student_manage.png" width="400"> |

## ⚙️ Cài đặt và Triển khai (Installation)

### [cite_start]Yêu cầu môi trường [cite: 1355-1366]
* **IDE:** Visual Studio 2022 (Backend/Web), Android Studio hoặc VS Code (Mobile).
* **SDK:** .NET 9.0 SDK, Flutter SDK.
* **Database:** SQL Server 2022 trở lên.

### Các bước cài đặt

#### 1. Backend & Web (ASP.NET Core)
1.  Clone repository:
    ```bash
    git clone [https://github.com/username/BlueSchoolSystem.git](https://github.com/username/BlueSchoolSystem.git)
    ```
2.  [cite_start]Mở solution `BlueSchoolSystem.sln` bằng Visual Studio[cite: 1420].
3.  Cấu hình chuỗi kết nối (Connection String) trong `appsettings.json` trỏ về SQL Server của bạn.
4.  Chạy lệnh Update Database (Entity Framework) trong Package Manager Console:
    ```powershell
    Update-Database
    ```
5.  Khởi chạy dự án (IIS Express hoặc Kestrel).

#### 2. Mobile App (Flutter)
1.  Di chuyển vào thư mục Mobile:
    ```bash
    cd BlueNet-V2
    ```
2.  Cài đặt các thư viện phụ thuộc:
    ```bash
    flutter pub get
    ```
3.  Cấu hình `config.dart`: Cập nhật `BASE_URL` trỏ về địa chỉ API (Nếu chạy localhost, sử dụng Cloudflare Tunnel hoặc IP LAN)[cite: 1396].
4.  Chạy ứng dụng trên máy ảo hoặc thiết bị thật:
    ```bash
    flutter run
    ```

## 👥 Nhóm tác giả (Authors)

[cite_start]Đồ án được thực hiện bởi nhóm sinh viên lớp **22DTHG1** - Khoa Công nghệ Thông tin HUTECH [cite: 9-16, 45-53]:

| STT | Họ và tên | MSSV | Vai trò |
| :---: | :--- | :--- | :--- |
| 1 | **Nguyễn Trường Giang** | 2280600761 | Backend, Mobile App, AI Integration |
| 2 | **Võ Tấn Tài** | 2280602832 | Frontend Web, Database |
| 3 | **Nguyễn Trần Thiên Ý** | 2280603808 | Mobile UI, Tester |

**Giảng viên hướng dẫn:** ThS. [cite_start]Trần Thị Vân Anh[cite: 8].

---
*Dự án phục vụ mục đích học tập và bảo vệ đồ án chuyên ngành năm 2025.*
