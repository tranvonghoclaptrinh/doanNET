# 🏫 Hệ Thống Quản Lý Trung Tâm Ngoại Ngữ (QL_TrungTamNgoaiNgu)

## 📋 Thông Tin Dự Án

**Tên dự án:** Hệ Thống Quản Lý Trung Tâm Ngoại Ngữ  
**Phiên bản:** 5.2  
**Mô tả:** Ứng dụng quản lý toàn diện cho trung tâm đào tạo ngoại ngữ, bao gồm quản lý học viên, khóa học, lịch dạy, thanh toán học phí, grades, và báo cáo.

---

## 👤 Người Thực Hiện

- **Họ Tên:** Trần Hữu Vong
- **Email:** tranhuuvong23092006@gmail.com
- **GitHub:** [tranvonghoclaptrinh](https://github.com/tranvonghoclaptrinh)
- **GitHub Page:** [tranvonghoclaptrinh.github.io](https://tranvonghoclaptrinh.github.io)

---

## 🛠️ Công Nghệ Sử Dụng

| Thành Phần | Công Nghệ |
|:-----------|:----------|
| **Framework** | .NET Framework 4.7.2 / WPF (Windows Presentation Foundation) |
| **Database** | SQL Server 2019+ |
| **ORM** | Entity Framework 6.4.4 |
| **Ngôn ngữ** | C# |
| **IDE** | Visual Studio 2022 |
| **Version Control** | Git / GitHub |

---

## 📁 Cấu Trúc Thư Mục

```text
QL_TrungTamNgoaiNgu/
├── QL_TrungTamNgoaiNgu.slnx          # Solution file
├── run_DB.sql                         # Script khởi tạo database
├── LichDay.sql                        # SQL bổ sung
├── select.sql                         # Query mẫu
├── packages/                          # NuGet packages
│   └── EntityFramework.6.4.4/
│
└── QL_TrungTamNgoaiNgu/              # Main application folder
    ├── QL_TrungTamNgoaiNgu.csproj    # Project file
    ├── App.xaml / App.xaml.cs         # Application entry point
    ├── MainWindow.xaml                # Main UI
    ├── LoginWindow.xaml               # Login screen
    ├── PaymentWindow.xaml             # Payment form
    ├── EntityFormWindow.xaml          # Generic entity form
    │
    ├── Models/                        # Entity Framework models
    │   ├── Model1.*.                  # Auto-generated EF models
    │   ├── NguoiDung.cs               # User model
    │   ├── VaiTro.cs                  # Role model
    │   ├── KhoaHoc.cs                 # Course model
    │   ├── DangKyKhoaHoc.cs           # Course registration
    │   ├── HoaDonHocPhi.cs            # Invoice model
    │   ├── GiaoDichThanhToan.cs       # Payment transaction
    │   ├── LichDay.cs                 # Teaching schedule
    │   ├── DiemSo.cs                  # Grades
    │   ├── LichSuHeThong.cs           # System audit log
    │   ├── ViewRows.cs                # DTO for report queries
    │   └── Other models.cs
    │
    ├── Services/                      # Business logic
    │   ├── AuthService.cs             # Authentication
    │   ├── PaymentService.cs          # Payment processing ⭐ FIXED
    │   ├── IKhoaHocRegistrationService.cs
    │   ├── KhoaHocRegistrationService.cs
    │   ├── ICsvExportService.cs
    │   ├── CsvExportService.cs        # CSV export
    │   └── UserSession.cs             # Session management
    │
    ├── ViewModels/                    # MVVM ViewModels
    │   ├── MainViewModel.cs           # Main window logic
    │   ├── BaseViewModel.cs           # Base class
    │   ├── DangKyKhoaHocViewModel.cs  # Registration form logic
    │   ├── RelayCommand.cs            # Command implementation
    │   ├── AsyncRelayCommand.cs       # Async command
    │   ├── FormField.cs               # Dynamic form field
    │   ├── ChartItem.cs               # Chart data
    │   ├── TableMenuItem.cs           # Menu item
    │   └── Other ViewModels.cs
    │
    ├── Views/                         # Additional UI components
    │
    ├── Properties/                    # Project properties
    │   ├── AssemblyInfo.cs
    │   └── Settings.settings
    │
    ├── obj/                           # Build output (temporary)
    └── bin/                           # Compiled binaries
        ├── Debug/
        └── Release/
```

---

## 🚀 Hướng Dẫn Chạy Ứng Dụng

### 📋 Yêu Cầu Tiên Quyết

1. **SQL Server 2019 hoặc mới hơn** được cài đặt
2. **Visual Studio 2022** hoặc mới hơn
3. **.NET Framework 4.7.2** hoặc mới hơn

### 📦 Bước 1: Clone Repository

```bash
git clone https://github.com/tranvonghoclaptrinh/doanNET.git
cd doanNET
cd QL_TrungTamNgoaiNgu
```

### 🗄️ Bước 2: Tạo Database

#### **Cách 2A: Sử dụng SQL Server Management Studio (SSMS)**

1. Mở **SQL Server Management Studio**
2. Kết nối tới server của bạn
3. Mở file: `run_DB.sql` (nằm trong thư mục gốc)
4. Chọn **Execute** (Ctrl+E)
5. Đợi script chạy xong (~30 giây)

#### **Cách 2B: Sử dụng Command Line**

```bash	sqlcmd -S your_server_name -U sa -P your_password -i "run_DB.sql"
```

**Ví dụ:**
```bash	sqlcmd -S LAPTOP-ABC\SQLEXPRESS -U sa -P 123456 -i "run_DB.sql"
```

### 🔧 Bước 3: Sửa Server Name trong Connection String

Mở file: `App.config` trong folder `QL_TrungTamNgoaiNgu/`

Tìm section `<connectionStrings>`:

```xml
<connectionStrings>
  <add name="HeThongQuanLyTrungTamNgoaiNguEntities" 
       connectionString="metadata=res://*/Models/Model1.csdl|res://*/Models/Model1.ssdl|res://*/Models/Model1.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=LAPTOP-ABC\\SQLEXPRESS;initial catalog=HeThongQuanLyTrungTamNgoaiNgu;integrated security=True;multipleactiveresultsets=True;application name=EntityFrameworkMutableProxy&quot;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

**Các thành phần cần sửa:**

| Thành Phần | Ý Nghĩa | Ví Dụ |
|:-----------|:---------|:-------|
| `data source` | Tên server SQL | `LAPTOP-ABC\SQLEXPRESS` hoặc `localhost` |
| `initial catalog` | Tên database | `HeThongQuanLyTrungTamNgoaiNgu` |
| `User ID` | (nếu dùng SQL Auth) | `sa` |
| `Password` | (nếu dùng SQL Auth) | `your_password` |

**Ví dụ sửa cho các môi trường khác nhau:**

##### Integrated Security (Windows Authentication):
```xml
data source=your_server_name;initial catalog=HeThongQuanLyTrungTamNgoaiNgu;integrated security=True;
```

##### SQL Authentication:
```xml
data source=your_server_name;initial catalog=HeThongQuanLyTrungTamNgoaiNgu;user id=sa;password=your_password;
```

##### Azure SQL Server:
```xml
data source=your_server.database.windows.net;initial catalog=HeThongQuanLyTrungTamNgoaiNgu;user id=admin@server;password=P@ssw0rd;
```

### ▶️ Bước 4: Compile và Chạy

1. **Mở Visual Studio**
   - File → Open → Project/Solution
   - Chọn `QL_TrungTamNgoaiNgu.csproj`

2. **Restore NuGet Packages**
   - Tools → NuGet Package Manager → Manage NuGet Packages for Solution
   - Hoặc (Ctrl+Shift+B) để build

3. **Build Project**
   - Build → Build Solution (Ctrl+Shift+B)

4. **Chạy ứng dụng**
   - Debug → Start Debugging (F5)
   - Hoặc nhấp nút ▶ (Start Button) trên toolbar

---

## 🔐 Tài Khoản Test Mặc Định

Script `run_DB.sql` sẽ tạo sẵn các tài khoản sau:

| Email | Mật Khẩu | Role |
|:-------|:---------|:------|
| `admin@center.edu.vn` | `Admin@2024!` | Quản Trị Viên |
| `hva@student.edu.vn` | `HocVienA#99` | Học Viên |
| `hvb@student.edu.vn` | `HVBee@2025` | Học Viên |
| `ketoan1@center.edu.vn` | `KeToan$2024` | Kế Toán Chính |

---

## ✨ Các Tính Năng Chính

### 👨‍💼 Role Quản Trị Viên
- ✅ Quản lý người dùng (CRUD)
- ✅ Quản lý khóa học
- ✅ Quản lý lịch dạy
- ✅ Quản lý phòng học
- ✅ Báo cáo toàn hệ thống

### 📚 Role Học Viên
- ✅ Xem công nợ của mình
- ✅ **Thanh toán học phí** (với ghi chú tùy chọn)
- ✅ Xem lịch học
- ✅ Xem điểm số
- ✅ Tải bảng điểm (CSV)

### 💰 Role Kế Toán
- ✅ Xem tất cả giao dịch thanh toán
- ✅ Báo cáo doanh thu theo tháng
- ✅ Báo cáo công nợ
- ✅ **Theo dõi lịch sử giao dịch**
- ✅ Xuất CSV

### 👨‍🏫 Role Giảng Viên
- ✅ Quản lý lịch dạy riêng
- ✅ Nhập điểm học viên
- ✅ Xem danh sách lớp

---

## 🔧 Các Sửa Chữa Gần Đây (v5.2)

### ⭐ FIX: Lỗi Thanh Toán Học Phí

**Vấn đề:** Khi học viên thanh toán, hiển thị lỗi "An error occurred while updating the entries"

**Nguyên nhân:** 
- Foreign key `NguoiXacNhan` không được xác nhận tồn tại
- Thiếu ghi lịch sử giao dịch
- Thiếu validation số tiền

**Giải pháp (PaymentService.cs):**
```csharp
// ✅ Thêm kiểm tra người dùng tồn tại
var userExists = await db.NguoiDungs.AnyAsync(u => u.MaNguoiDung == nguoiXacNhan);
if (!userExists) throw new InvalidOperationException(...);

// ✅ Thêm validation
if (soTien <= 0) throw new InvalidOperationException(...);

// ✅ Thêm ghi lịch sử tự động
db.LichSuHeThongs.Add(new LichSuHeThong {
    TenBang = "GiaoDichThanhToan",
    HanhDong = "INSERT",
    NoiDung = $"Thanh toan hoa don {maHoaDon}: {soTien} VND",
    MaNguoiDung = nguoiXacNhan,
    NgayThucHien = DateTime.Now
});
```

**Kết quả:**
- ✅ Thanh toán hoạt động bình thường
- ✅ Tiền dư nợ tự động cập nhật
- ✅ Trạng thái hóa đơn cập nhật
- ✅ Kế toán thấy rõ dòng tiền trong tháng

---

## 📊 Cấu Trúc Database

```sql
-- Bảng chính
CREATE TABLE VaiTro (MaVaiTro INT PRIMARY KEY, TenVaiTro NVARCHAR(50))
CREATE TABLE NguoiDung (MaNguoiDung INT PRIMARY KEY, HoTen, Email, ...)
CREATE TABLE NguoiDung_VaiTro (MaNguoiDung, MaVaiTro) -- Many-to-Many

-- Khóa học
CREATE TABLE KhoaHoc (MaKhoaHoc INT PRIMARY KEY, TenKhoaHoc, ...)
CREATE TABLE DangKyKhoaHoc (MaDangKy INT PRIMARY KEY, MaHocVien, MaKhoaHoc, ...)

-- Thanh toán
CREATE TABLE HoaDonHocPhi (MaHoaDon INT PRIMARY KEY, MaDangKy, TongTien, TrangThai, ...)
CREATE TABLE GiaoDichThanhToan (MaGiaoDich INT PRIMARY KEY, MaHoaDon, SoTien, NgayGiaoDich, ...)

-- Lớp học
CREATE TABLE LichDay (MaLich INT PRIMARY KEY, MaKhoaHoc, MaGiangVien, NgayDay, ...)
CREATE TABLE LichHocVien (MaLichHocVien INT PRIMARY KEY, MaDangKy, MaLich, ...)

-- Điểm
CREATE TABLE DiemSo (MaDiem INT PRIMARY KEY, MaDangKy, DiemThao, DiemKiemTra, ...)

-- Lịch sử
CREATE TABLE LichSuHeThong (MaLichSu INT PRIMARY KEY, TenBang, HanhDong, NoiDung, ...)
```

---

## 🐛 Troubleshooting

### ❌ Lỗi: "Could not open connection to database"

**Nguyên nhân:** Server name sai hoặc SQL Server không chạy

**Giải pháp:**
1. Kiểm tra server name: `data source=?` trong `App.config`
2. Mở **SQL Server Configuration Manager**
3. Đảm bảo SQL Server service đang chạy
4. Kiểm tra port mặc định (1433)

### ❌ Lỗi: "Invalid object name 'HeThongQuanLyTrungTamNgoaiNgu'"

**Nguyên nhân:** Database chưa được tạo

**Giải pháp:**
- Chạy lại script `run_DB.sql` (xem Bước 2)

### ❌ Lỗi: "Login failed for user 'sa'"

**Nguyên nhân:** Sai mật khẩu SQL Server

**Giải pháp:**
1. Mở SSMS
2. Đặt lại mật khẩu cho user `sa`
3. Cập nhật `App.config` với mật khẩu mới

### ❌ Lỗi: "NuGet packages not restored"

**Giải pháp:**
```bash
# Trong Package Manager Console
Update-Package -Reinstall Entity Framework -Version 6.4.4
```

---

## 📝 Quy Ước Code

- **Naming:** camelCase cho biến, PascalCase cho class/property
- **Language:** Ghi chú bằng Tiếng Anh, tên biến tiếng Việt được phép
- **Structure:** MVVM pattern cho WPF, Service layer cho business logic
- **Database:** 3NF normalization, FK constraints bắt buộc

---

## 📞 Hỗ Trợ & Liên Hệ

- **Email:** tranhuuvong23092006@gmail.com
- **GitHub Issues:** [Report bugs](https://github.com/tranvonghoclaptrinh/doanNET/issues)
- **GitHub:** [tranvonghoclaptrinh](https://github.com/tranvonghoclaptrinh)

---

## 📄 License

MIT License - Xem [LICENSE](LICENSE) file để chi tiết

---

## 🔄 Commit History

- **v5.2** (May 2026)
  - ⭐ FIX: Sửa lỗi thanh toán học phí
  - ADD: Ghi lịch sử giao dịch tự động
  - ADD: Validation tiền thanh toán

- **v5.0** (Initial Release)
  - Core features

---

**Last Updated:** May 9, 2026  
**Author:** Trần Hữu Vọng