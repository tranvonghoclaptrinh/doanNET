using QL_TrungTamNgoaiNgu.Models;
using System;
using System.Threading.Tasks;

namespace QL_TrungTamNgoaiNgu.Services
{
    public interface IKhoaHocRegistrationService
    {
        Task<DangKyKhoaHocResult> DangKyKhoaHocAsync(int maHocVien, int maKhoaHoc, DateTime hanThanhToan);
    }
}
