using QL_TrungTamNgoaiNgu.Models;
using System.Data.Entity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QL_TrungTamNgoaiNgu.Services
{
    public sealed class PaymentService
    {
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

                var now = DateTime.Now;
                var transaction = new GiaoDichThanhToan
                {
                    MaHoaDon = maHoaDon,
                    NgayGiaoDich = now,
                    SoTien = soTien,
                    PhuongThuc = phuongThuc ?? "Chuyen khoan",
                    MaChungTu = string.IsNullOrWhiteSpace(maChungTu) ? Guid.NewGuid().ToString("N").Substring(0, 12) : maChungTu,
                    GhiChu = ghiChu,
                    NguoiXacNhan = nguoiXacNhan
                };

                db.GiaoDichThanhToans.Add(transaction);

                var paid = invoice.GiaoDichThanhToans.Sum(item => item.SoTien) + soTien;
                invoice.TrangThai = paid >= invoice.TongTien
                    ? "Đã hoàn tất"
                    : paid > 0 ? "Thanh toán một phần" : "Chưa thanh toán";

                // Log transaction history
                db.LichSuHeThongs.Add(new LichSuHeThong
                {
                    TenBang = "GiaoDichThanhToan",
                    HanhDong = "INSERT",
                    MaNguoiDung = nguoiXacNhan,
                    NoiDung = $"Thanh toan hoa don {maHoaDon}: {soTien} VND ({phuongThuc}). Trang thai: {invoice.TrangThai}",
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
    }
}
