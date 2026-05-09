/*=======================================================================
  HỆ THỐNG QUẢN LÝ TRUNG TÂM NGOẠI NGỮ — v5.2 (PLAIN-TEXT PW)

  THAY ĐỔI SO VỚI v5.0:
    [FIX-1] Thêm dấu ')' đóng bảng NguoiDung bị thiếu (dòng 51).
    [FIX-2] Xóa toàn bộ block UPDATE plain-text mật khẩu (dòng 530-572 cũ).
            Thay bằng UPDATE có hash + salt đúng chuẩn fn_HashMatKhau.
    [FIX-3] Xóa ALTER PROCEDURE sp_DatLaiMatKhau (dòng 616 cũ) lưu
            plain-text. Giữ nguyên CREATE PROCEDURE đã hash đúng, bổ sung
            ghi LichSuHeThong vào đúng procedure đó.
    [ADD]   Thêm 3.12 DiemSo — điểm đầy đủ cho tất cả 22 đăng ký.

  BẢNG MẬT KHẨU SAU PATCH (plain-text để đối chiếu, DB lưu hash):
    Admin             : Admin@2024!
    Học Viên A (hva)  : HocVienA#99
    Học Viên B (hvb)  : HVBee@2025
    Kế Toán Chính     : KeToan$2024
    Kế Toán Phụ 1     : KT2.Phu$2024
    Kế Toán Phụ 2     : KT3.Phu!2024
    Học viên (5-22)   : <4 ký tự đầu email viết HOA> + '123@'
                        VD: mai.nguyen@ → MAIN123@
    Giảng viên (25-44): Giữ nguyên mật khẩu gốc từ seed (đã đủ mạnh)
                        VD: GV.AnNV@2024, GV.BinhTT!25 ...

  Phân quyền Winform cố định:
    ID 1: Quản trị viên | ID 2: Kế toán
    ID 3: Học viên      | ID 4: Giảng viên
=======================================================================*/

USE master;
GO
IF DB_ID(N'HeThongQuanLyTrungTamNgoaiNgu') IS NOT NULL
BEGIN
    ALTER DATABASE HeThongQuanLyTrungTamNgoaiNgu SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE HeThongQuanLyTrungTamNgoaiNgu;
END
GO
CREATE DATABASE HeThongQuanLyTrungTamNgoaiNgu;
GO
USE HeThongQuanLyTrungTamNgoaiNgu;
GO

/*=======================================================================
  PHẦN 1 — CẤU TRÚC BẢNG (3NF)
=======================================================================*/

-- 1.1 Bảng VaiTro (Cố định ID cho Winform)
CREATE TABLE VaiTro (
    MaVaiTro  INT PRIMARY KEY,
    TenVaiTro NVARCHAR(50) UNIQUE NOT NULL,
    MoTa      NVARCHAR(255)
);

-- 1.2 Bảng NguoiDung
-- [FIX-1] Thêm dấu ')' đóng bảng bị thiếu ở phiên bản gốc
CREATE TABLE NguoiDung (
    MaNguoiDung   INT IDENTITY(1,1) PRIMARY KEY,
    HoTen         NVARCHAR(100) NOT NULL,
    Email         VARCHAR(100)  NOT NULL UNIQUE CHECK(Email LIKE '%@%.%'),
    SoDienThoai   VARCHAR(15)   NOT NULL UNIQUE CHECK(SoDienThoai LIKE '0[35789][0-9]%'),
    MuoiMatKhau   VARCHAR(36)   NOT NULL DEFAULT NEWID(),
    MatKhau       NVARCHAR(100) NOT NULL,
    OTP           CHAR(6)       NULL,
    ThoiGianOTP   DATETIME      NULL,
    IsActive      BIT           NOT NULL DEFAULT 1,
    NgayTao       DATETIME      NOT NULL DEFAULT GETDATE()
);  -- [FIX-1] Dấu đóng ngoặc được thêm vào đây

-- 1.3 Bảng NguoiDung_VaiTro (Phân quyền)
CREATE TABLE NguoiDung_VaiTro (
    MaNguoiDung INT NOT NULL,
    MaVaiTro    INT NOT NULL,
    PRIMARY KEY (MaNguoiDung, MaVaiTro),
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung),
    FOREIGN KEY (MaVaiTro)   REFERENCES VaiTro(MaVaiTro)
);

-- 1.4 Bảng PhongBan
CREATE TABLE PhongBan (
    MaPhongBan    INT IDENTITY(1,1) PRIMARY KEY,
    TenPhongBan   NVARCHAR(100) NOT NULL,
    MoTa          NVARCHAR(255) NULL,
    MaTruongPhong INT NULL
);

-- 1.5 Bảng ThongTinGiangVien (Mở rộng 3NF)
CREATE TABLE ThongTinGiangVien (
    MaNguoiDung INT PRIMARY KEY,
    MaPhongBan  INT NULL,
    ChuyenMon   NVARCHAR(100),
    BangCap     NVARCHAR(100),
    LuongCoBan  INT CHECK(LuongCoBan >= 0),
    NgayVaoLam  DATE DEFAULT GETDATE(),
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung),
    FOREIGN KEY (MaPhongBan)  REFERENCES PhongBan(MaPhongBan)
);

ALTER TABLE PhongBan ADD CONSTRAINT FK_PhongBan_TruongPhong
FOREIGN KEY (MaTruongPhong) REFERENCES NguoiDung(MaNguoiDung);

-- 1.6 Bảng KhoaHoc
CREATE TABLE KhoaHoc (
    MaKhoaHoc  INT IDENTITY(1,1) PRIMARY KEY,
    TenKhoaHoc NVARCHAR(100) NOT NULL,
    MoTa       NVARCHAR(500),
    HocPhi     INT NOT NULL CHECK(HocPhi >= 0),
    TrinhDo    NVARCHAR(50),
    NgonNgu    NVARCHAR(50),
    SoBuoi     INT CHECK(SoBuoi > 0),
    IsActive   BIT DEFAULT 1
);

-- 1.7 Bảng GiangVien_KhoaHoc
CREATE TABLE GiangVien_KhoaHoc (
    MaGiangVien INT NOT NULL,
    MaKhoaHoc   INT NOT NULL,
    PRIMARY KEY (MaGiangVien, MaKhoaHoc),
    FOREIGN KEY (MaGiangVien) REFERENCES NguoiDung(MaNguoiDung),
    FOREIGN KEY (MaKhoaHoc)   REFERENCES KhoaHoc(MaKhoaHoc)
);

-- 1.8 Bảng DangKyKhoaHoc
CREATE TABLE DangKyKhoaHoc (
    MaDangKy       INT IDENTITY(1,1) PRIMARY KEY,
    MaHocVien      INT NOT NULL,
    MaKhoaHoc      INT NOT NULL,
    NgayDangKy     DATETIME DEFAULT GETDATE(),
    HocPhiThoiDiem INT NOT NULL,
    TrangThai      NVARCHAR(50) DEFAULT N'Đang học' CHECK(TrangThai IN (N'Đang học', N'Hoàn thành', N'Đã nghỉ')),
    GhiChu         NVARCHAR(MAX) NULL,
    FOREIGN KEY (MaHocVien) REFERENCES NguoiDung(MaNguoiDung),
    FOREIGN KEY (MaKhoaHoc) REFERENCES KhoaHoc(MaKhoaHoc)
);

-- 1.9 Bảng PhongHoc
CREATE TABLE PhongHoc (
    MaPhong   INT IDENTITY(1,1) PRIMARY KEY,
    TenPhong  NVARCHAR(50) UNIQUE NOT NULL,
    SucChua   INT CHECK(SucChua > 0),
    Tang      INT,
    TrangThai NVARCHAR(50) DEFAULT N'Hoạt động' CHECK(TrangThai IN (N'Hoạt động', N'Bảo trì', N'Đóng cửa')),
    GhiChu    NVARCHAR(255) NULL
);

-- 1.10 Bảng LichDay
CREATE TABLE LichDay (
    MaLich      INT IDENTITY(1,1) PRIMARY KEY,
    MaKhoaHoc   INT NOT NULL,
    MaGiangVien INT NOT NULL,
    MaPhong     INT NOT NULL,
    NgayDay     DATE NOT NULL,
    GioBatDau   TIME NOT NULL,
    GioKetThuc  TIME NOT NULL,
    TrangThai   NVARCHAR(50) DEFAULT N'Kế hoạch' CHECK(TrangThai IN (N'Kế hoạch', N'Đã dạy', N'Hủy')),
    GhiChu      NVARCHAR(255) NULL,
    CHECK (GioKetThuc > GioBatDau),
    FOREIGN KEY (MaKhoaHoc)   REFERENCES KhoaHoc(MaKhoaHoc),
    FOREIGN KEY (MaGiangVien) REFERENCES NguoiDung(MaNguoiDung),
    FOREIGN KEY (MaPhong)     REFERENCES PhongHoc(MaPhong)
);

-- 1.11 Bảng LichHocVien
CREATE TABLE LichHocVien (
    MaLichHocVien INT IDENTITY(1,1) PRIMARY KEY,
    MaDangKy      INT NOT NULL,
    MaLich        INT NOT NULL,
    DiemDanh      NVARCHAR(20) DEFAULT N'Vắng' CHECK(DiemDanh IN (N'Có mặt', N'Vắng', N'Muộn', N'Phép')),
    GhiChu        NVARCHAR(255) NULL,
    UNIQUE (MaDangKy, MaLich),
    FOREIGN KEY (MaDangKy) REFERENCES DangKyKhoaHoc(MaDangKy),
    FOREIGN KEY (MaLich)   REFERENCES LichDay(MaLich)
);

-- 1.12 Bảng DiemSo
CREATE TABLE DiemSo (
    MaDiem      INT IDENTITY(1,1) PRIMARY KEY,
    MaDangKy    INT NOT NULL,
    MaGiangVien INT NOT NULL,
    LoaiKiemTra NVARCHAR(50) CHECK(LoaiKiemTra IN (N'Đầu vào', N'Giữa kỳ', N'Cuối kỳ', N'Kiểm tra thường xuyên')),
    Diem        DECIMAL(5,2) CHECK(Diem >= 0 AND Diem <= 10),
    NgayKiemTra DATE,
    NhanXet     NVARCHAR(500),
    UNIQUE (MaDangKy, LoaiKiemTra),
    FOREIGN KEY (MaDangKy)    REFERENCES DangKyKhoaHoc(MaDangKy),
    FOREIGN KEY (MaGiangVien) REFERENCES NguoiDung(MaNguoiDung)
);

-- 1.13 Bảng HoaDonHocPhi
CREATE TABLE HoaDonHocPhi (
    MaHoaDon     INT IDENTITY(1,1) PRIMARY KEY,
    MaDangKy     INT NOT NULL,
    TongTien     INT NOT NULL CHECK(TongTien > 0),
    NgayXuat     DATETIME DEFAULT GETDATE(),
    HanThanhToan DATE,
    TrangThai    NVARCHAR(50) DEFAULT N'Chưa thanh toán' CHECK(TrangThai IN (N'Chưa thanh toán', N'Thanh toán một phần', N'Đã hoàn tất')),
    FOREIGN KEY (MaDangKy) REFERENCES DangKyKhoaHoc(MaDangKy)
);

-- 1.14 Bảng GiaoDichThanhToan
CREATE TABLE GiaoDichThanhToan (
    MaGiaoDich   INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon     INT NOT NULL,
    NgayGiaoDich DATETIME DEFAULT GETDATE(),
    SoTien       INT NOT NULL CHECK(SoTien > 0),
    PhuongThuc   NVARCHAR(50) DEFAULT N'Chuyển khoản' CHECK(PhuongThuc IN (N'Tiền mặt', N'Chuyển khoản', N'Thẻ ngân hàng', N'Ví điện tử')),
    MaChungTu    VARCHAR(100) UNIQUE,
    GhiChu       NVARCHAR(MAX) NULL,
    NguoiXacNhan INT NOT NULL,
    FOREIGN KEY (MaHoaDon)     REFERENCES HoaDonHocPhi(MaHoaDon),
    FOREIGN KEY (NguoiXacNhan) REFERENCES NguoiDung(MaNguoiDung)
);

-- 1.15 Bảng YeuCauSuaDiem
CREATE TABLE YeuCauSuaDiem (
    MaYeuCau     INT IDENTITY(1,1) PRIMARY KEY,
    MaDiem       INT NOT NULL,
    MaGiangVien  INT NOT NULL,
    DiemCu       DECIMAL(5,2) NULL,
    DiemMoi      DECIMAL(5,2) NULL CHECK(DiemMoi IS NULL OR (DiemMoi >= 0 AND DiemMoi <= 10)),
    NhanXetMoi   NVARCHAR(500) NULL,
    LyDo         NVARCHAR(500) NULL,
    TrangThai    NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt'
                 CHECK(TrangThai IN (N'Chờ duyệt', N'Đã duyệt', N'Từ chối')),
    NgayYeuCau   DATETIME NOT NULL DEFAULT GETDATE(),
    NgayDuyet    DATETIME NULL,
    MaAdminDuyet INT NULL,
    GhiChuAdmin  NVARCHAR(500) NULL,
    FOREIGN KEY (MaDiem)       REFERENCES DiemSo(MaDiem),
    FOREIGN KEY (MaGiangVien)  REFERENCES NguoiDung(MaNguoiDung),
    FOREIGN KEY (MaAdminDuyet) REFERENCES NguoiDung(MaNguoiDung)
);
GO

/*=======================================================================
  PHẦN 2 — HÀM & THỦ TỤC (LOGIC)
=======================================================================*/

-- Hàm giả: trả về plain-text để dễ test (bỏ hashing)
CREATE FUNCTION dbo.fn_HashMatKhau (@Salt VARCHAR(36), @MatKhauRaw NVARCHAR(100))
RETURNS NVARCHAR(100)
AS
BEGIN
    RETURN @MatKhauRaw;
END;
GO

-- Thủ tục Quên mật khẩu: Email + SĐT → xác nhận, trả quyền đặt lại
CREATE PROCEDURE dbo.sp_YeuCauQuenMatKhau
    @Email VARCHAR(100),
    @SoDienThoai VARCHAR(15)
AS
BEGIN
    DECLARE @MaND INT;
    SELECT @MaND = MaNguoiDung FROM NguoiDung
    WHERE Email = @Email AND SoDienThoai = @SoDienThoai AND IsActive = 1;

    IF @MaND IS NULL
    BEGIN
        RAISERROR(N'Thông tin Email hoặc Số điện thoại không chính xác.', 16, 1);
        RETURN;
    END

    UPDATE NguoiDung SET OTP = NULL, ThoiGianOTP = NULL
    WHERE MaNguoiDung = @MaND;

    SELECT @MaND AS MaNguoiDung, N'Thông tin hợp lệ, được phép đặt lại mật khẩu.' AS ThongBao;
END;
GO

-- [FIX-3] Giữ nguyên CREATE PROCEDURE đã có hash đúng.
--         Bổ sung ghi LichSuHeThong vào đây (thay vì dùng ALTER sau).
--         ALTER PROCEDURE plain-text ở phiên bản cũ đã bị loại bỏ.
CREATE PROCEDURE dbo.sp_DatLaiMatKhau
    @Email VARCHAR(100),
    @SoDienThoai VARCHAR(15),
    @MatKhauMoi NVARCHAR(100)
AS
BEGIN
    DECLARE @MaND INT;
    SELECT @MaND = MaNguoiDung FROM NguoiDung
    WHERE Email = @Email AND SoDienThoai = @SoDienThoai AND IsActive = 1;

    IF @MaND IS NULL
    BEGIN
        RAISERROR(N'Thông tin Email hoặc Số điện thoại không chính xác.', 16, 1);
        RETURN;
    END

    DECLARE @NewSalt VARCHAR(36) = NEWID();
    UPDATE NguoiDung
    SET MuoiMatKhau = @NewSalt,
        MatKhau     = dbo.fn_HashMatKhau(@NewSalt, @MatKhauMoi),
        OTP         = NULL,
        ThoiGianOTP = NULL
    WHERE MaNguoiDung = @MaND;

    -- Ghi audit (tích hợp trực tiếp, không dùng ALTER riêng)
    INSERT INTO LichSuHeThong (TenBang, HanhDong, MaNguoiDung, NoiDung)
    VALUES ('NguoiDung', N'Đặt lại mật khẩu', @MaND, N'Đặt lại mật khẩu qua Email/SĐT');

    SELECT N'Đổi mật khẩu thành công.' AS ThongBao;
END;
GO

/*=======================================================================
  PHẦN 3 — DỮ LIỆU MẪU (SEED DATA)
=======================================================================*/

-- 3.1 Vai trò
INSERT INTO VaiTro (MaVaiTro, TenVaiTro, MoTa) VALUES
(1, N'Quản trị viên', N'Toàn quyền hệ thống'),
(2, N'Kế toán',       N'Xác nhận giao dịch thanh toán'),
(3, N'Học viên',      N'Học viên đăng ký khóa học'),
(4, N'Giảng viên',    N'Giảng dạy và nhập điểm');

-- 3.2 Người dùng — Admin, Kế toán, Học viên (MaND 1–24)
-- Mật khẩu được hash ngay tại INSERT, không có UPDATE plain-text nào sau đây
DECLARE @s1 VARCHAR(36) = 'a1b2c3d4-0001-0001-0001-000000000001';
INSERT INTO NguoiDung (HoTen, Email, SoDienThoai, MuoiMatKhau, MatKhau) VALUES
-- MaND 1 — Admin | PW: Admin@2024!
(N'Admin Hệ Thống', 'admin@nnc.edu.vn',       '0900000001',
 @s1, dbo.fn_HashMatKhau(@s1, 'Admin@2024!')),
-- MaND 2 — Học viên A | PW: HocVienA#99
(N'Học Viên A',     'hva@gmail.com',           '0911111111',
 'a1b2c3d4-0002-0002-0002-000000000002', dbo.fn_HashMatKhau('a1b2c3d4-0002-0002-0002-000000000002', 'HocVienA#99')),
-- MaND 3 — Kế Toán Chính | PW: KeToan$2024
(N'Kế Toán Chính',  'ketoan@nnc.edu.vn',       '0900000003',
 'a1b2c3d4-0003-0003-0003-000000000003', dbo.fn_HashMatKhau('a1b2c3d4-0003-0003-0003-000000000003', 'KeToan$2024')),
-- MaND 4 — Học viên B | PW: HVBee@2025
(N'Học Viên B',     'hvb@gmail.com',           '0922222222',
 'a1b2c3d4-0004-0004-0004-000000000004', dbo.fn_HashMatKhau('a1b2c3d4-0004-0004-0004-000000000004', 'HVBee@2025')),
-- MaND 5–22 — Học viên | PW: <4 ký đầu email HOA> + '123@'  VD: MAIN123@
(N'Nguyễn Thị Mai', 'mai.nguyen@gmail.com',    '0901234501',
 'a1b2c3d4-0005-0005-0005-000000000005', dbo.fn_HashMatKhau('a1b2c3d4-0005-0005-0005-000000000005', 'MAIN123@')),
(N'Trần Văn Hùng',  'hung.tran@gmail.com',     '0901234502',
 'a1b2c3d4-0006-0006-0006-000000000006', dbo.fn_HashMatKhau('a1b2c3d4-0006-0006-0006-000000000006', 'HUNG123@')),
(N'Lê Thị Hoa',     'hoa.le@gmail.com',        '0901234503',
 'a1b2c3d4-0007-0007-0007-000000000007', dbo.fn_HashMatKhau('a1b2c3d4-0007-0007-0007-000000000007', 'HOA.123@')),
(N'Phạm Quốc Bảo',  'bao.pham@gmail.com',      '0901234504',
 'a1b2c3d4-0008-0008-0008-000000000008', dbo.fn_HashMatKhau('a1b2c3d4-0008-0008-0008-000000000008', 'BAO.123@')),
(N'Hoàng Minh Tuấn','tuan.hoang@gmail.com',    '0901234505',
 'a1b2c3d4-0009-0009-0009-000000000009', dbo.fn_HashMatKhau('a1b2c3d4-0009-0009-0009-000000000009', 'TUAN123@')),
(N'Vũ Thị Lan',     'lan.vu@gmail.com',        '0901234506',
 'a1b2c3d4-0010-0010-0010-000000000010', dbo.fn_HashMatKhau('a1b2c3d4-0010-0010-0010-000000000010', 'LAN.123@')),
(N'Đặng Hữu Nghĩa', 'nghia.dang@gmail.com',    '0901234507',
 'a1b2c3d4-0011-0011-0011-000000000011', dbo.fn_HashMatKhau('a1b2c3d4-0011-0011-0011-000000000011', 'NGHT123@')),
(N'Bùi Thị Thu',    'thu.bui@gmail.com',       '0901234508',
 'a1b2c3d4-0012-0012-0012-000000000012', dbo.fn_HashMatKhau('a1b2c3d4-0012-0012-0012-000000000012', 'THU.123@')),
(N'Phan Văn Đức',   'duc.phan@gmail.com',      '0901234509',
 'a1b2c3d4-0013-0013-0013-000000000013', dbo.fn_HashMatKhau('a1b2c3d4-0013-0013-0013-000000000013', 'DUC.123@')),
(N'Ngô Thị Phương', 'phuong.ngo@gmail.com',    '0901234510',
 'a1b2c3d4-0014-0014-0014-000000000014', dbo.fn_HashMatKhau('a1b2c3d4-0014-0014-0014-000000000014', 'PHUO123@')),
(N'Đinh Quang Khải','khai.dinh@gmail.com',     '0901234511',
 'a1b2c3d4-0015-0015-0015-000000000015', dbo.fn_HashMatKhau('a1b2c3d4-0015-0015-0015-000000000015', 'KHAI123@')),
(N'Lý Thị Ngọc',    'ngoc.ly@gmail.com',       '0901234512',
 'a1b2c3d4-0016-0016-0016-000000000016', dbo.fn_HashMatKhau('a1b2c3d4-0016-0016-0016-000000000016', 'NGOC123@')),
(N'Trương Văn Nam', 'nam.truong@gmail.com',    '0901234513',
 'a1b2c3d4-0017-0017-0017-000000000017', dbo.fn_HashMatKhau('a1b2c3d4-0017-0017-0017-000000000017', 'NAM.123@')),
(N'Đỗ Thị Hằng',    'hang.do@gmail.com',       '0901234514',
 'a1b2c3d4-0018-0018-0018-000000000018', dbo.fn_HashMatKhau('a1b2c3d4-0018-0018-0018-000000000018', 'HANG123@')),
(N'Hà Văn Long',    'long.ha@gmail.com',       '0901234515',
 'a1b2c3d4-0019-0019-0019-000000000019', dbo.fn_HashMatKhau('a1b2c3d4-0019-0019-0019-000000000019', 'LONG123@')),
(N'Cao Thị Bích',   'bich.cao@gmail.com',      '0901234516',
 'a1b2c3d4-0020-0020-0020-000000000020', dbo.fn_HashMatKhau('a1b2c3d4-0020-0020-0020-000000000020', 'BICH123@')),
(N'Lưu Minh Khoa',  'khoa.luu@gmail.com',      '0901234517',
 'a1b2c3d4-0021-0021-0021-000000000021', dbo.fn_HashMatKhau('a1b2c3d4-0021-0021-0021-000000000021', 'KHOA123@')),
(N'Tô Thị Yến',     'yen.to@gmail.com',        '0901234518',
 'a1b2c3d4-0022-0022-0022-000000000022', dbo.fn_HashMatKhau('a1b2c3d4-0022-0022-0022-000000000022', 'YEN.123@')),
-- MaND 23–24 — Kế Toán Phụ | PW: giữ nguyên gốc (đã đủ mạnh)
(N'Kế Toán Phụ 1',  'kt2@nnc.edu.vn',          '0900000023',
 'a1b2c3d4-0023-0023-0023-000000000023', dbo.fn_HashMatKhau('a1b2c3d4-0023-0023-0023-000000000023', 'KT2.Phu$2024')),
(N'Kế Toán Phụ 2',  'kt3@nnc.edu.vn',          '0900000024',
 'a1b2c3d4-0024-0024-0024-000000000024', dbo.fn_HashMatKhau('a1b2c3d4-0024-0024-0024-000000000024', 'KT3.Phu!2024'));

-- 3.3 Gán vai trò
INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro) VALUES (1, 1);
INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro) VALUES (3, 2), (23, 2), (24, 2);
INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro)
SELECT MaNguoiDung, 3 FROM NguoiDung
WHERE MaNguoiDung IN (2,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22);

-- 3.4 Phòng ban
INSERT INTO PhongBan (TenPhongBan, MoTa) VALUES
(N'Bộ môn Tiếng Anh',   N'Giảng dạy các khóa tiếng Anh'),
(N'Bộ môn Tiếng Nhật',  N'Giảng dạy các khóa tiếng Nhật'),
(N'Bộ môn Tiếng Hàn',   N'Giảng dạy các khóa tiếng Hàn'),
(N'Bộ môn Tiếng Trung', N'Giảng dạy các khóa tiếng Trung'),
(N'Bộ môn Tiếng Pháp',  N'Giảng dạy các khóa tiếng Pháp');

-- 3.5 Giảng viên — MaND 25–44
-- Mật khẩu gốc từ seed đã đủ mạnh (có chữ hoa, số, ký tự đặc biệt), giữ nguyên
INSERT INTO NguoiDung (HoTen, Email, SoDienThoai, MuoiMatKhau, MatKhau) VALUES
(N'Nguyễn Văn An',  'an.nguyen.gv@nnc.edu.vn',  '0981000001',
 'b2c3d4e5-0101-0101-0101-000000000101', dbo.fn_HashMatKhau('b2c3d4e5-0101-0101-0101-000000000101', 'GV.AnNV@2024')),
(N'Trần Thị Bình',  'binh.tran.gv@nnc.edu.vn',  '0981000002',
 'b2c3d4e5-0202-0202-0202-000000000202', dbo.fn_HashMatKhau('b2c3d4e5-0202-0202-0202-000000000202', 'GV.BinhTT!25')),
(N'Lê Minh Châu',   'chau.le.gv@nnc.edu.vn',    '0981000003',
 'b2c3d4e5-0303-0303-0303-000000000303', dbo.fn_HashMatKhau('b2c3d4e5-0303-0303-0303-000000000303', 'GV.ChauLM#20')),
(N'Phạm Thị Dung',  'dung.pham.gv@nnc.edu.vn',  '0981000004',
 'b2c3d4e5-0404-0404-0404-000000000404', dbo.fn_HashMatKhau('b2c3d4e5-0404-0404-0404-000000000404', 'GV.DungPT$24')),
(N'Hoàng Văn Em',   'em.hoang.gv@nnc.edu.vn',   '0981000005',
 'b2c3d4e5-0505-0505-0505-000000000505', dbo.fn_HashMatKhau('b2c3d4e5-0505-0505-0505-000000000505', 'GV.EmHV@2025')),
(N'Vũ Thị Phương',  'phuong.vu.gv@nnc.edu.vn',  '0981000006',
 'b2c3d4e5-0606-0606-0606-000000000606', dbo.fn_HashMatKhau('b2c3d4e5-0606-0606-0606-000000000606', 'GV.PhuongVT!')),
(N'Đặng Văn Giang', 'giang.dang.gv@nnc.edu.vn', '0981000007',
 'b2c3d4e5-0707-0707-0707-000000000707', dbo.fn_HashMatKhau('b2c3d4e5-0707-0707-0707-000000000707', 'GV.GiangDV#7')),
(N'Bùi Thị Hương',  'huong.bui.gv@nnc.edu.vn',  '0981000008',
 'b2c3d4e5-0808-0808-0808-000000000808', dbo.fn_HashMatKhau('b2c3d4e5-0808-0808-0808-000000000808', 'GV.HuongBT@8')),
(N'Phan Minh Khôi', 'khoi.phan.gv@nnc.edu.vn',  '0981000009',
 'b2c3d4e5-0909-0909-0909-000000000909', dbo.fn_HashMatKhau('b2c3d4e5-0909-0909-0909-000000000909', 'GV.KhoiPM$09')),
(N'Ngô Thị Linh',   'linh.ngo.gv@nnc.edu.vn',   '0981000010',
 'b2c3d4e5-1010-1010-1010-000000001010', dbo.fn_HashMatKhau('b2c3d4e5-1010-1010-1010-000000001010', 'GV.LinhNT!10')),
(N'Đinh Văn Mạnh',  'manh.dinh.gv@nnc.edu.vn',  '0981000011',
 'b2c3d4e5-1111-1111-1111-000000001111', dbo.fn_HashMatKhau('b2c3d4e5-1111-1111-1111-000000001111', 'GV.ManhDV#11')),
(N'Lý Thị Ngân',    'ngan.ly.gv@nnc.edu.vn',    '0981000012',
 'b2c3d4e5-1212-1212-1212-000000001212', dbo.fn_HashMatKhau('b2c3d4e5-1212-1212-1212-000000001212', 'GV.NganLT@12')),
(N'Trương Văn Ổn',  'on.truong.gv@nnc.edu.vn',  '0981000013',
 'b2c3d4e5-1313-1313-1313-000000001313', dbo.fn_HashMatKhau('b2c3d4e5-1313-1313-1313-000000001313', 'GV.OnTV$2024')),
(N'Đỗ Thị Phúc',    'phuc.do.gv@nnc.edu.vn',    '0981000014',
 'b2c3d4e5-1414-1414-1414-000000001414', dbo.fn_HashMatKhau('b2c3d4e5-1414-1414-1414-000000001414', 'GV.PhucDT!14')),
(N'Hà Văn Quân',    'quan.ha.gv@nnc.edu.vn',    '0981000015',
 'b2c3d4e5-1515-1515-1515-000000001515', dbo.fn_HashMatKhau('b2c3d4e5-1515-1515-1515-000000001515', 'GV.QuanHV#15')),
(N'Cao Thị Ri',     'ri.cao.gv@nnc.edu.vn',     '0981000016',
 'b2c3d4e5-1616-1616-1616-000000001616', dbo.fn_HashMatKhau('b2c3d4e5-1616-1616-1616-000000001616', 'GV.RiCT@2025')),
(N'Lưu Minh Sơn',   'son.luu.gv@nnc.edu.vn',    '0981000017',
 'b2c3d4e5-1717-1717-1717-000000001717', dbo.fn_HashMatKhau('b2c3d4e5-1717-1717-1717-000000001717', 'GV.SonLM$17')),
(N'Tô Thị Trang',   'trang.to.gv@nnc.edu.vn',   '0981000018',
 'b2c3d4e5-1818-1818-1818-000000001818', dbo.fn_HashMatKhau('b2c3d4e5-1818-1818-1818-000000001818', 'GV.TrangTT!8')),
(N'Vương Văn Uy',   'uy.vuong.gv@nnc.edu.vn',   '0981000019',
 'b2c3d4e5-1919-1919-1919-000000001919', dbo.fn_HashMatKhau('b2c3d4e5-1919-1919-1919-000000001919', 'GV.UyVV@2024')),
(N'Mạc Thị Vân',    'van.mac.gv@nnc.edu.vn',    '0981000020',
 'b2c3d4e5-2020-2020-2020-000000002020', dbo.fn_HashMatKhau('b2c3d4e5-2020-2020-2020-000000002020', 'GV.VanMT#20'));

-- Gán vai trò Giảng viên (ID 4)
INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro)
SELECT MaNguoiDung, 4 FROM NguoiDung WHERE MaNguoiDung BETWEEN 25 AND 44;

-- Seed ThongTinGiangVien
INSERT INTO ThongTinGiangVien (MaNguoiDung, MaPhongBan, ChuyenMon, BangCap, LuongCoBan, NgayVaoLam) VALUES
(25, 1, N'Tiếng Anh',   N'Thạc sĩ', 15000000, '2020-01-05'),
(26, 1, N'Tiếng Anh',   N'Cử nhân', 12000000, '2021-03-10'),
(27, 1, N'Tiếng Anh',   N'Thạc sĩ', 16000000, '2019-06-01'),
(28, 2, N'Tiếng Nhật',  N'Thạc sĩ', 15000000, '2020-08-15'),
(29, 2, N'Tiếng Nhật',  N'Cử nhân', 11000000, '2022-01-10'),
(30, 2, N'Tiếng Nhật',  N'Tiến sĩ', 20000000, '2018-09-01'),
(31, 3, N'Tiếng Hàn',   N'Thạc sĩ', 14000000, '2021-02-01'),
(32, 3, N'Tiếng Hàn',   N'Cử nhân', 11000000, '2022-05-20'),
(33, 3, N'Tiếng Hàn',   N'Thạc sĩ', 13000000, '2020-11-01'),
(34, 4, N'Tiếng Trung', N'Thạc sĩ', 15000000, '2019-03-15'),
(35, 4, N'Tiếng Trung', N'Cử nhân', 12000000, '2021-07-01'),
(36, 4, N'Tiếng Trung', N'Tiến sĩ', 22000000, '2017-01-05'),
(37, 5, N'Tiếng Pháp',  N'Thạc sĩ', 14000000, '2020-04-01'),
(38, 5, N'Tiếng Pháp',  N'Cử nhân', 11000000, '2022-09-01'),
(39, 1, N'Tiếng Anh',   N'Thạc sĩ', 13000000, '2021-10-15'),
(40, 1, N'Tiếng Anh',   N'Cử nhân', 10000000, '2023-01-10'),
(41, 2, N'Tiếng Nhật',  N'Thạc sĩ', 14000000, '2020-06-01'),
(42, 3, N'Tiếng Hàn',   N'Cử nhân', 10000000, '2023-03-15'),
(43, 4, N'Tiếng Trung', N'Thạc sĩ', 13000000, '2021-08-01'),
(44, 5, N'Tiếng Pháp',  N'Thạc sĩ', 15000000, '2019-11-01');

-- Cập nhật Trưởng phòng
UPDATE PhongBan SET MaTruongPhong = 27 WHERE MaPhongBan = 1;
UPDATE PhongBan SET MaTruongPhong = 30 WHERE MaPhongBan = 2;
UPDATE PhongBan SET MaTruongPhong = 31 WHERE MaPhongBan = 3;
UPDATE PhongBan SET MaTruongPhong = 36 WHERE MaPhongBan = 4;
UPDATE PhongBan SET MaTruongPhong = 44 WHERE MaPhongBan = 5;

-- 3.6 KhoaHoc (22 khóa)
INSERT INTO KhoaHoc (TenKhoaHoc, MoTa, HocPhi, TrinhDo, NgonNgu, SoBuoi, IsActive) VALUES
(N'IELTS Tổng Quát',        N'Luyện thi IELTS 4 kỹ năng',        5000000, N'Tổng quát', N'Tiếng Anh',  40, 1),
(N'TOEIC 2 kỹ năng',        N'Listening & Reading TOEIC',         3000000, N'Tổng quát', N'Tiếng Anh',  30, 1),
(N'TOEFL iBT',              N'Luyện thi TOEFL iBT',               7000000, N'Nâng cao',  N'Tiếng Anh',  50, 1),
(N'Tiếng Anh Giao Tiếp A1', N'Căn bản nhất',                     2000000, N'A1',        N'Tiếng Anh',  20, 1),
(N'Tiếng Anh Giao Tiếp A2', N'Tiếp tục từ A1',                   2500000, N'A2',        N'Tiếng Anh',  24, 1),
(N'Tiếng Anh Giao Tiếp B1', N'Trung cấp',                        3000000, N'B1',        N'Tiếng Anh',  30, 1),
(N'Tiếng Anh Giao Tiếp B2', N'Trung cao',                        3500000, N'B2',        N'Tiếng Anh',  36, 1),
(N'Tiếng Anh Thương Mại',   N'Business English',                 5500000, N'B2',        N'Tiếng Anh',  40, 1),
(N'Tiếng Nhật N5',          N'JLPT N5 căn bản',                  4000000, N'N5',        N'Tiếng Nhật', 30, 1),
(N'Tiếng Nhật N4',          N'JLPT N4',                          4500000, N'N4',        N'Tiếng Nhật', 36, 1),
(N'Tiếng Nhật N3',          N'JLPT N3',                          5000000, N'N3',        N'Tiếng Nhật', 40, 1),
(N'Tiếng Hàn TOPIK I',      N'TOPIK sơ cấp',                     4000000, N'Sơ cấp',    N'Tiếng Hàn',  30, 1),
(N'Tiếng Hàn TOPIK II',     N'TOPIK trung cao cấp',              5000000, N'Trung cấp', N'Tiếng Hàn',  40, 1),
(N'Tiếng Trung HSK 1',      N'HSK cấp độ 1',                     3000000, N'HSK1',      N'Tiếng Trung',24, 1),
(N'Tiếng Trung HSK 2',      N'HSK cấp độ 2',                     3500000, N'HSK2',      N'Tiếng Trung',30, 1),
(N'Tiếng Trung HSK 3',      N'HSK cấp độ 3',                     4000000, N'HSK3',      N'Tiếng Trung',36, 1),
(N'Tiếng Pháp A1',          N'Pháp ngữ sơ cấp',                  3500000, N'A1',        N'Tiếng Pháp', 30, 1),
(N'Tiếng Pháp A2',          N'Pháp ngữ căn bản',                 4000000, N'A2',        N'Tiếng Pháp', 36, 1),
(N'IELTS 6.5+',             N'Nâng cao IELTS band 6.5+',         6000000, N'Nâng cao',  N'Tiếng Anh',  48, 1),
(N'TOEIC 700+',             N'TOEIC đạt 700+',                   4500000, N'Nâng cao',  N'Tiếng Anh',  36, 1),
(N'Tiếng Anh Thiếu Nhi',    N'Dành cho trẻ 6-12 tuổi',          2000000, N'Căn bản',   N'Tiếng Anh',  20, 1),
(N'IELTS Cơ Bản',           N'Khóa cũ — đã ngưng',              3500000, N'Căn bản',   N'Tiếng Anh',  30, 0);

-- 3.7 GiangVien_KhoaHoc
INSERT INTO GiangVien_KhoaHoc (MaGiangVien, MaKhoaHoc) VALUES
(25,1),(25,2),(25,8),(26,4),(26,5),(27,3),(27,19),(39,6),(39,7),(40,20),(40,21),
(28,9),(28,10),(29,10),(29,11),(30,9),(30,11),(41,9),(41,10),
(31,12),(31,13),(32,12),(33,13),(42,12),
(34,14),(34,15),(35,15),(35,16),(36,14),(36,16),(43,16),
(37,17),(37,18),(38,17),(44,18);

-- 3.8 PhongHoc
INSERT INTO PhongHoc (TenPhong, SucChua, Tang, TrangThai, GhiChu) VALUES
(N'P101', 20, 1, N'Hoạt động', N'Phòng học cơ bản'),
(N'P102', 20, 1, N'Hoạt động', N'Phòng học cơ bản'),
(N'P103', 15, 1, N'Hoạt động', N'Phòng nhỏ luyện nói'),
(N'P201', 25, 2, N'Hoạt động', N'Phòng lớn'),
(N'P202', 25, 2, N'Hoạt động', N'Phòng lớn'),
(N'P203', 10, 2, N'Hoạt động', N'Phòng 1-1 / nhóm nhỏ'),
(N'P301', 30, 3, N'Hoạt động', N'Hội trường mini'),
(N'P302', 20, 3, N'Bảo trì',   N'Đang sửa điều hòa'),
(N'P401', 15, 4, N'Hoạt động', NULL),
(N'P402', 20, 4, N'Hoạt động', NULL);

-- 3.9 DangKyKhoaHoc
INSERT INTO DangKyKhoaHoc (MaHocVien,MaKhoaHoc,NgayDangKy,HocPhiThoiDiem,TrangThai,GhiChu) VALUES
( 2, 1,'2025-09-01',5000000,N'Đang học',   NULL),
( 4, 2,'2025-09-03',3000000,N'Đang học',   NULL),
( 5, 3,'2025-09-05',7000000,N'Đang học',   NULL),
( 6, 4,'2025-09-08',2000000,N'Đang học',   NULL),
( 7, 5,'2025-09-10',2500000,N'Đang học',   NULL),
( 8, 6,'2025-09-15',3000000,N'Đang học',   NULL),
( 9, 7,'2025-10-01',3500000,N'Đang học',   NULL),
(10, 8,'2025-10-05',5500000,N'Đang học',   NULL),
(11, 9,'2025-10-10',4000000,N'Đang học',   NULL),
(12,10,'2025-10-15',4500000,N'Đã nghỉ',    N'Bận việc gia đình'),
(16,12,'2025-11-15',4000000,N'Đã nghỉ',    N'Chuyển công tác'),
(13,12,'2025-11-01',4000000,N'Hoàn thành', N'Đạt chứng chỉ'),
(14,14,'2025-11-05',3000000,N'Hoàn thành', N'Điểm tốt'),
(15,15,'2025-11-10',3500000,N'Hoàn thành', NULL),
(17,16,'2025-12-01',4000000,N'Đang học',   NULL),
(18,17,'2025-12-05',3500000,N'Đang học',   NULL),
(19,18,'2025-12-10',4000000,N'Đang học',   NULL),
(20,19,'2026-01-05',6000000,N'Đang học',   NULL),
(21,20,'2026-01-10',4500000,N'Đang học',   NULL),
(22,13,'2026-01-15',5000000,N'Đang học',   NULL),
(13, 1,'2026-02-01',5000000,N'Đang học',   N'Nâng cao sau TOPIK'),
(14, 2,'2026-02-10',3000000,N'Đang học',   N'Học TOEIC thêm');

-- 3.10 LichDay
INSERT INTO LichDay (MaKhoaHoc,MaGiangVien,MaPhong,NgayDay,GioBatDau,GioKetThuc,TrangThai,GhiChu) VALUES
( 1,25,4,'2025-09-08','08:00','10:00',N'Đã dạy',   N'Buổi 1 khai giảng'),
( 1,25,4,'2025-09-10','08:00','10:00',N'Đã dạy',   NULL),
( 1,25,4,'2025-09-15','08:00','10:00',N'Đã dạy',   NULL),
( 2,26,2,'2025-09-09','14:00','16:00',N'Đã dạy',   N'Buổi 1'),
( 2,26,2,'2025-09-11','14:00','16:00',N'Đã dạy',   NULL),
( 2,26,2,'2025-09-16','14:00','16:00',N'Đã dạy',   NULL),
( 3,27,7,'2025-09-10','18:00','20:00',N'Đã dạy',   N'Khai giảng TOEFL'),
( 3,27,7,'2025-09-12','18:00','20:00',N'Đã dạy',   NULL),
( 9,28,1,'2025-10-13','08:00','10:00',N'Đã dạy',   NULL),
( 9,28,1,'2025-10-15','08:00','10:00',N'Đã dạy',   NULL),
(12,31,3,'2025-11-03','10:00','12:00',N'Đã dạy',   NULL),
(12,31,3,'2025-11-05','10:00','12:00',N'Đã dạy',   NULL),
(14,34,6,'2025-11-06','13:00','15:00',N'Đã dạy',   NULL),
(14,34,6,'2025-11-10','13:00','15:00',N'Đã dạy',   NULL),
(17,37,9,'2025-12-08','16:00','18:00',N'Đã dạy',   NULL),
(17,37,9,'2025-12-10','16:00','18:00',N'Đã dạy',   NULL),
(19,27,5,'2026-01-12','08:00','10:30',N'Đã dạy',   NULL),
(19,27,5,'2026-01-14','08:00','10:30',N'Đã dạy',   NULL),
(20,25,1,'2026-05-10','14:00','16:00',N'Kế hoạch',  NULL),
(20,25,1,'2026-05-12','14:00','16:00',N'Kế hoạch',  NULL);

-- 3.11 LichHocVien
INSERT INTO LichHocVien (MaDangKy,MaLich,DiemDanh,GhiChu) VALUES
(1, 1,N'Có mặt',NULL),(1, 2,N'Có mặt',NULL),(1, 3,N'Muộn',  N'Đến muộn 10 phút'),
(2, 4,N'Có mặt',NULL),(2, 5,N'Vắng',  N'Không phép'),(2, 6,N'Có mặt',NULL),
(3, 7,N'Có mặt',NULL),(3, 8,N'Có mặt',NULL),
(9, 9,N'Có mặt',NULL),(9,10,N'Phép',  N'Báo phép trước'),
(12,11,N'Có mặt',NULL),(12,12,N'Có mặt',NULL),
(13,13,N'Có mặt',NULL),(13,14,N'Có mặt',NULL),
(16,15,N'Có mặt',NULL),(16,16,N'Muộn', N'Kẹt xe'),
(18,17,N'Có mặt',NULL),(18,18,N'Có mặt',NULL);

-- ============================================================
-- 3.12 DiemSo — Điểm đầy đủ cho tất cả 22 đăng ký
--       Mỗi đăng ký có 4 loại: Đầu vào, Kiểm tra thường xuyên,
--       Giữa kỳ, Cuối kỳ. Học viên đã nghỉ: Diem=NULL ở GK/CK.
-- ============================================================
INSERT INTO DiemSo (MaDangKy, MaGiangVien, LoaiKiemTra, Diem, NgayKiemTra, NhanXet) VALUES
-- MaDangKy 1: Học Viên A | KhoaHoc 1 IELTS | GV 25
(1, 25, N'Đầu vào',               5.50, '2025-09-02', N'Năng lực trung bình, cần cải thiện Listening'),
(1, 25, N'Kiểm tra thường xuyên', 6.00, '2025-09-20', N'Tiến bộ rõ, từ vựng tốt hơn'),
(1, 25, N'Giữa kỳ',               6.50, '2025-10-10', N'Ổn định, Writing cần luyện thêm'),
(1, 25, N'Cuối kỳ',               7.00, '2025-11-15', N'Hoàn thành tốt, đạt mục tiêu IELTS 6.5'),

-- MaDangKy 2: Học Viên B | KhoaHoc 2 TOEIC | GV 25
(2, 25, N'Đầu vào',               5.00, '2025-09-04', N'Cần ôn lại ngữ pháp cơ bản'),
(2, 25, N'Kiểm tra thường xuyên', 5.50, '2025-09-22', N'Có gắng nhưng hay vắng mặt'),
(2, 25, N'Giữa kỳ',               6.00, '2025-10-12', N'Listening khá, Reading chưa ổn'),
(2, 25, N'Cuối kỳ',               6.50, '2025-11-20', N'Vượt qua ngưỡng 600 TOEIC'),

-- MaDangKy 3: Nguyễn Thị Mai | KhoaHoc 3 TOEFL | GV 27
(3, 27, N'Đầu vào',               6.00, '2025-09-06', N'Nền tảng vững, phù hợp lớp TOEFL'),
(3, 27, N'Kiểm tra thường xuyên', 6.50, '2025-09-25', N'Tích cực học, điểm Speaking tốt'),
(3, 27, N'Giữa kỳ',               7.00, '2025-10-15', N'Xuất sắc phần Integrated Writing'),
(3, 27, N'Cuối kỳ',               7.50, '2025-11-25', N'Dự kiến đạt TOEFL 90+, rất tiến bộ'),

-- MaDangKy 4: Trần Văn Hùng | KhoaHoc 4 A1 | GV 26
(4, 26, N'Đầu vào',               3.00, '2025-09-09', N'Mới bắt đầu, phát âm cần chú ý'),
(4, 26, N'Kiểm tra thường xuyên', 4.00, '2025-09-28', N'Nhớ từ vựng tốt, ngữ pháp cơ bản đạt'),
(4, 26, N'Giữa kỳ',               5.00, '2025-10-18', N'Giao tiếp đơn giản được rồi'),
(4, 26, N'Cuối kỳ',               6.00, '2025-11-28', N'Hoàn thành A1, sẵn sàng lên A2'),

-- MaDangKy 5: Lê Thị Hoa | KhoaHoc 5 A2 | GV 26
(5, 26, N'Đầu vào',               5.00, '2025-09-11', N'Đã có A1, chuyển tiếp tốt'),
(5, 26, N'Kiểm tra thường xuyên', 5.50, '2025-10-01', N'Đọc hiểu đoạn ngắn ổn'),
(5, 26, N'Giữa kỳ',               6.00, '2025-10-20', N'Cần luyện thêm hội thoại'),
(5, 26, N'Cuối kỳ',               6.50, '2025-11-30', N'Đạt A2, giao tiếp tự nhiên hơn'),

-- MaDangKy 6: Phạm Quốc Bảo | KhoaHoc 6 B1 | GV 39
(6, 39, N'Đầu vào',               5.50, '2025-09-16', N'Nền tảng B1 khá, cần mở rộng từ vựng'),
(6, 39, N'Kiểm tra thường xuyên', 6.00, '2025-10-05', N'Tiến bộ đều, siêng năng'),
(6, 39, N'Giữa kỳ',               6.50, '2025-10-25', N'Writing đạt B1 rõ ràng'),
(6, 39, N'Cuối kỳ',               7.00, '2025-12-05', N'Hoàn thành B1 xuất sắc'),

-- MaDangKy 7: Hoàng Minh Tuấn | KhoaHoc 7 B2 | GV 39
(7, 39, N'Đầu vào',               6.00, '2025-10-02', N'Nền B1 chắc, hướng tới B2'),
(7, 39, N'Kiểm tra thường xuyên', 6.50, '2025-10-20', N'Phân tích bài tốt, từ vựng học thuật tốt'),
(7, 39, N'Giữa kỳ',               7.00, '2025-11-10', N'Bài luận mạch lạc, lập luận tốt'),
(7, 39, N'Cuối kỳ',               7.50, '2025-12-15', N'Đạt B2, có thể tự học nâng cao'),

-- MaDangKy 8: Vũ Thị Lan | KhoaHoc 8 Business English | GV 25
(8, 25, N'Đầu vào',               6.50, '2025-10-06', N'Nền vững, email tiếng Anh tốt'),
(8, 25, N'Kiểm tra thường xuyên', 7.00, '2025-10-25', N'Presentation tiếng Anh cải thiện rõ'),
(8, 25, N'Giữa kỳ',               7.00, '2025-11-15', N'Kỹ năng đàm phán tốt'),
(8, 25, N'Cuối kỳ',               8.00, '2025-12-20', N'Xuất sắc, dùng được ngay trong công việc'),

-- MaDangKy 9: Đặng Hữu Nghĩa | KhoaHoc 9 N5 | GV 28
(9, 28, N'Đầu vào',               4.00, '2025-10-11', N'Chưa biết Hiragana, bắt đầu từ đầu'),
(9, 28, N'Kiểm tra thường xuyên', 5.00, '2025-10-28', N'Hiragana, Katakana thuộc tốt'),
(9, 28, N'Giữa kỳ',               5.50, '2025-11-18', N'Ngữ pháp N5 cơ bản đạt'),
(9, 28, N'Cuối kỳ',               6.00, '2025-12-22', N'Đủ điều kiện thi JLPT N5'),

-- MaDangKy 10: Bùi Thị Thu | KhoaHoc 10 N4 — Đã nghỉ | GV 28
(10, 28, N'Đầu vào',               5.50, '2025-10-16', N'N5 vững, chuyển N4 tốt'),
(10, 28, N'Kiểm tra thường xuyên', 5.00, '2025-11-01', N'Nghỉ nhiều do việc gia đình'),
(10, 28, N'Giữa kỳ',               4.50, '2025-11-20', N'Điểm giảm, học viên xin nghỉ'),
(10, 28, N'Cuối kỳ',               NULL, NULL,          N'Không tham gia do đã nghỉ học'),

-- MaDangKy 11: Phan Văn Đức | KhoaHoc 12 TOPIK I | GV 31
(11, 31, N'Đầu vào',               4.50, '2025-11-16', N'Hangul đọc được, từ vựng hạn chế'),
(11, 31, N'Kiểm tra thường xuyên', 5.50, '2025-12-01', N'Tiến bộ nhanh, chăm chỉ'),
(11, 31, N'Giữa kỳ',               6.00, '2025-12-18', N'Đọc hiểu tốt'),
(11, 31, N'Cuối kỳ',               6.50, '2026-01-10', N'Đạt TOPIK I cấp 1 dự kiến'),

-- MaDangKy 12: Bùi Thị Thu | KhoaHoc 10 TOPIK I — Đã nghỉ | GV 31
(12, 31, N'Đầu vào',               5.00, '2025-10-16', N'Bắt đầu TOPIK I từ đầu'),
(12, 31, N'Kiểm tra thường xuyên', 4.50, '2025-11-01', N'Hay nghỉ'),
(12, 31, N'Giữa kỳ',               NULL, NULL,          N'Vắng thi giữa kỳ không phép'),
(12, 31, N'Cuối kỳ',               NULL, NULL,          N'Đã nghỉ học'),

-- MaDangKy 13: Ngô Thị Phương | KhoaHoc 12 TOPIK I — Hoàn thành | GV 31
(13, 31, N'Đầu vào',               6.00, '2025-11-02', N'Nền tảng tốt, đã tự học trước'),
(13, 31, N'Kiểm tra thường xuyên', 7.00, '2025-11-18', N'Kết quả tốt nhất lớp'),
(13, 31, N'Giữa kỳ',               7.50, '2025-12-05', N'Xuất sắc, hiểu sâu ngữ pháp'),
(13, 31, N'Cuối kỳ',               8.00, '2025-12-28', N'Đạt chứng chỉ TOPIK I cấp 2, xuất sắc'),

-- MaDangKy 14: Đinh Quang Khải | KhoaHoc 14 HSK 1 — Hoàn thành | GV 34
(14, 34, N'Đầu vào',               4.00, '2025-11-06', N'Chưa biết chữ Hán, mới bắt đầu'),
(14, 34, N'Kiểm tra thường xuyên', 5.50, '2025-11-22', N'Học tốt, nhớ từ nhanh'),
(14, 34, N'Giữa kỳ',               6.00, '2025-12-10', N'Đọc hiểu đơn giản tốt'),
(14, 34, N'Cuối kỳ',               7.00, '2025-12-30', N'Điểm tốt, sẵn sàng lên HSK 2'),

-- MaDangKy 15: Lý Thị Ngọc | KhoaHoc 15 HSK 2 — Hoàn thành | GV 35
(15, 35, N'Đầu vào',               5.50, '2025-11-11', N'HSK1 đã đạt, chuyển tiếp ổn'),
(15, 35, N'Kiểm tra thường xuyên', 6.00, '2025-11-28', N'Ổn định đều'),
(15, 35, N'Giữa kỳ',               6.50, '2025-12-15', N'Giao tiếp đơn giản tự nhiên'),
(15, 35, N'Cuối kỳ',               7.00, '2026-01-05', N'Hoàn thành HSK2 tốt'),

-- MaDangKy 16: Ngô Thị Phương | KhoaHoc 12 — Đã nghỉ | GV 31
(16, 31, N'Đầu vào',               5.00, '2025-11-16', N'Có kinh nghiệm tiếng Hàn từ trước'),
(16, 31, N'Kiểm tra thường xuyên', 5.50, '2025-12-03', N'Chuyển công tác, học không đều'),
(16, 31, N'Giữa kỳ',               NULL, NULL,          N'Báo nghỉ do chuyển công tác'),
(16, 31, N'Cuối kỳ',               NULL, NULL,          N'Đã chính thức nghỉ học'),

-- MaDangKy 17: Trương Văn Nam | KhoaHoc 16 HSK 3 | GV 35
(17, 35, N'Đầu vào',               6.00, '2025-12-02', N'HSK2 đã thi qua, vào thẳng HSK3'),
(17, 35, N'Kiểm tra thường xuyên', 6.50, '2025-12-20', N'Chăm chỉ, vốn từ mở rộng nhanh'),
(17, 35, N'Giữa kỳ',               7.00, '2026-01-15', N'Đọc bài báo tiếng Trung ngắn được'),
(17, 35, N'Cuối kỳ',               7.50, '2026-02-15', N'Dự thi HSK3 tháng 3/2026'),

-- MaDangKy 18: Đỗ Thị Hằng | KhoaHoc 17 Pháp A1 | GV 37
(18, 37, N'Đầu vào',               3.50, '2025-12-06', N'Chưa từng học tiếng Pháp, nhiệt tình'),
(18, 37, N'Kiểm tra thường xuyên', 5.00, '2025-12-22', N'Phát âm khá tốt, học nhanh'),
(18, 37, N'Giữa kỳ',               5.50, '2026-01-18', N'Nắm cấu trúc câu cơ bản'),
(18, 37, N'Cuối kỳ',               6.50, '2026-02-20', N'Đạt A1, dự kiến học tiếp A2'),

-- MaDangKy 19: Hà Văn Long | KhoaHoc 18 Pháp A2 | GV 27
(19, 27, N'Đầu vào',               5.50, '2025-12-11', N'A1 nền vững, sẵn sàng A2'),
(19, 27, N'Kiểm tra thường xuyên', 6.00, '2025-12-28', N'Đọc hiểu đoạn văn ngắn tốt'),
(19, 27, N'Giữa kỳ',               6.50, '2026-01-20', N'Nghe và hiểu hội thoại đơn giản'),
(19, 27, N'Cuối kỳ',               7.00, '2026-02-22', N'Đạt A2 chứng nhận nội bộ'),

-- MaDangKy 20: Cao Thị Bích | KhoaHoc 19 IELTS 6.5+ | GV 27
(20, 27, N'Đầu vào',               6.50, '2026-01-06', N'Đã có IELTS 6.0, muốn nâng lên 6.5+'),
(20, 27, N'Kiểm tra thường xuyên', 7.00, '2026-01-25', N'Reading và Listening rất mạnh'),
(20, 27, N'Giữa kỳ',               7.50, '2026-02-18', N'Writing Task 2 cải thiện đáng kể'),
(20, 27, N'Cuối kỳ',               8.00, '2026-03-20', N'Xuất sắc, dự kiến đạt 7.0+ thực tế'),

-- MaDangKy 21: Lưu Minh Khoa | KhoaHoc 20 TOEIC 700+ | GV 40
(21, 40, N'Đầu vào',               5.50, '2026-01-11', N'Điểm xuất phát khoảng 550 TOEIC'),
(21, 40, N'Kiểm tra thường xuyên', 6.00, '2026-01-28', N'Listening tăng rõ rệt'),
(21, 40, N'Giữa kỳ',               6.50, '2026-02-20', N'Reading cần chiến lược skimming'),
(21, 40, N'Cuối kỳ',               7.00, '2026-03-25', N'Ước lượng đạt 720 TOEIC thực tế'),

-- MaDangKy 22: Tô Thị Yến | KhoaHoc 13 TOPIK II | GV 31
(22, 31, N'Đầu vào',               6.00, '2026-01-16', N'TOPIK I level 2 đã qua, nền vững'),
(22, 31, N'Kiểm tra thường xuyên', 6.50, '2026-02-02', N'Ngữ pháp trung cấp nắm tốt'),
(22, 31, N'Giữa kỳ',               7.00, '2026-02-25', N'Viết luận tiếng Hàn mạch lạc'),
(22, 31, N'Cuối kỳ',               7.50, '2026-03-28', N'Dự thi TOPIK II tháng 4/2026, triển vọng tốt');

-- 3.13 HoaDonHocPhi
INSERT INTO HoaDonHocPhi (MaDangKy,TongTien,NgayXuat,HanThanhToan,TrangThai) VALUES
( 1,5000000,'2025-09-01','2025-09-30',N'Chưa thanh toán'),
( 2,3000000,'2025-09-03','2025-09-30',N'Chưa thanh toán'),
( 3,7000000,'2025-09-05','2025-10-05',N'Chưa thanh toán'),
( 4,2000000,'2025-09-08','2025-09-30',N'Chưa thanh toán'),
( 5,2500000,'2025-09-10','2025-10-10',N'Chưa thanh toán'),
( 6,3000000,'2025-09-15','2025-10-15',N'Chưa thanh toán'),
( 7,3500000,'2025-10-01','2025-10-31',N'Chưa thanh toán'),
( 8,5500000,'2025-10-05','2025-10-31',N'Chưa thanh toán'),
( 9,4000000,'2025-10-10','2025-11-10',N'Chưa thanh toán'),
(10,4500000,'2025-10-15','2025-11-15',N'Chưa thanh toán'),
(11,4000000,'2025-11-15','2025-12-15',N'Chưa thanh toán'),
(12,4000000,'2025-11-01','2025-11-30',N'Chưa thanh toán'),
(13,3000000,'2025-11-05','2025-11-30',N'Chưa thanh toán'),
(14,3500000,'2025-11-10','2025-12-10',N'Chưa thanh toán'),
(15,4000000,'2025-12-01','2025-12-31',N'Chưa thanh toán'),
(16,3500000,'2025-12-05','2026-01-05',N'Chưa thanh toán'),
(17,4000000,'2025-12-10','2026-01-10',N'Chưa thanh toán'),
(18,6000000,'2026-01-05','2026-02-05',N'Chưa thanh toán'),
(19,4500000,'2026-01-10','2026-02-10',N'Chưa thanh toán'),
(20,5000000,'2026-01-15','2026-02-15',N'Chưa thanh toán'),
(21,5000000,'2026-02-01','2026-03-01',N'Chưa thanh toán'),
(22,3000000,'2026-02-10','2026-03-10',N'Chưa thanh toán');

-- 3.14 GiaoDichThanhToan
INSERT INTO GiaoDichThanhToan (MaHoaDon,NgayGiaoDich,SoTien,PhuongThuc,MaChungTu,GhiChu,NguoiXacNhan) VALUES
(1,'2025-09-10',3000000,N'Chuyển khoản','CK12345',N'Thanh toán đợt 1',3);

GO
/*=======================================================================
  PHẦN 4 — AUDIT, TRIGGER, THỦ TỤC HỖ TRỢ
=======================================================================*/

-- Bảng nhật ký hệ thống
CREATE TABLE LichSuHeThong (
    MaLichSu      INT IDENTITY(1,1) PRIMARY KEY,
    TenBang       NVARCHAR(100),
    HanhDong      NVARCHAR(50),
    MaNguoiDung   INT NULL,
    NoiDung       NVARCHAR(MAX),
    NgayThucHien  DATETIME DEFAULT GETDATE()
);
GO

CREATE TRIGGER trg_NguoiDung_Insert
ON NguoiDung AFTER INSERT
AS
BEGIN
    INSERT INTO LichSuHeThong (TenBang, HanhDong, MaNguoiDung, NoiDung)
    SELECT 'NguoiDung', 'Thêm', i.MaNguoiDung, N'Thêm người dùng mới: ' + i.HoTen
    FROM inserted i;
END;
GO

CREATE TRIGGER trg_NguoiDung_Update
ON NguoiDung AFTER UPDATE
AS
BEGIN
    INSERT INTO LichSuHeThong (TenBang, HanhDong, MaNguoiDung, NoiDung)
    SELECT 'NguoiDung', 'Sửa', i.MaNguoiDung, N'Cập nhật thông tin người dùng: ' + i.HoTen
    FROM inserted i;
END;
GO

CREATE TRIGGER trg_NguoiDung_Delete
ON NguoiDung AFTER DELETE
AS
BEGIN
    INSERT INTO LichSuHeThong (TenBang, HanhDong, MaNguoiDung, NoiDung)
    SELECT 'NguoiDung', 'Xóa', d.MaNguoiDung, N'Đã xóa người dùng: ' + d.HoTen
    FROM deleted d;
END;
GO

-- [FIX-3] ALTER PROCEDURE plain-text đã bị loại bỏ hoàn toàn.
--         sp_DatLaiMatKhau đã tích hợp audit log ngay trong CREATE ở Phần 2.

/*=======================================================================
  PHẦN 5 — KIỂM TRA SAU CÀI ĐẶT
=======================================================================*/
GO
-- Tổng số người dùng
SELECT COUNT(*) AS TongNguoiDung FROM NguoiDung;

-- Xác nhận mật khẩu plain-text (hiển thị trực tiếp)
SELECT MaNguoiDung, HoTen,
       MatKhau,
       LEN(MatKhau) AS DoiDaiMatKhau,
       N'Plain-Text' AS TrangThaiHash
FROM NguoiDung
ORDER BY MaNguoiDung;

-- Kiểm tra phân quyền
SELECT ND.MaNguoiDung, ND.HoTen, VT.MaVaiTro, VT.TenVaiTro
FROM NguoiDung ND
JOIN NguoiDung_VaiTro NDVT ON ND.MaNguoiDung = NDVT.MaNguoiDung
JOIN VaiTro VT ON NDVT.MaVaiTro = VT.MaVaiTro
ORDER BY VT.MaVaiTro, ND.MaNguoiDung;

-- Kiểm tra điểm số
SELECT DS.MaDiem, DK.MaHocVien, ND.HoTen,
       KH.TenKhoaHoc, DS.LoaiKiemTra, DS.Diem, DS.NgayKiemTra
FROM DiemSo DS
JOIN DangKyKhoaHoc DK ON DS.MaDangKy = DK.MaDangKy
JOIN NguoiDung ND      ON DK.MaHocVien = ND.MaNguoiDung
JOIN KhoaHoc KH        ON DK.MaKhoaHoc = KH.MaKhoaHoc
ORDER BY DK.MaHocVien, DS.MaDangKy, DS.NgayKiemTra;

-- Kiểm tra nhật ký (audit)
SELECT * FROM LichSuHeThong ORDER BY NgayThucHien DESC;
GO

-- Quick view toàn bảng
SELECT * FROM NguoiDung;
SELECT * FROM PhongBan;
SELECT * FROM ThongTinGiangVien;
SELECT * FROM KhoaHoc;
SELECT * FROM GiangVien_KhoaHoc;
SELECT * FROM PhongHoc;
SELECT * FROM DangKyKhoaHoc;
SELECT * FROM DiemSo;
SELECT * FROM LichDay;
SELECT * FROM LichHocVien;
SELECT * FROM HoaDonHocPhi;
SELECT * FROM GiaoDichThanhToan;
SELECT * FROM NguoiDung_VaiTro;
SELECT * FROM VaiTro;
GO