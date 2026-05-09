using System;

namespace QL_TrungTamNgoaiNgu.Models
{
    public sealed class CongNoHocPhiRow
    {
        public int MaHoaDon { get; set; }
        public int MaHocVien { get; set; }
        public string TenHocVien { get; set; }
        public string EmailHocVien { get; set; }
        public int MaKhoaHoc { get; set; }
        public string TenKhoaHoc { get; set; }
        public int TongTien { get; set; }
        public int DaThanhToan { get; set; }
        public int ConNo { get; set; }
        public DateTime? HanThanhToan { get; set; }
        public string TrangThai { get; set; }
        public int QuaHan { get; set; }
    }

    public sealed class DoanhThuTheoThangRow
    {
        public int Nam { get; set; }
        public int Thang { get; set; }
        public int SoGiaoDich { get; set; }
        public int TongDoanhThu { get; set; }
        public int SoHoaDon { get; set; }
    }

    public sealed class LichDayViewRow
    {
        public int MaLich { get; set; }
        public DateTime NgayDay { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
        public string TenKhoaHoc { get; set; }
        public string TenGiangVien { get; set; }
        public string TenPhong { get; set; }
        public int SucChua { get; set; }
        public string TrangThai { get; set; }
        public string GhiChu { get; set; }
    }

    public sealed class BangDiemViewRow
    {
        public int MaDiem { get; set; }
        public int MaDangKy { get; set; }
        public string TenHocVien { get; set; }
        public string TenKhoaHoc { get; set; }
        public string TenGiangVienCham { get; set; }
        public string LoaiKiemTra { get; set; }
        public decimal? Diem { get; set; }
        public DateTime? NgayKiemTra { get; set; }
        public string NhanXet { get; set; }
        public string XepLoai { get; set; }
    }

    public sealed class StudentTodayScheduleRow
    {
        public DateTime NgayDay { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
        public string TenKhoaHoc { get; set; }
        public string TenPhong { get; set; }
        public string TenGiangVien { get; set; }
        public string DiemDanh { get; set; }
    }

    public sealed class StudentCourseSummaryRow
    {
        public string TenKhoaHoc { get; set; }
        public string TrangThai { get; set; }
        public int HocPhiThoiDiem { get; set; }
        public DateTime? NgayDangKy { get; set; }
    }

    public sealed class GradeChangeRequestRow
    {
        public int MaYeuCau { get; set; }
        public int MaDiem { get; set; }
        public int MaGiangVien { get; set; }
        public string TenGiangVien { get; set; }
        public decimal? DiemCu { get; set; }
        public decimal? DiemMoi { get; set; }
        public string NhanXetMoi { get; set; }
        public string LyDo { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayYeuCau { get; set; }
    }

    public sealed class DiemDanhViewRow
    {
        public int MaLichHocVien { get; set; }
        public DateTime NgayDay { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public string TenKhoaHoc { get; set; }
        public string TenHocVien { get; set; }
        public string TenGiangVien { get; set; }
        public string TenPhong { get; set; }
        public string DiemDanh { get; set; }
        public string GhiChu { get; set; }
    }
}
