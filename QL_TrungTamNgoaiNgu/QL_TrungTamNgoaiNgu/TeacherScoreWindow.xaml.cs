using QL_TrungTamNgoaiNgu.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QL_TrungTamNgoaiNgu
{
    public partial class TeacherScoreWindow : Window
    {
        private readonly int _maGiangVien;

        public TeacherScoreWindow(int maGiangVien)
        {
            InitializeComponent();
            _maGiangVien = maGiangVien;
            MaGiangVienTextBox.Text = maGiangVien.ToString(CultureInfo.InvariantCulture);
            Loaded += async (_, __) => await LoadStudentsAsync();
        }

        public Dictionary<string, string> Values { get; private set; }

        private async System.Threading.Tasks.Task LoadStudentsAsync()
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                var students = await db.Database.SqlQuery<StudentChoice>(
                    @"SELECT DISTINCT hv.MaNguoiDung AS MaHocVien, hv.HoTen AS TenHocVien
                      FROM DangKyKhoaHoc dk
                      JOIN NguoiDung hv ON dk.MaHocVien = hv.MaNguoiDung
                      JOIN KhoaHoc kh ON dk.MaKhoaHoc = kh.MaKhoaHoc
                      JOIN GiangVien_KhoaHoc khgv ON kh.MaKhoaHoc = khgv.MaKhoaHoc
                      WHERE khgv.MaGiangVien = @p0
                      ORDER BY hv.HoTen",
                    _maGiangVien).ToListAsync();

                HocVienComboBox.ItemsSource = students;
                HocVienComboBox.SelectedIndex = students.Count > 0 ? 0 : -1;
            }
        }

        private async void HocVienComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(HocVienComboBox.SelectedItem is StudentChoice student))
            {
                MaHocVienTextBox.Text = string.Empty;
                KhoaHocComboBox.ItemsSource = null;
                return;
            }

            MaHocVienTextBox.Text = student.MaHocVien.ToString(CultureInfo.InvariantCulture);

            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                var courses = await db.Database.SqlQuery<CourseChoice>(
                    @"SELECT dk.MaDangKy, dk.MaKhoaHoc, kh.TenKhoaHoc
                      FROM DangKyKhoaHoc dk
                      JOIN KhoaHoc kh ON dk.MaKhoaHoc = kh.MaKhoaHoc
                      JOIN GiangVien_KhoaHoc khgv ON kh.MaKhoaHoc = khgv.MaKhoaHoc
                      WHERE dk.MaHocVien = @p0 AND khgv.MaGiangVien = @p1
                      ORDER BY kh.TenKhoaHoc",
                    student.MaHocVien,
                    _maGiangVien).ToListAsync();

                KhoaHocComboBox.ItemsSource = courses;
                KhoaHocComboBox.SelectedIndex = courses.Count > 0 ? 0 : -1;
            }
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(HocVienComboBox.SelectedItem is StudentChoice))
            {
                MessageBox.Show("Hay chon hoc vien.", "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!(KhoaHocComboBox.SelectedItem is CourseChoice course))
            {
                MessageBox.Show("Hay chon khoa hoc cua hoc vien.", "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(DiemTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Diem khong hop le. Hay nhap so theo dinh dang 8.5.", "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Values = new Dictionary<string, string>
            {
                ["MaDangKy"] = course.MaDangKy.ToString(CultureInfo.InvariantCulture),
                ["MaGiangVien"] = _maGiangVien.ToString(CultureInfo.InvariantCulture),
                ["LoaiKiemTra"] = string.IsNullOrWhiteSpace(LoaiKiemTraTextBox.Text) ? "Kiem tra" : LoaiKiemTraTextBox.Text,
                ["Diem"] = DiemTextBox.Text,
                ["NgayKiemTra"] = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["NhanXet"] = NhanXetTextBox.Text
            };

            DialogResult = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private sealed class StudentChoice
        {
            public int MaHocVien { get; set; }
            public string TenHocVien { get; set; }
        }

        private sealed class CourseChoice
        {
            public int MaDangKy { get; set; }
            public int MaKhoaHoc { get; set; }
            public string TenKhoaHoc { get; set; }
        }
    }
}
