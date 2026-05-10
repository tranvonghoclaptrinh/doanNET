/* 
=======================================================================
TRANG: PAYMENT & CÔNG NỢ (ĐÃ SỬA LỖI VÀ CẬP NHẬT DỮ LIỆU LỚN)
Mô tả: File này cung cấp các kịch bản và logic nâng cao cho thanh toán và quản lý công nợ.
Đặc điểm: 
- Đã sửa lỗi khai báo biến sau lệnh GO.
- Không sửa cấu trúc bảng gốc.
- Có kiểm tra IF để chạy được nhiều lần.
- Bao gồm dữ liệu mẫu bổ sung với giá trị lớn và các kịch bản test.
- Tự động cập nhật trạng thái hóa đơn thông qua TRIGGER.
=======================================================================
*/

USE HeThongQuanLyTrungTamNgoaiNgu;
GO

PRINT N'--- BẮT ĐẦU CẬP NHẬT LOGIC PAYMENT & CÔNG NỢ (ĐÃ SỬA LỖI) ---';

-- Chuẩn hóa payment theo English/international để ứng dụng không bị lỗi CHECK constraint
-- và ghi chú thanh toán chỉ nhận printable ASCII (hoặc NULL/trống).
IF EXISTS (SELECT * FROM sys.triggers WHERE name = N'trg_GiaoDichThanhToan_AfterInsertUpdate')
    DROP TRIGGER trg_GiaoDichThanhToan_AfterInsertUpdate;
GO

DECLARE @DropPaymentMethodChecks NVARCHAR(MAX) = N'';
SELECT @DropPaymentMethodChecks = @DropPaymentMethodChecks
    + N'ALTER TABLE dbo.GiaoDichThanhToan DROP CONSTRAINT ' + QUOTENAME(name) + N';'
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.GiaoDichThanhToan')
  AND definition LIKE N'%PhuongThuc%';

IF LEN(@DropPaymentMethodChecks) > 0
    EXEC sp_executesql @DropPaymentMethodChecks;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_GiaoDichThanhToan_GhiChu_ASCII')
    ALTER TABLE dbo.GiaoDichThanhToan DROP CONSTRAINT CK_GiaoDichThanhToan_GhiChu_ASCII;

UPDATE dbo.GiaoDichThanhToan
SET PhuongThuc = CASE
    WHEN PhuongThuc IN (N'Chuyển khoản', N'Chuyen khoan', N'Bank transfer') THEN N'Bank transfer'
    WHEN PhuongThuc IN (N'Tiền mặt', N'Tien mat', N'Cash') THEN N'Cash'
    WHEN PhuongThuc IN (N'Thẻ ngân hàng', N'The ngan hang', N'Card') THEN N'Card'
    WHEN PhuongThuc IN (N'Ví điện tử', N'Vi dien tu', N'E-wallet') THEN N'E-wallet'
    ELSE N'Bank transfer'
END,
GhiChu = CASE
    WHEN GhiChu IS NULL OR GhiChu NOT LIKE N'%[^ -~]%' COLLATE Latin1_General_BIN2 THEN GhiChu
    ELSE NULL
END;

ALTER TABLE dbo.GiaoDichThanhToan WITH CHECK ADD CONSTRAINT CK_GiaoDichThanhToan_PhuongThuc_English
CHECK (PhuongThuc IN (N'Bank transfer', N'Cash', N'Card', N'E-wallet'));

ALTER TABLE dbo.GiaoDichThanhToan WITH CHECK ADD CONSTRAINT CK_GiaoDichThanhToan_GhiChu_ASCII
CHECK (GhiChu IS NULL OR GhiChu NOT LIKE N'%[^ -~]%' COLLATE Latin1_General_BIN2);

-- 1. THỦ TỤC CẬP NHẬT TRẠNG THÁI HÓA ĐƠN
-- Đảm bảo thủ tục này tồn tại và được cập nhật
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CapNhatTrangThaiHoaDon]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_CapNhatTrangThaiHoaDon];
GO

CREATE PROCEDURE [dbo].[sp_CapNhatTrangThaiHoaDon]
    @MaHoaDon INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TongTien INT;
    DECLARE @DaThanhToan INT;
    DECLARE @TrangThaiHienTai NVARCHAR(50);

    SELECT @TongTien = TongTien, @TrangThaiHienTai = TrangThai FROM HoaDonHocPhi WHERE MaHoaDon = @MaHoaDon;
    SELECT @DaThanhToan = ISNULL(SUM(SoTien), 0) FROM GiaoDichThanhToan WHERE MaHoaDon = @MaHoaDon;

    DECLARE @TrangThaiMoi NVARCHAR(50);

    IF @DaThanhToan >= @TongTien
        SET @TrangThaiMoi = N'Đã hoàn tất';
    ELSE IF @DaThanhToan > 0
        SET @TrangThaiMoi = N'Thanh toán một phần';
    ELSE
        SET @TrangThaiMoi = N'Chưa thanh toán';

    IF @TrangThaiHienTai <> @TrangThaiMoi
    BEGIN
        UPDATE HoaDonHocPhi SET TrangThai = @TrangThaiMoi WHERE MaHoaDon = @MaHoaDon;
        PRINT N'Cập nhật trạng thái hóa đơn ' + CAST(@MaHoaDon AS NVARCHAR(10)) + N' từ ' + @TrangThaiHienTai + N' sang ' + @TrangThaiMoi;
    END
END;
GO

-- 2. TRIGGER TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI HÓA ĐƠN
-- Trigger này sẽ tự động gọi sp_CapNhatTrangThaiHoaDon sau mỗi giao dịch thanh toán
IF EXISTS (SELECT * FROM sys.triggers WHERE name = N'trg_GiaoDichThanhToan_AfterInsertUpdate')
    DROP TRIGGER trg_GiaoDichThanhToan_AfterInsertUpdate;
GO

CREATE TRIGGER trg_GiaoDichThanhToan_AfterInsertUpdate
ON GiaoDichThanhToan
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MaHoaDon INT;
    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT MaHoaDon FROM inserted
        UNION
        SELECT DISTINCT MaHoaDon FROM deleted;

    OPEN cur;
    FETCH NEXT FROM cur INTO @MaHoaDon;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC sp_CapNhatTrangThaiHoaDon @MaHoaDon;
        FETCH NEXT FROM cur INTO @MaHoaDon;
    END

    CLOSE cur;
    DEALLOCATE cur;
END;
GO

-- 3. VIEW THEO DÕI CÔNG NỢ (CẬP NHẬT THÊM)
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[v_TheoDoiCongNo]'))
    DROP VIEW [dbo].[v_TheoDoiCongNo];
GO

CREATE VIEW [dbo].[v_TheoDoiCongNo] AS
SELECT 
    HD.MaHoaDon,
    ND.HoTen AS TenHocVien,
    ND.Email AS EmailHocVien,
    ND.SoDienThoai AS SdtHocVien,
    KH.TenKhoaHoc,
    HD.TongTien,
    ISNULL(SUM(GD.SoTien), 0) AS DaThanhToan,
    CASE
        WHEN HD.TongTien - ISNULL(SUM(GD.SoTien), 0) > 0 THEN HD.TongTien - ISNULL(SUM(GD.SoTien), 0)
        ELSE 0
    END AS ConNo,
    HD.NgayXuat AS NgayXuatHoaDon,
    HD.HanThanhToan,
    HD.TrangThai AS TrangThaiHoaDon,
    CASE 
        WHEN HD.TongTien - ISNULL(SUM(GD.SoTien), 0) > 0 AND HD.HanThanhToan < GETDATE() THEN N'Quá hạn'
        WHEN HD.TongTien - ISNULL(SUM(GD.SoTien), 0) > 0 THEN N'Trong hạn'
        ELSE N'Đã hoàn tất'
    END AS TinhTrangCongNo
FROM HoaDonHocPhi HD
JOIN DangKyKhoaHoc DK ON HD.MaDangKy = DK.MaDangKy
JOIN NguoiDung ND ON DK.MaHocVien = ND.MaNguoiDung
JOIN KhoaHoc KH ON DK.MaKhoaHoc = KH.MaKhoaHoc
LEFT JOIN GiaoDichThanhToan GD ON HD.MaHoaDon = GD.MaHoaDon
GROUP BY HD.MaHoaDon, ND.HoTen, ND.Email, ND.SoDienThoai, KH.TenKhoaHoc, HD.TongTien, HD.NgayXuat, HD.HanThanhToan, HD.TrangThai;
GO

-- 4. THÊM DỮ LIỆU MẪU BỔ SUNG VÀ KỊCH BẢN TEST (Đảm bảo biến được khai báo trong cùng một batch)

-- Thêm vai trò Kế toán nếu chưa có (ID 2)
IF NOT EXISTS (SELECT 1 FROM VaiTro WHERE MaVaiTro = 2)
BEGIN
    INSERT INTO VaiTro (MaVaiTro, TenVaiTro, MoTa) VALUES (2, N'Kế toán', N'Quản lý các giao dịch tài chính');
END

-- Thêm người dùng Kế toán nếu chưa có
DECLARE @MaKTC INT;
IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE Email = 'ketoan.chinh@example.com')
BEGIN
    INSERT INTO NguoiDung (HoTen, Email, SoDienThoai, MatKhau)
    VALUES (N'Kế Toán Chính', 'ketoan.chinh@example.com', '0901234567', N'KeToan$2024');
    SET @MaKTC = SCOPE_IDENTITY();
    INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro) VALUES (@MaKTC, 2);
END
ELSE
BEGIN
    SET @MaKTC = (SELECT MaNguoiDung FROM NguoiDung WHERE Email = 'ketoan.chinh@example.com');
END

-- Thêm học viên mới để test từ đầu với nhiều đăng ký khóa học và hóa đơn lớn
DECLARE @MaHocVienTest INT;
IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE Email = 'hocvien.vip@example.com')
BEGIN
    INSERT INTO NguoiDung (HoTen, Email, SoDienThoai, MatKhau)
    VALUES (N'Học Viên VIP', 'hocvien.vip@example.com', '0912345678', N'HocVienVIP@123');
    SET @MaHocVienTest = SCOPE_IDENTITY();
    INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro) VALUES (@MaHocVienTest, 3); -- Vai trò Học viên
END
ELSE
BEGIN
    SET @MaHocVienTest = (SELECT MaNguoiDung FROM NguoiDung WHERE Email = 'hocvien.vip@example.com');
END

-- Thêm các đăng ký khóa học và hóa đơn cho Học Viên VIP
IF @MaHocVienTest IS NOT NULL
BEGIN
    -- Đăng ký khóa học 1 (Học phí 10.000.000)
    IF NOT EXISTS (SELECT 1 FROM DangKyKhoaHoc WHERE MaHocVien = @MaHocVienTest AND MaKhoaHoc = 1)
    BEGIN
        INSERT INTO DangKyKhoaHoc (MaHocVien, MaKhoaHoc, NgayDangKy, HocPhiThoiDiem, TrangThai)
        VALUES (@MaHocVienTest, 1, GETDATE(), 10000000, N'Đang học');
        DECLARE @MaDangKy1 INT = SCOPE_IDENTITY();
        INSERT INTO HoaDonHocPhi (MaDangKy, TongTien, NgayXuat, HanThanhToan, TrangThai)
        VALUES (@MaDangKy1, 10000000, GETDATE(), DATEADD(day, 30, GETDATE()), N'Chưa thanh toán');
    END

    -- Đăng ký khóa học 2 (Học phí 15.000.000)
    IF NOT EXISTS (SELECT 1 FROM DangKyKhoaHoc WHERE MaHocVien = @MaHocVienTest AND MaKhoaHoc = 2)
    BEGIN
        INSERT INTO DangKyKhoaHoc (MaHocVien, MaKhoaHoc, NgayDangKy, HocPhiThoiDiem, TrangThai)
        VALUES (@MaHocVienTest, 2, GETDATE(), 15000000, N'Đang học');
        DECLARE @MaDangKy2 INT = SCOPE_IDENTITY();
        INSERT INTO HoaDonHocPhi (MaDangKy, TongTien, NgayXuat, HanThanhToan, TrangThai)
        VALUES (@MaDangKy2, 15000000, GETDATE(), DATEADD(day, 45, GETDATE()), N'Chưa thanh toán');
    END

    -- Đăng ký khóa học 3 (Học phí 20.000.000)
    IF NOT EXISTS (SELECT 1 FROM DangKyKhoaHoc WHERE MaHocVien = @MaHocVienTest AND MaKhoaHoc = 3)
    BEGIN
        INSERT INTO DangKyKhoaHoc (MaHocVien, MaKhoaHoc, NgayDangKy, HocPhiThoiDiem, TrangThai)
        VALUES (@MaHocVienTest, 3, GETDATE(), 20000000, N'Đang học');
        DECLARE @MaDangKy3 INT = SCOPE_IDENTITY();
        INSERT INTO HoaDonHocPhi (MaDangKy, TongTien, NgayXuat, HanThanhToan, TrangThai)
        VALUES (@MaDangKy3, 20000000, GETDATE(), DATEADD(day, 60, GETDATE()), N'Chưa thanh toán');
    END
END

PRINT N'--- THÊM DỮ LIỆU HỌC VIÊN, ĐĂNG KÝ VÀ THANH TOÁN LỚN (MỞ RỘNG) ---';

DECLARE @i INT = 1;
WHILE @i <= 10 -- Thêm 10 học viên mới
BEGIN
    DECLARE @HoTenHV NVARCHAR(100) = N'Học Viên Test ' + CAST(@i AS NVARCHAR(2));
    DECLARE @EmailHV VARCHAR(100) = 'hocvien.test' + CAST(@i AS NVARCHAR(2)) + '@example.com';
    DECLARE @SdtHV VARCHAR(15) = '09' + RIGHT('00000000' + CAST(@i * 1000000 AS NVARCHAR(10)), 8);
    DECLARE @MatKhauHV NVARCHAR(100) = N'HVTest@' + CAST(@i AS NVARCHAR(2));
    DECLARE @MaHocVienMoi INT;

    IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE Email = @EmailHV)
    BEGIN
        INSERT INTO NguoiDung (HoTen, Email, SoDienThoai, MatKhau)
        VALUES (@HoTenHV, @EmailHV, @SdtHV, @MatKhauHV);
        SET @MaHocVienMoi = SCOPE_IDENTITY();
        INSERT INTO NguoiDung_VaiTro (MaNguoiDung, MaVaiTro) VALUES (@MaHocVienMoi, 3);
    END
    ELSE
    BEGIN
        SET @MaHocVienMoi = (SELECT MaNguoiDung FROM NguoiDung WHERE Email = @EmailHV);
    END

    -- Đăng ký 2 khóa học cho mỗi học viên mới
    DECLARE @j INT = 1;
    WHILE @j <= 2
    BEGIN
        DECLARE @MaKhoaHoc INT = (@i + @j) % 20 + 1; -- Chọn ngẫu nhiên khóa học từ 1-20
        DECLARE @HocPhi INT = 10000000 + (@i * 1000000) + (@j * 500000); -- Học phí lớn
        DECLARE @NgayDangKy DATE = DATEADD(day, -(@i * 7 + @j * 3), GETDATE());
        DECLARE @HanThanhToan DATE = DATEADD(day, 30, @NgayDangKy);
        DECLARE @MaDangKyMoi INT;

        IF NOT EXISTS (SELECT 1 FROM DangKyKhoaHoc WHERE MaHocVien = @MaHocVienMoi AND MaKhoaHoc = @MaKhoaHoc)
        BEGIN
            INSERT INTO DangKyKhoaHoc (MaHocVien, MaKhoaHoc, NgayDangKy, HocPhiThoiDiem, TrangThai)
            VALUES (@MaHocVienMoi, @MaKhoaHoc, @NgayDangKy, @HocPhi, N'Đang học');
            SET @MaDangKyMoi = SCOPE_IDENTITY();

            INSERT INTO HoaDonHocPhi (MaDangKy, TongTien, NgayXuat, HanThanhToan, TrangThai)
            VALUES (@MaDangKyMoi, @HocPhi, @NgayDangKy, @HanThanhToan, N'Chưa thanh toán');
            DECLARE @MaHoaDonMoi INT = SCOPE_IDENTITY();

            -- Thêm giao dịch thanh toán cho hóa đơn này (thanh toán một phần hoặc toàn bộ)
            DECLARE @SoTienThanhToan INT = @HocPhi * (0.5 + RAND() * 0.5); -- Thanh toán từ 50% đến 100%
            DECLARE @MaChungTuGD VARCHAR(100) = 'GD_HV' + CAST(@MaHocVienMoi AS NVARCHAR(5)) + '_KH' + CAST(@MaKhoaHoc AS NVARCHAR(5)) + '_P1';

            IF NOT EXISTS (SELECT 1 FROM GiaoDichThanhToan WHERE MaHoaDon = @MaHoaDonMoi AND MaChungTu = @MaChungTuGD)
            BEGIN
                INSERT INTO GiaoDichThanhToan (MaHoaDon, NgayGiaoDich, SoTien, PhuongThuc, MaChungTu, GhiChu, NguoiXacNhan)
                VALUES (@MaHoaDonMoi, DATEADD(day, 5, @NgayDangKy), @SoTienThanhToan, N'Bank transfer', @MaChungTuGD, N'First installment', @MaKTC);
            END
        END
        SET @j = @j + 1;
    END
    SET @i = @i + 1;
END;

-- Cập nhật các hóa đơn hiện có trong database gốc để tăng số tiền đã thanh toán
-- Lấy các hóa đơn có trạng thái 'Chưa thanh toán' hoặc 'Thanh toán một phần' từ dữ liệu gốc
DECLARE @CursorHoaDon CURSOR;
DECLARE @MaHoaDonHienCo INT;
DECLARE @TongTienHD INT;
DECLARE @DaThanhToanHD INT;

SET @CursorHoaDon = CURSOR FOR
SELECT HD.MaHoaDon, HD.TongTien, ISNULL(SUM(GD.SoTien), 0)
FROM HoaDonHocPhi HD
LEFT JOIN GiaoDichThanhToan GD ON HD.MaHoaDon = GD.MaHoaDon
WHERE HD.TrangThai IN (N'Chưa thanh toán', N'Thanh toán một phần')
GROUP BY HD.MaHoaDon, HD.TongTien;

OPEN @CursorHoaDon;
FETCH NEXT FROM @CursorHoaDon INTO @MaHoaDonHienCo, @TongTienHD, @DaThanhToanHD;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @ConNoHienCo INT = @TongTienHD - @DaThanhToanHD;
    IF @ConNoHienCo > 0
    BEGIN
        DECLARE @SoTienBoSung INT = @ConNoHienCo * (0.7 + RAND() * 0.3); -- Thanh toán thêm 70-100% số còn nợ
        IF @SoTienBoSung > 0
        BEGIN
            DECLARE @MaChungTuBoSung VARCHAR(100) = 'GD_BOSUNG_' + CAST(@MaHoaDonHienCo AS NVARCHAR(10)) + '_' + FORMAT(GETDATE(), 'yyyyMMddHHmmss');
            IF NOT EXISTS (SELECT 1 FROM GiaoDichThanhToan WHERE MaHoaDon = @MaHoaDonHienCo AND MaChungTu = @MaChungTuBoSung)
            BEGIN
                INSERT INTO GiaoDichThanhToan (MaHoaDon, NgayGiaoDich, SoTien, PhuongThuc, MaChungTu, GhiChu, NguoiXacNhan)
                VALUES (@MaHoaDonHienCo, GETDATE(), @SoTienBoSung, N'Bank transfer', @MaChungTuBoSung, N'Additional payment', @MaKTC);
            END
        END
    END
    FETCH NEXT FROM @CursorHoaDon INTO @MaHoaDonHienCo, @TongTienHD, @DaThanhToanHD;
END;
CLOSE @CursorHoaDon;
DEALLOCATE @CursorHoaDon;

PRINT N'--- KỊCH BẢN TEST VỚI DỮ LIỆU LỚN VÀ CÁC TRƯỜNG HỢP THANH TOÁN ---';

-- Lấy lại các biến cần thiết sau khi đã có GO (đảm bảo chúng có giá trị trong batch này)
SET @MaKTC = (SELECT MaNguoiDung FROM NguoiDung WHERE Email = 'ketoan.chinh@example.com');
IF @MaKTC IS NULL SET @MaKTC = 1; -- Fallback nếu không tìm thấy kế toán chính

SET @MaHocVienTest = (SELECT MaNguoiDung FROM NguoiDung WHERE Email = 'hocvien.vip@example.com');

PRINT N'--- TEST CASE 1: THANH TOÁN MỘT PHẦN CHO HÓA ĐƠN LỚN ---';
DECLARE @MaHD_VIP1 INT = (SELECT MaHoaDon FROM HoaDonHocPhi HD JOIN DangKyKhoaHoc DK ON HD.MaDangKy = DK.MaDangKy WHERE DK.MaHocVien = @MaHocVienTest AND DK.MaKhoaHoc = 1);
IF @MaHD_VIP1 IS NOT NULL
BEGIN
    DECLARE @ConNo_VIP1_Test1 INT;
    SELECT @ConNo_VIP1_Test1 = ConNo FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP1;

    IF NOT EXISTS (SELECT 1 FROM GiaoDichThanhToan WHERE MaHoaDon = @MaHD_VIP1 AND MaChungTu = 'VIP_CK_001')
       AND @ConNo_VIP1_Test1 > 0
    BEGIN
        DECLARE @SoTien_VIP1_Test1 INT = CASE WHEN @ConNo_VIP1_Test1 < 3000000 THEN @ConNo_VIP1_Test1 ELSE 3000000 END;
        INSERT INTO GiaoDichThanhToan (MaHoaDon, NgayGiaoDich, SoTien, PhuongThuc, MaChungTu, GhiChu, NguoiXacNhan)
        VALUES (@MaHD_VIP1, GETDATE(), @SoTien_VIP1_Test1, N'Bank transfer', 'VIP_CK_001', N'IELTS first installment', @MaKTC);
    END
    SELECT * FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP1;
END

PRINT N'--- TEST CASE 2: THANH TOÁN TIẾP MỘT PHẦN KHÁC ---';
IF @MaHD_VIP1 IS NOT NULL
BEGIN
    DECLARE @ConNo_VIP1_Test2 INT;
    SELECT @ConNo_VIP1_Test2 = ConNo FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP1;

    IF NOT EXISTS (SELECT 1 FROM GiaoDichThanhToan WHERE MaHoaDon = @MaHD_VIP1 AND MaChungTu = 'VIP_CK_002')
       AND @ConNo_VIP1_Test2 > 0
    BEGIN
        DECLARE @SoTien_VIP1_Test2 INT = CASE WHEN @ConNo_VIP1_Test2 < 2000000 THEN @ConNo_VIP1_Test2 ELSE 2000000 END;
        INSERT INTO GiaoDichThanhToan (MaHoaDon, NgayGiaoDich, SoTien, PhuongThuc, MaChungTu, GhiChu, NguoiXacNhan)
        VALUES (@MaHD_VIP1, DATEADD(day, 5, GETDATE()), @SoTien_VIP1_Test2, N'Cash', 'VIP_CK_002', N'IELTS second installment', @MaKTC);
    END
    SELECT * FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP1;
END

PRINT N'--- TEST CASE 3: THANH TOÁN HOÀN TẤT HÓA ĐƠN ---';
IF @MaHD_VIP1 IS NOT NULL
BEGIN
    DECLARE @ConNo_VIP1 INT;
    SELECT @ConNo_VIP1 = ConNo FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP1;
    
    -- Thanh toán nốt số tiền còn lại
    IF @ConNo_VIP1 > 0 AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToan WHERE MaHoaDon = @MaHD_VIP1 AND MaChungTu = 'VIP_CK_003')
    BEGIN
        INSERT INTO GiaoDichThanhToan (MaHoaDon, NgayGiaoDich, SoTien, PhuongThuc, MaChungTu, GhiChu, NguoiXacNhan)
        VALUES (@MaHD_VIP1, DATEADD(day, 10, GETDATE()), @ConNo_VIP1, N'Card', 'VIP_CK_003', N'IELTS final payment', @MaKTC);
    END
    SELECT * FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP1;
END

PRINT N'--- TEST CASE 4: HÓA ĐƠN KHÁC THANH TOÁN MỘT LẦN ---';
DECLARE @MaHD_VIP2 INT = (SELECT MaHoaDon FROM HoaDonHocPhi HD JOIN DangKyKhoaHoc DK ON HD.MaDangKy = DK.MaDangKy WHERE DK.MaHocVien = @MaHocVienTest AND DK.MaKhoaHoc = 2);
IF @MaHD_VIP2 IS NOT NULL
BEGIN
    DECLARE @ConNo_VIP2 INT;
    SELECT @ConNo_VIP2 = ConNo FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP2;

    IF NOT EXISTS (SELECT 1 FROM GiaoDichThanhToan WHERE MaHoaDon = @MaHD_VIP2 AND MaChungTu = 'VIP_CK_004')
       AND @ConNo_VIP2 > 0
    BEGIN
        INSERT INTO GiaoDichThanhToan (MaHoaDon, NgayGiaoDich, SoTien, PhuongThuc, MaChungTu, GhiChu, NguoiXacNhan)
        VALUES (@MaHD_VIP2, GETDATE(), @ConNo_VIP2, N'Bank transfer', 'VIP_CK_004', N'TOEIC full payment', @MaKTC);
    END
    SELECT * FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP2;
END

PRINT N'--- TEST CASE 5: HÓA ĐƠN CHƯA THANH TOÁN VÀ QUÁ HẠN ---';
DECLARE @MaHD_VIP3 INT = (SELECT MaHoaDon FROM HoaDonHocPhi HD JOIN DangKyKhoaHoc DK ON HD.MaDangKy = DK.MaDangKy WHERE DK.MaHocVien = @MaHocVienTest AND DK.MaKhoaHoc = 3);
IF @MaHD_VIP3 IS NOT NULL
BEGIN
    -- Cập nhật HanThanhToan về quá khứ để test trạng thái quá hạn
    UPDATE HoaDonHocPhi SET HanThanhToan = DATEADD(day, -10, GETDATE()) WHERE MaHoaDon = @MaHD_VIP3;
    SELECT * FROM v_TheoDoiCongNo WHERE MaHoaDon = @MaHD_VIP3;
END

PRINT N'--- TEST CASE 6: KIỂM TRA TỔNG HỢP CÔNG NỢ CỦA TẤT CẢ HỌC VIÊN ---';
SELECT * FROM v_TheoDoiCongNo;

PRINT N'--- HOÀN TẤT CẬP NHẬT LOGIC PAYMENT & CÔNG NỢ ---';
