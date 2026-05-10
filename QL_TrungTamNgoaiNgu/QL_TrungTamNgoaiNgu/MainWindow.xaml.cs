using QL_TrungTamNgoaiNgu.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QL_TrungTamNgoaiNgu.Models;

namespace QL_TrungTamNgoaiNgu
{
    public partial class MainWindow : Window
    {
        public MainWindow()
            : this(new Services.UserSession(0, "Admin", "admin@local", string.Empty, new[] { "Admin" }))
        {
        }

        public MainWindow(Services.UserSession session)
        {
            InitializeComponent();

            var viewModel = new MainViewModel(session);
            DataContext = viewModel;
            Loaded += async (_, __) =>
            {
                try
                {
                    await viewModel.InitializeAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Loi tai trang", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
        }

        private void DataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (!IsScalarType(e.PropertyType))
            {
                e.Cancel = true;
                return;
            }

            if (DataContext is MainViewModel viewModel
                && !viewModel.Session.IsAdmin
                && IsRestrictedColumn(e.PropertyName))
            {
                e.Cancel = true;
                return;
            }

            e.Column.Header = SplitPascalCase(e.PropertyName);
        }

        private async void MainDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var viewModel = (MainViewModel)DataContext;
                if (!viewModel.Session.IsAdmin
                    || viewModel.SelectedTable?.Key != "YeuCauSuaDiem"
                    || !(viewModel.SelectedRow is GradeChangeRequestRow request))
                {
                    return;
                }

                var message = $"Yeu cau sua diem #{request.MaYeuCau}\n"
                              + $"Giang vien: {request.TenGiangVien}\n"
                              + $"Diem cu: {request.DiemCu?.ToString() ?? "(trong)"} -> Diem moi: {request.DiemMoi?.ToString() ?? "(trong)"}\n\n"
                              + "Chon Yes de xac nhan cap nhat diem, No de tu choi.";
                var result = MessageBox.Show(message, "Duyet yeu cau sua diem", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await viewModel.DecideGradeChangeRequestAsync(request, approve: true);
                }
                else if (result == MessageBoxResult.No)
                {
                    await viewModel.DecideGradeChangeRequestAsync(request, approve: false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static bool IsScalarType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(decimal)
                   || type == typeof(DateTime)
                   || type == typeof(Guid);
        }

        private static string SplitPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return System.Text.RegularExpressions.Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
        }

        private static bool IsRestrictedColumn(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            return propertyName.StartsWith("Ma", StringComparison.OrdinalIgnoreCase)
                   || propertyName.IndexOf("MatKhau", StringComparison.OrdinalIgnoreCase) >= 0
                   || propertyName.IndexOf("MuoiMatKhau", StringComparison.OrdinalIgnoreCase) >= 0
                   || propertyName.IndexOf("OTP", StringComparison.OrdinalIgnoreCase) >= 0
                   || propertyName.IndexOf("ThoiGianOTP", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async void CreateButton_OnClick(object sender, RoutedEventArgs e)
        {
            await OpenEntityFormAsync(isEdit: false);
        }

        private async void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            await OpenEntityFormAsync(isEdit: true);
        }

        private async System.Threading.Tasks.Task OpenEntityFormAsync(bool isEdit)
        {
            try
            {
                var viewModel = (MainViewModel)DataContext;
                if (isEdit && viewModel.SelectedRow == null)
                {
                    MessageBox.Show("Hay chon mot dong de sua.");
                    return;
                }

                if (viewModel.Session.IsTeacher && viewModel.SelectedTable?.Key == "vw_BangDiem")
                {
                    if (!isEdit || !(viewModel.SelectedRow is BangDiemViewRow gradeRow))
                    {
                        MessageBox.Show("Hay chon mot dong bang diem de gui yeu cau sua.");
                        return;
                    }

                    var gradeFields = BuildTeacherGradeRequestFields(gradeRow);
                    var gradeDialog = new EntityFormWindow("Gui yeu cau sua diem", gradeFields)
                    {
                        Owner = this
                    };

                    if (gradeDialog.ShowDialog() == true)
                    {
                        var values = gradeDialog.Values;
                        values["MaDiem"] = gradeRow.MaDiem.ToString();
                        await viewModel.RequestTeacherGradeChangeFromBangDiemAsync(values);
                    }

                    return;
                }

                var source = isEdit ? viewModel.SelectedRow : null;
                var fields = BuildFields(viewModel.CurrentEntityType ?? source?.GetType(), source, isEdit);
                var dialog = new EntityFormWindow(isEdit ? "Sua du lieu" : "Them du lieu", fields)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    await viewModel.SaveEntityAsync(dialog.Values, isEdit);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Xoa cac dong da chon?", "Xac nhan", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }

                await ((MainViewModel)DataContext).DeleteEntitiesAsync(MainDataGrid.SelectedItems);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void PaymentButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var viewModel = (MainViewModel)DataContext;
                var dialog = CreatePaymentWindow(viewModel);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    await viewModel.PayAsync(dialog.MaHoaDon, dialog.SoTien, dialog.PhuongThuc, dialog.MaChungTu, dialog.GhiChu);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static PaymentWindow CreatePaymentWindow(MainViewModel viewModel)
        {
            if (viewModel.Session.IsStudent && viewModel.SelectedTable?.Key == "vw_CongNoHocPhi")
            {
                if (!(viewModel.SelectedRow is CongNoHocPhiRow debtRow))
                {
                    throw new InvalidOperationException("Hay chon mot dong cong no can thanh toan.");
                }

                if (debtRow.ConNo <= 0)
                {
                    throw new InvalidOperationException("Hoa don nay khong con cong no can thanh toan.");
                }

                var info = $"Hoc vien: {debtRow.TenHocVien}\n"
                           + $"Khoa hoc: {debtRow.TenKhoaHoc}\n"
                           + $"Tong tien: {debtRow.TongTien:#,0}\n"
                           + $"Da thanh toan: {debtRow.DaThanhToan:#,0}\n"
                           + $"Con no: {debtRow.ConNo:#,0}\n"
                           + $"Han thanh toan: {debtRow.HanThanhToan?.ToString("dd/MM/yyyy") ?? "Chua co"}\n"
                           + $"Trang thai: {debtRow.TrangThai}";

                return new PaymentWindow(debtRow.MaHoaDon, debtRow.ConNo, info);
            }

            if (viewModel.Session.IsStudent && viewModel.SelectedTable?.Key == "HoaDonHocPhi")
            {
                if (!(viewModel.SelectedRow is HoaDonHocPhi invoiceRow))
                {
                    throw new InvalidOperationException("Hay chon mot hoa don can thanh toan.");
                }

                var paid = GetPaidAmount(invoiceRow.MaHoaDon);
                var remaining = Math.Max(invoiceRow.TongTien - paid, 0);
                if (remaining <= 0)
                {
                    throw new InvalidOperationException("Hoa don nay khong con cong no can thanh toan.");
                }

                var info = $"Tong tien: {invoiceRow.TongTien:#,0}\n"
                           + $"Da thanh toan: {paid:#,0}\n"
                           + $"Con no: {remaining:#,0}\n"
                           + $"Ngay xuat: {invoiceRow.NgayXuat?.ToString("dd/MM/yyyy") ?? "Chua co"}\n"
                           + $"Han thanh toan: {invoiceRow.HanThanhToan?.ToString("dd/MM/yyyy") ?? "Chua co"}\n"
                           + $"Trang thai: {invoiceRow.TrangThai}";

                return new PaymentWindow(invoiceRow.MaHoaDon, remaining, info);
            }

            return new PaymentWindow();
        }

        private static int GetPaidAmount(int maHoaDon)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                return db.GiaoDichThanhToans
                    .Where(item => item.MaHoaDon == maHoaDon)
                    .Select(item => (int?)item.SoTien)
                    .Sum() ?? 0;
            }
        }

        private void LogoutButton_OnClick(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private static IEnumerable<FormField> BuildFields(Type entityType, object source, bool isEdit)
        {
            var identityNames = new HashSet<string>
            {
                "MaTaiKhoan", "MaNguoiDung", "MaKhoaHoc", "MaVaiTro", "MaDangKy", "MaHoaDon", "MaGiaoDich",
                "MaPhong", "MaPhongBan", "MaLich", "MaLichHocVien", "MaDiem", "MaYeuCau"
            };

            if (entityType == null)
            {
                return Enumerable.Empty<FormField>();
            }

            return entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanWrite && IsScalarType(property.PropertyType))
                .Where(property => isEdit || !identityNames.Contains(property.Name))
                .Select(property =>
                {
                    var value = source == null ? GetDefaultValueText(property.PropertyType) : Convert.ToString(property.GetValue(source));
                    return new FormField(property.Name, value, isEdit && identityNames.Contains(property.Name));
                })
                .ToList();
        }

        private static IEnumerable<FormField> BuildTeacherGradeRequestFields(BangDiemViewRow source)
        {
            return new[]
            {
                new FormField("Diem", source.Diem?.ToString() ?? string.Empty, false),
                new FormField("NhanXet", source.NhanXet ?? string.Empty, false)
            };
        }

        private static string GetDefaultValueText(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(bool)) return "True";
            if (type == typeof(DateTime)) return DateTime.Now.ToString("yyyy-MM-dd");
            if (type == typeof(int)) return "0";
            return string.Empty;
        }
    }
}
