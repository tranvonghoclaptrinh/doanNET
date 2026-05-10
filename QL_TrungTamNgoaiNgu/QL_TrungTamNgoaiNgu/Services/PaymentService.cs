using QL_TrungTamNgoaiNgu.Models;
using System.Data.Entity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace QL_TrungTamNgoaiNgu.Services
{
    public sealed class PaymentService
    {
        private static readonly HashSet<string> AllowedPaymentMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Bank transfer",
            "Cash",
            "Card",
            "E-wallet"
        };

        public async Task<ThanhToanHocPhiResult> ThanhToanAsync(
            int maHoaDon,
            int soTien,
            string phuongThuc,
            string maChungTu,
            string ghiChu,
            int nguoiXacNhan)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                // Verify that the user exists to prevent foreign key violations
                var userExists = await db.NguoiDungs.AnyAsync(u => u.MaNguoiDung == nguoiXacNhan);
                if (!userExists)
                {
                    throw new InvalidOperationException($"Nguoi xac nhan (ID: {nguoiXacNhan}) khong ton tai trong he thong.");
                }

                var invoice = await db.HoaDonHocPhis
                    .Include(item => item.GiaoDichThanhToans)
                    .Include(item => item.DangKyKhoaHoc)
                    .FirstOrDefaultAsync(item => item.MaHoaDon == maHoaDon);

                if (invoice == null)
                {
                    throw new InvalidOperationException("Khong tim thay hoa don.");
                }

                if (soTien <= 0)
                {
                    throw new InvalidOperationException("So tien thanh toan phai lon hon 0.");
                }

                var paidBefore = invoice.GiaoDichThanhToans.Sum(item => item.SoTien);
                var remaining = invoice.TongTien - paidBefore;
                if (remaining <= 0)
                {
                    throw new InvalidOperationException("Hoa don nay da thanh toan xong.");
                }

                if (soTien > remaining)
                {
                    throw new InvalidOperationException($"So tien thanh toan vuot qua cong no con lai ({remaining:#,0} VND).");
                }

                var normalizedMethod = NormalizePaymentMethod(phuongThuc);
                var normalizedNote = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();
                if (!IsPrintableAscii(normalizedNote))
                {
                    throw new InvalidOperationException("Ghi chu chi duoc dung tieng Anh khong dau va ky tu ASCII.");
                }

                var now = DateTime.Now;
                var transaction = new GiaoDichThanhToan
                {
                    MaHoaDon = maHoaDon,
                    NgayGiaoDich = now,
                    SoTien = soTien,
                    PhuongThuc = normalizedMethod,
                    MaChungTu = string.IsNullOrWhiteSpace(maChungTu) ? Guid.NewGuid().ToString("N").Substring(0, 12) : maChungTu,
                    GhiChu = normalizedNote,
                    NguoiXacNhan = nguoiXacNhan
                };

                db.GiaoDichThanhToans.Add(transaction);

                var paid = paidBefore + soTien;
                invoice.TrangThai = paid >= invoice.TongTien
                    ? "Đã hoàn tất"
                    : paid > 0 ? "Thanh toán một phần" : "Chưa thanh toán";

                // Log transaction history
                db.LichSuHeThongs.Add(new LichSuHeThong
                {
                    TenBang = "GiaoDichThanhToan",
                    HanhDong = "INSERT",
                    MaNguoiDung = nguoiXacNhan,
                    NoiDung = $"Payment invoice {maHoaDon}: {soTien} VND ({normalizedMethod}). Status: {invoice.TrangThai}",
                    NgayThucHien = now
                });

                await db.SaveChangesAsync();

                return new ThanhToanHocPhiResult
                {
                    MaHoaDon = invoice.MaHoaDon,
                    TongTien = invoice.TongTien,
                    TrangThaiThanhToan = invoice.TrangThai
                };
            }
        }

        private static string NormalizePaymentMethod(string phuongThuc)
        {
            var value = string.IsNullOrWhiteSpace(phuongThuc) ? "Bank transfer" : phuongThuc.Trim();
            switch (value)
            {
                case "Chuyen khoan":
                case "Chuyển khoản":
                    value = "Bank transfer";
                    break;
                case "Tien mat":
                case "Tiền mặt":
                    value = "Cash";
                    break;
                case "The ngan hang":
                case "Thẻ ngân hàng":
                    value = "Card";
                    break;
                case "Vi dien tu":
                case "Ví điện tử":
                    value = "E-wallet";
                    break;
            }

            if (!AllowedPaymentMethods.Contains(value))
            {
                throw new InvalidOperationException("Phuong thuc thanh toan khong hop le.");
            }

            return value;
        }

        private static bool IsPrintableAscii(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            foreach (var ch in value)
            {
                if (ch < 32 || ch > 126)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
