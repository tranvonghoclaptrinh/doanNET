using QL_TrungTamNgoaiNgu.Models;
using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace QL_TrungTamNgoaiNgu.Services
{
    public sealed class KhoaHocRegistrationService : IKhoaHocRegistrationService
    {
        public async Task<DangKyKhoaHocResult> DangKyKhoaHocAsync(
            int maHocVien,
            int maKhoaHoc,
            DateTime hanThanhToan)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                var maHocVienParam = new SqlParameter("@MaHocVien", maHocVien);
                var maKhoaHocParam = new SqlParameter("@MaKhoaHoc", maKhoaHoc);
                var hanThanhToanParam = new SqlParameter("@HanThanhToan", hanThanhToan.Date);

                return await db.Database
                    .SqlQuery<DangKyKhoaHocResult>(
                        "EXEC dbo.sp_DangKyKhoaHoc @MaHocVien, @MaKhoaHoc, @HanThanhToan",
                        maHocVienParam,
                        maKhoaHocParam,
                        hanThanhToanParam)
                    .SingleAsync();
            }
        }
    }
}
