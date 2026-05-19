using QL_TrungTamNgoaiNgu.Models;
using QL_TrungTamNgoaiNgu.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace QL_TrungTamNgoaiNgu.ViewModels
{
    public sealed class MainViewModel : BaseViewModel
    {
        private readonly ICsvExportService _csvExportService;
        private readonly UserSession _session;
        private TableMenuItem _selectedTable;
        private ObservableCollection<object> _rows = new ObservableCollection<object>();
        private ICollectionView _rowsView;
        private object _selectedRow;
        private string _searchText;
        private decimal _totalRevenue;
        private int _unpaidInvoiceCount;
        private int _activeStudentCount;

        public MainViewModel()
            : this(new CsvExportService(), new UserSession(0, "Admin", "admin@local", string.Empty, new[] { "Admin" }))
        {
        }

        public MainViewModel(UserSession session)
            : this(new CsvExportService(), session)
        {
        }

        public MainViewModel(ICsvExportService csvExportService, UserSession session)
        {
            _csvExportService = csvExportService ?? throw new ArgumentNullException(nameof(csvExportService));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Tables = new ObservableCollection<TableMenuItem>(BuildTablesForSession(_session));
            StudentTodaySchedule = new ObservableCollection<StudentTodayScheduleRow>();
            StudentCourses = new ObservableCollection<StudentCourseSummaryRow>();
            StudentGrades = new ObservableCollection<BangDiemViewRow>();

            SelectTableCommand = new RelayCommand(async table => await SelectTableAsync(table as TableMenuItem));
            RefreshCommand = new AsyncRelayCommand(LoadSelectedTableAsync, () => SelectedTable != null && !IsBusy);
            ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync, () => RowsView != null && RowsView.Cast<object>().Any() && !IsBusy);

            SelectedTable = Tables.FirstOrDefault();
        }

        public ObservableCollection<TableMenuItem> Tables { get; }
        public ObservableCollection<ChartItem> MonthlyRevenueChart { get; } = new ObservableCollection<ChartItem>();
        public ObservableCollection<ChartItem> CourseStudentChart { get; } = new ObservableCollection<ChartItem>();
        public ObservableCollection<StudentTodayScheduleRow> StudentTodaySchedule { get; }
        public ObservableCollection<StudentCourseSummaryRow> StudentCourses { get; }
        public ObservableCollection<BangDiemViewRow> StudentGrades { get; }
        public UserSession Session => _session;
        public bool CanUseCrud => CanCreate || CanEdit || CanDelete;
        public bool CanCreate => (_session.IsAdmin && CurrentEntityType != null) || (_session.IsTeacher && SelectedTable?.Key == "DiemSo");
        public bool CanEdit => (_session.IsAdmin && (CurrentEntityType != null || SelectedTable?.Key == "YeuCauSuaDiem")) || (_session.IsTeacher && (SelectedTable?.Key == "DiemSo" || SelectedTable?.Key == "vw_BangDiem"));
        public bool CanDelete => _session.IsAdmin && CurrentEntityType != null;
        public bool CanUsePayment => IsPaymentTable
                                     && (_session.IsAccountant || _session.IsAdmin || _session.IsStudent);
        public Visibility PaymentButtonVisibility => CanUsePayment ? Visibility.Visible : Visibility.Collapsed;
        private bool IsPaymentTable => SelectedTable?.Key == "HoaDonHocPhi"
                                       || SelectedTable?.Key == "vw_CongNoHocPhi";
        public string UserSummary => $"{_session.HoTen} - {string.Join(", ", _session.Roles)}";

        public TableMenuItem SelectedTable
        {
            get => _selectedTable;
            private set
            {
                if (SetProperty(ref _selectedTable, value))
                {
                    OnPropertyChanged(nameof(CurrentTitle));
                    OnPropertyChanged(nameof(IsAccountingDashboard));
                    OnPropertyChanged(nameof(IsStudentDashboard));
                    OnPropertyChanged(nameof(DataGridVisibility));
                    OnPropertyChanged(nameof(AccountingDashboardVisibility));
                    OnPropertyChanged(nameof(StudentDashboardVisibility));
                    OnPropertyChanged(nameof(SearchVisibility));
                    OnPropertyChanged(nameof(CanUseCrud));
                    OnPropertyChanged(nameof(CanCreate));
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CanDelete));
                    OnPropertyChanged(nameof(CanUsePayment));
                    OnPropertyChanged(nameof(PaymentButtonVisibility));
                }
            }
        }

        public string CurrentTitle => SelectedTable?.Title ?? "Du lieu";
        public bool IsAccountingDashboard => SelectedTable?.Key == "BaoCaoKeToan";
        public bool IsStudentDashboard => SelectedTable?.Key == "DashboardHocVien";
        public Visibility DataGridVisibility => IsAccountingDashboard || IsStudentDashboard ? Visibility.Collapsed : Visibility.Visible;
        public Visibility AccountingDashboardVisibility => IsAccountingDashboard ? Visibility.Visible : Visibility.Collapsed;
        public Visibility StudentDashboardVisibility => IsStudentDashboard ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SearchVisibility => IsAccountingDashboard || IsStudentDashboard ? Visibility.Collapsed : Visibility.Visible;

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            private set => SetProperty(ref _totalRevenue, value);
        }

        public int UnpaidInvoiceCount
        {
            get => _unpaidInvoiceCount;
            private set => SetProperty(ref _unpaidInvoiceCount, value);
        }

        public int ActiveStudentCount
        {
            get => _activeStudentCount;
            private set => SetProperty(ref _activeStudentCount, value);
        }

        public string TotalRevenueText => TotalRevenue.ToString("#,0", CultureInfo.InvariantCulture);

        public ObservableCollection<object> Rows
        {
            get => _rows;
            private set => SetProperty(ref _rows, value);
        }

        public ICollectionView RowsView
        {
            get => _rowsView;
            private set => SetProperty(ref _rowsView, value);
        }

        public object SelectedRow
        {
            get => _selectedRow;
            set => SetProperty(ref _selectedRow, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RowsView?.Refresh();
                }
            }
        }

        public ICommand SelectTableCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public Type CurrentEntityType => GetEntityType(SelectedTable?.Key);

        public async Task InitializeAsync()
        {
            try
            {
                await EnsureSupportTablesAsync();
                await LoadSidebarCountsAsync();
            }
            catch (Exception ex)
            {
                SetErrorToast(ex);
            }

            await LoadSelectedTableAsync();
        }

        private async Task SelectTableAsync(TableMenuItem table)
        {
            if (table == null || table == SelectedTable)
            {
                return;
            }

            SelectedTable = table;
            SearchText = null;
            await LoadSelectedTableAsync();
        }

        private async Task LoadSidebarCountsAsync()
        {
            foreach (var table in Tables)
            {
                try
                {
                    table.RowCount = await CountRowsForTableAsync(table);
                }
                catch
                {
                    table.RowCount = 0;
                }
            }
        }

        private async Task<int> CountRowsForTableAsync(TableMenuItem table)
        {
            if (table == null)
            {
                return 0;
            }

            if (table.Key == "BaoCaoKeToan")
            {
                await LoadAccountingDashboardAsync();
                return MonthlyRevenueChart.Count + CourseStudentChart.Count;
            }

            if (table.Key == "DashboardHocVien")
            {
                await LoadStudentDashboardAsync();
                return StudentTodaySchedule.Count + StudentCourses.Count + StudentGrades.Count;
            }

            var data = await LoadRowsAsync(table.Key);
            return data.Count;
        }

        private async Task LoadSelectedTableAsync()
        {
            if (SelectedTable == null)
            {
                return;
            }

            try
            {
                IsBusy = true;
                RaiseAsyncCommandStates();

                if (IsAccountingDashboard)
                {
                    await LoadAccountingDashboardAsync();
                    SetEmptyRows();
                    SelectedTable.RowCount = MonthlyRevenueChart.Count + CourseStudentChart.Count;
                    SetSuccessToast("Da tai dashboard ke toan.");
                    return;
                }

                if (IsStudentDashboard)
                {
                    await LoadStudentDashboardAsync();
                    SetEmptyRows();
                    SelectedTable.RowCount = StudentTodaySchedule.Count + StudentCourses.Count + StudentGrades.Count;
                    SetSuccessToast("Da tai dashboard hoc vien.");
                    return;
                }

                var data = await LoadRowsAsync(SelectedTable.Key);
                Rows = new ObservableCollection<object>(data.Cast<object>());
                RowsView = CollectionViewSource.GetDefaultView(Rows);
                RowsView.Filter = FilterRow;
                SelectedTable.RowCount = Rows.Count;
                SelectedRow = null;

                SetSuccessToast($"Da tai {Rows.Count} dong tu {SelectedTable.Title}.");
            }
            catch (Exception ex)
            {
                SetErrorToast(ex);
            }
            finally
            {
                IsBusy = false;
                RaiseAsyncCommandStates();
            }
        }

        private void SetEmptyRows()
        {
            Rows = new ObservableCollection<object>();
            RowsView = CollectionViewSource.GetDefaultView(Rows);
            SelectedRow = null;
        }

        private async Task<IList> LoadRowsAsync(string tableKey)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                switch (tableKey)
                {
                    case "NguoiDung":
                        return await LoadNguoiDungRowsAsync(db);
                    case "KhoaHoc":
                        return await LoadKhoaHocRowsAsync(db);
                    case "VaiTro":
                        return await db.VaiTroes.AsNoTracking().OrderBy(item => item.MaVaiTro).ToListAsync();
                    case "NguoiDungVaiTro":
                        return await LoadNguoiDungVaiTroRowsAsync(db);
                    case "DangKyKhoaHoc":
                        return await LoadDangKyRowsAsync(db);
                    case "HoaDonHocPhi":
                        return await LoadHoaDonRowsAsync(db);
                    case "GiaoDichThanhToan":
                        return await LoadGiaoDichRowsAsync(db);
                    case "ThongTinGiangVien":
                        return await LoadThongTinGiangVienRowsAsync(db);
                    case "PhongBan":
                        return await LoadPhongBanRowsAsync(db);
                    case "PhongHoc":
                        return await db.PhongHocs.AsNoTracking().OrderBy(item => item.MaPhong).ToListAsync();
                    case "LichDay":
                        return await LoadLichDayRowsAsync(db);
                    case "LichHocVien":
                        return await LoadLichHocVienRowsAsync(db);
                    case "DiemSo":
                        return await LoadDiemSoRowsAsync(db);
                    case "vw_CongNoHocPhi":
                        return await LoadCongNoRowsAsync(db);
                    case "vw_DoanhThuTheoThang":
                        return await LoadDoanhThuRowsAsync(db);
                    case "vw_LichDay":
                        return await LoadLichDayViewRowsAsync(db);
                    case "vw_BangDiem":
                        return await LoadBangDiemRowsAsync(db);
                    case "vw_DiemDanh":
                        return await LoadDiemDanhRowsAsync(db);
                    case "YeuCauSuaDiem":
                        return await LoadGradeChangeRequestsAsync(db);
                    default:
                        throw new InvalidOperationException("Bang du lieu khong hop le.");
                }
            }
        }

        private async Task<IList> LoadNguoiDungRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<NguoiDung> query = db.NguoiDungs.AsNoTracking();
            if (_session.IsAccountant && !_session.IsAdmin)
            {
                query = query.Where(item => item.VaiTroes.Any(role => role.MaVaiTro == 3));
            }

            if (!_session.IsAdmin)
            {
                return await query
                    .OrderBy(item => item.HoTen)
                    .Select(item => new NguoiDungPublicRow
                    {
                        MaHocVien = item.MaNguoiDung,
                        HoTen = item.HoTen,
                        Email = item.Email,
                        SoDienThoai = item.SoDienThoai,
                        IsActive = item.IsActive,
                        NgayTao = item.NgayTao
                    })
                    .ToListAsync();
            }

            return await query.OrderBy(item => item.MaNguoiDung).ToListAsync();
        }

        private async Task<IList> LoadKhoaHocRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<KhoaHoc> query = db.KhoaHocs.AsNoTracking();
            if (_session.IsStudent && !_session.IsAdmin)
            {
                query = query.Where(item => item.DangKyKhoaHocs.Any(dangKy => dangKy.MaHocVien == _session.MaNguoiDung));
            }

            if (_session.IsTeacher && !_session.IsAdmin)
            {
                query = query.Where(item => item.NguoiDungs.Any(teacher => teacher.MaNguoiDung == _session.MaNguoiDung));
            }

            return await query.OrderBy(item => item.MaKhoaHoc).ToListAsync();
        }

        private async Task<IList> LoadNguoiDungVaiTroRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            return await db.Database.SqlQuery<NguoiDungVaiTroRow>(
                @"SELECT nd.MaNguoiDung, nd.HoTen, vt.MaVaiTro, vt.TenVaiTro
                  FROM NguoiDung nd
                  JOIN NguoiDung_VaiTro ndvt ON nd.MaNguoiDung = ndvt.MaNguoiDung
                  JOIN VaiTro vt ON ndvt.MaVaiTro = vt.MaVaiTro
                  ORDER BY vt.MaVaiTro, nd.MaNguoiDung").ToListAsync();
        }

        private async Task<IList> LoadDangKyRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<DangKyKhoaHoc> query = db.DangKyKhoaHocs.AsNoTracking();
            if (_session.IsStudent && !_session.IsAdmin)
            {
                query = query.Where(item => item.MaHocVien == _session.MaNguoiDung);
            }
            else if (_session.IsTeacher && !_session.IsAdmin)
            {
                query = query.Where(item => item.KhoaHoc.NguoiDungs.Any(teacher => teacher.MaNguoiDung == _session.MaNguoiDung));
            }

            if (_session.IsAccountant || _session.IsTeacher)
            {
                return await query
                    .OrderByDescending(item => item.NgayDangKy)
                    .Select(item => new DangKyHocVienRow
                    {
                        MaDangKy = item.MaDangKy,
                        MaHocVien = item.MaHocVien,
                        TenHocVien = item.NguoiDung.HoTen,
                        MaKhoaHoc = item.MaKhoaHoc,
                        TenKhoaHoc = item.KhoaHoc.TenKhoaHoc,
                        NgayDangKy = item.NgayDangKy,
                        HocPhiThoiDiem = item.HocPhiThoiDiem,
                        TrangThai = item.TrangThai,
                        GhiChu = item.GhiChu
                    })
                    .ToListAsync();
            }

            return await query.OrderByDescending(item => item.NgayDangKy).ToListAsync();
        }

        private async Task<IList> LoadHoaDonRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<HoaDonHocPhi> query = db.HoaDonHocPhis.AsNoTracking();
            if (_session.IsStudent && !_session.IsAdmin)
            {
                query = query.Where(item => item.DangKyKhoaHoc.MaHocVien == _session.MaNguoiDung);
            }

            if (!_session.IsAdmin)
            {
                return await query
                    .GroupJoin(db.GiaoDichThanhToans.AsNoTracking(),
                        hoaDon => hoaDon.MaHoaDon,
                        giaoDich => giaoDich.MaHoaDon,
                        (hoaDon, giaoDichs) => new HoaDonHocPhiViewRow
                        {
                            MaHoaDon = hoaDon.MaHoaDon,
                            MaDangKy = hoaDon.MaDangKy,
                            MaHocVien = hoaDon.DangKyKhoaHoc.MaHocVien,
                            TenHocVien = hoaDon.DangKyKhoaHoc.NguoiDung.HoTen,
                            MaKhoaHoc = hoaDon.DangKyKhoaHoc.MaKhoaHoc,
                            TenKhoaHoc = hoaDon.DangKyKhoaHoc.KhoaHoc.TenKhoaHoc,
                            TongTien = hoaDon.TongTien,
                            DaThanhToan = giaoDichs.Select(item => (int?)item.SoTien).Sum() ?? 0,
                            ConNo = hoaDon.TongTien - (giaoDichs.Select(item => (int?)item.SoTien).Sum() ?? 0),
                            NgayXuat = hoaDon.NgayXuat,
                            HanThanhToan = hoaDon.HanThanhToan,
                            TrangThai = hoaDon.TrangThai
                        })
                    .OrderByDescending(item => item.NgayXuat)
                    .ToListAsync();
            }

            return await query.OrderByDescending(item => item.NgayXuat).ToListAsync();
        }

        private async Task<IList> LoadGiaoDichRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<GiaoDichThanhToan> query = db.GiaoDichThanhToans.AsNoTracking();
            if (_session.IsStudent && !_session.IsAdmin)
            {
                query = query.Where(item => item.HoaDonHocPhi.DangKyKhoaHoc.MaHocVien == _session.MaNguoiDung);
            }

            if (!_session.IsAdmin)
            {
                return await query
                    .OrderByDescending(item => item.NgayGiaoDich)
                    .Select(item => new GiaoDichThanhToanViewRow
                    {
                        MaGiaoDich = item.MaGiaoDich,
                        MaHoaDon = item.MaHoaDon,
                        MaHocVien = item.HoaDonHocPhi.DangKyKhoaHoc.MaHocVien,
                        TenHocVien = item.HoaDonHocPhi.DangKyKhoaHoc.NguoiDung.HoTen,
                        TenKhoaHoc = item.HoaDonHocPhi.DangKyKhoaHoc.KhoaHoc.TenKhoaHoc,
                        NgayGiaoDich = item.NgayGiaoDich,
                        SoTien = item.SoTien,
                        PhuongThuc = item.PhuongThuc,
                        MaChungTu = item.MaChungTu,
                        GhiChu = item.GhiChu,
                        TenNguoiXacNhan = item.NguoiDung.HoTen
                    })
                    .ToListAsync();
            }

            return await query.OrderByDescending(item => item.NgayGiaoDich).ToListAsync();
        }

        private async Task<IList> LoadThongTinGiangVienRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<ThongTinGiangVien> query = db.ThongTinGiangViens.AsNoTracking();
            if (_session.IsTeacher && !_session.IsAdmin)
            {
                query = query.Where(item => item.MaNguoiDung == _session.MaNguoiDung);
            }

            return await query.OrderBy(item => item.MaNguoiDung).ToListAsync();
        }

        private async Task<IList> LoadPhongBanRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<PhongBan> query = db.PhongBans.AsNoTracking();
            if (_session.IsTeacher && !_session.IsAdmin)
            {
                query = query.Where(item => item.ThongTinGiangViens.Any(info => info.MaNguoiDung == _session.MaNguoiDung) || item.MaTruongPhong == _session.MaNguoiDung);
            }

            return await query.OrderBy(item => item.MaPhongBan).ToListAsync();
        }

        private async Task<IList> LoadLichDayRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<LichDay> query = db.LichDays.AsNoTracking();
            if (_session.IsTeacher && !_session.IsAdmin)
            {
                query = query.Where(item => item.MaGiangVien == _session.MaNguoiDung);
            }
            else if (_session.IsStudent && !_session.IsAdmin)
            {
                query = query.Where(item => item.LichHocViens.Any(lhv => lhv.DangKyKhoaHoc.MaHocVien == _session.MaNguoiDung));
            }

            return await query.OrderBy(item => item.NgayDay).ThenBy(item => item.GioBatDau).ToListAsync();
        }

        private async Task<IList> LoadLichHocVienRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<LichHocVien> query = db.LichHocViens.AsNoTracking();
            if (_session.IsStudent && !_session.IsAdmin)
            {
                query = query.Where(item => item.DangKyKhoaHoc.MaHocVien == _session.MaNguoiDung);
            }
            else if (_session.IsTeacher && !_session.IsAdmin)
            {
                query = query.Where(item => item.LichDay.MaGiangVien == _session.MaNguoiDung);
            }

            return await query.OrderBy(item => item.MaLichHocVien).ToListAsync();
        }

        private async Task<IList> LoadDiemSoRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            IQueryable<DiemSo> query = db.DiemSoes.AsNoTracking();
            if (_session.IsTeacher && !_session.IsAdmin)
            {
                query = query.Where(item => item.MaGiangVien == _session.MaNguoiDung);
            }
            else if (_session.IsStudent && !_session.IsAdmin)
            {
                query = query.Where(item => item.DangKyKhoaHoc.MaHocVien == _session.MaNguoiDung);
            }

            if (_session.IsTeacher || _session.IsAccountant)
            {
                return await query
                    .OrderBy(item => item.MaDiem)
                    .Select(item => new DiemSoViewRow
                    {
                        MaDiem = item.MaDiem,
                        MaDangKy = item.MaDangKy,
                        MaHocVien = item.DangKyKhoaHoc.MaHocVien,
                        TenHocVien = item.DangKyKhoaHoc.NguoiDung.HoTen,
                        MaGiangVien = item.MaGiangVien,
                        TenGiangVien = item.NguoiDung.HoTen,
                        TenKhoaHoc = item.DangKyKhoaHoc.KhoaHoc.TenKhoaHoc,
                        LoaiKiemTra = item.LoaiKiemTra,
                        Diem = item.Diem,
                        NgayKiemTra = item.NgayKiemTra,
                        NhanXet = item.NhanXet
                    })
                    .ToListAsync();
            }

            return await query.OrderBy(item => item.MaDiem).ToListAsync();
        }

        private async Task<IList> LoadCongNoRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            var sql = @"
                SELECT hd.MaHoaDon, dk.MaHocVien, nd.HoTen AS TenHocVien, nd.Email AS EmailHocVien,
                       dk.MaKhoaHoc, kh.TenKhoaHoc, hd.TongTien,
                       ISNULL(SUM(gd.SoTien), 0) AS DaThanhToan,
                       CASE
                           WHEN hd.TongTien - ISNULL(SUM(gd.SoTien), 0) > 0 THEN hd.TongTien - ISNULL(SUM(gd.SoTien), 0)
                           ELSE 0
                       END AS ConNo,
                       hd.HanThanhToan, hd.TrangThai,
                       CASE
                           WHEN hd.HanThanhToan < CAST(GETDATE() AS date)
                                AND hd.TongTien - ISNULL(SUM(gd.SoTien), 0) > 0 THEN 1
                           ELSE 0
                       END AS QuaHan
                FROM HoaDonHocPhi hd
                JOIN DangKyKhoaHoc dk ON hd.MaDangKy = dk.MaDangKy
                JOIN NguoiDung nd ON dk.MaHocVien = nd.MaNguoiDung
                JOIN KhoaHoc kh ON dk.MaKhoaHoc = kh.MaKhoaHoc
                LEFT JOIN GiaoDichThanhToan gd ON hd.MaHoaDon = gd.MaHoaDon
                WHERE (@MaHocVien = 0 OR dk.MaHocVien = @MaHocVien)
                GROUP BY hd.MaHoaDon, dk.MaHocVien, nd.HoTen, nd.Email, dk.MaKhoaHoc, kh.TenKhoaHoc, hd.TongTien, hd.HanThanhToan, hd.TrangThai
                ORDER BY hd.MaHoaDon";

            return await db.Database.SqlQuery<CongNoHocPhiRow>(
                sql,
                CreateIntParameter("@MaHocVien", _session.IsStudent && !_session.IsAdmin ? _session.MaNguoiDung : 0)).ToListAsync();
        }

        private async Task<IList> LoadDoanhThuRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            return await db.Database.SqlQuery<DoanhThuTheoThangRow>(
                @"SELECT YEAR(NgayGiaoDich) AS Nam, MONTH(NgayGiaoDich) AS Thang,
                         COUNT(*) AS SoGiaoDich, SUM(SoTien) AS TongDoanhThu,
                         COUNT(DISTINCT MaHoaDon) AS SoHoaDon
                  FROM GiaoDichThanhToan
                  GROUP BY YEAR(NgayGiaoDich), MONTH(NgayGiaoDich)
                  ORDER BY Nam, Thang").ToListAsync();
        }

        private async Task<IList> LoadLichDayViewRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            var sql = @"
                SELECT ld.MaLich, ld.NgayDay, ld.GioBatDau, ld.GioKetThuc, kh.TenKhoaHoc,
                       gv.HoTen AS TenGiangVien, ph.TenPhong, ph.SucChua, ld.TrangThai, ld.GhiChu
                FROM LichDay ld
                JOIN KhoaHoc kh ON ld.MaKhoaHoc = kh.MaKhoaHoc
                JOIN NguoiDung gv ON ld.MaGiangVien = gv.MaNguoiDung
                JOIN PhongHoc ph ON ld.MaPhong = ph.MaPhong
                WHERE (@MaGiangVien = 0 OR ld.MaGiangVien = @MaGiangVien)
                ORDER BY ld.NgayDay, ld.GioBatDau";

            return await db.Database.SqlQuery<LichDayViewRow>(
                sql,
                CreateIntParameter("@MaGiangVien", _session.IsTeacher && !_session.IsAdmin ? _session.MaNguoiDung : 0)).ToListAsync();
        }

        private async Task<IList> LoadBangDiemRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            return await db.Database.SqlQuery<BangDiemViewRow>(
                BuildBangDiemSql(),
                CreateIntParameter("@MaHocVien", _session.IsStudent && !_session.IsAdmin ? _session.MaNguoiDung : 0),
                CreateIntParameter("@MaGiangVien", _session.IsTeacher && !_session.IsAdmin ? _session.MaNguoiDung : 0)).ToListAsync();
        }

        private static string BuildBangDiemSql()
        {
            return @"
                SELECT ds.MaDiem, dk.MaDangKy, dk.MaHocVien, hv.HoTen AS TenHocVien, kh.TenKhoaHoc,
                       gv.HoTen AS TenGiangVienCham, ds.LoaiKiemTra, ds.Diem,
                       ds.NgayKiemTra, ds.NhanXet,
                       CASE WHEN ds.Diem >= 8 THEN N'Giỏi'
                            WHEN ds.Diem >= 6.5 THEN N'Khá'
                            WHEN ds.Diem >= 5 THEN N'Trung bình'
                            ELSE N'Cần cố gắng' END AS XepLoai
                FROM DiemSo ds
                JOIN DangKyKhoaHoc dk ON ds.MaDangKy = dk.MaDangKy
                JOIN NguoiDung hv ON dk.MaHocVien = hv.MaNguoiDung
                JOIN KhoaHoc kh ON dk.MaKhoaHoc = kh.MaKhoaHoc
                JOIN NguoiDung gv ON ds.MaGiangVien = gv.MaNguoiDung
                WHERE (@MaHocVien = 0 OR dk.MaHocVien = @MaHocVien)
                  AND (@MaGiangVien = 0 OR ds.MaGiangVien = @MaGiangVien)
                ORDER BY dk.MaDangKy, ds.LoaiKiemTra";
        }

        private static string BuildStudentBangDiemSql()
        {
            return @"
                SELECT ds.MaDiem, dk.MaDangKy, dk.MaHocVien, hv.HoTen AS TenHocVien, kh.TenKhoaHoc,
                       gv.HoTen AS TenGiangVienCham, ds.LoaiKiemTra, ds.Diem,
                       ds.NgayKiemTra, ds.NhanXet,
                       CASE WHEN ds.Diem >= 8 THEN N'Giỏi'
                            WHEN ds.Diem >= 6.5 THEN N'Khá'
                            WHEN ds.Diem >= 5 THEN N'Trung bình'
                            ELSE N'Cần cố gắng' END AS XepLoai
                FROM DiemSo ds
                JOIN DangKyKhoaHoc dk ON ds.MaDangKy = dk.MaDangKy
                JOIN NguoiDung hv ON dk.MaHocVien = hv.MaNguoiDung
                JOIN KhoaHoc kh ON dk.MaKhoaHoc = kh.MaKhoaHoc
                JOIN NguoiDung gv ON ds.MaGiangVien = gv.MaNguoiDung
                WHERE dk.MaHocVien = @MaHocVien
                ORDER BY dk.MaDangKy, ds.LoaiKiemTra";
        }

        private async Task<IList> LoadDiemDanhRowsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            var sql = @"
                SELECT lhv.MaLichHocVien, dk.MaHocVien, ld.NgayDay, ld.GioBatDau, kh.TenKhoaHoc,
                       hv.HoTen AS TenHocVien, gv.HoTen AS TenGiangVien, ph.TenPhong,
                       lhv.DiemDanh, lhv.GhiChu
                FROM LichHocVien lhv
                JOIN DangKyKhoaHoc dk ON lhv.MaDangKy = dk.MaDangKy
                JOIN NguoiDung hv ON dk.MaHocVien = hv.MaNguoiDung
                JOIN LichDay ld ON lhv.MaLich = ld.MaLich
                JOIN NguoiDung gv ON ld.MaGiangVien = gv.MaNguoiDung
                JOIN KhoaHoc kh ON ld.MaKhoaHoc = kh.MaKhoaHoc
                JOIN PhongHoc ph ON ld.MaPhong = ph.MaPhong
                WHERE (@MaHocVien = 0 OR dk.MaHocVien = @MaHocVien)
                  AND (@MaGiangVien = 0 OR ld.MaGiangVien = @MaGiangVien)
                ORDER BY ld.NgayDay, ld.GioBatDau";

            return await db.Database.SqlQuery<DiemDanhViewRow>(
                sql,
                CreateIntParameter("@MaHocVien", _session.IsStudent && !_session.IsAdmin ? _session.MaNguoiDung : 0),
                CreateIntParameter("@MaGiangVien", _session.IsTeacher && !_session.IsAdmin ? _session.MaNguoiDung : 0)).ToListAsync();
        }

        private async Task LoadAccountingDashboardAsync()
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                var payments = await db.GiaoDichThanhToans.AsNoTracking().ToListAsync();
                var monthlyRevenue = payments
                    .Where(item => item.NgayGiaoDich.HasValue)
                    .GroupBy(item => new { item.NgayGiaoDich.Value.Year, item.NgayGiaoDich.Value.Month })
                    .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.Month)
                    .Select(group => new { Label = $"{group.Key.Month:00}/{group.Key.Year}", Value = (decimal)group.Sum(item => item.SoTien) })
                    .ToList();

                var courseStats = await db.DangKyKhoaHocs.AsNoTracking()
                    .GroupBy(item => item.KhoaHoc.TenKhoaHoc)
                    .Select(group => new { Label = group.Key, Value = group.Select(item => item.MaHocVien).Distinct().Count() })
                    .OrderByDescending(item => item.Value)
                    .Take(8)
                    .ToListAsync();

                TotalRevenue = monthlyRevenue.Sum(item => item.Value);
                OnPropertyChanged(nameof(TotalRevenueText));
                UnpaidInvoiceCount = await db.HoaDonHocPhis.AsNoTracking().CountAsync(item => item.TrangThai != "Đã hoàn tất");
                ActiveStudentCount = await db.NguoiDungs.AsNoTracking().CountAsync(item => item.IsActive && item.VaiTroes.Any(role => role.MaVaiTro == 3));

                ReplaceChartItems(MonthlyRevenueChart, monthlyRevenue.Select(item => new ChartItem(item.Label, item.Value, monthlyRevenue.Any() ? monthlyRevenue.Max(row => row.Value) : 0)));
                ReplaceChartItems(CourseStudentChart, courseStats.Select(item => new ChartItem(item.Label, item.Value, courseStats.Any() ? courseStats.Max(row => row.Value) : 0)));
            }
        }

        private async Task LoadStudentDashboardAsync()
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                var today = DateTime.Today;
                var schedules = await db.Database.SqlQuery<StudentTodayScheduleRow>(
                    @"SELECT ld.NgayDay, ld.GioBatDau, ld.GioKetThuc, kh.TenKhoaHoc, ph.TenPhong,
                             gv.HoTen AS TenGiangVien, lhv.DiemDanh
                      FROM LichHocVien lhv
                      JOIN DangKyKhoaHoc dk ON lhv.MaDangKy = dk.MaDangKy
                      JOIN LichDay ld ON lhv.MaLich = ld.MaLich
                      JOIN KhoaHoc kh ON ld.MaKhoaHoc = kh.MaKhoaHoc
                      JOIN PhongHoc ph ON ld.MaPhong = ph.MaPhong
                      JOIN NguoiDung gv ON ld.MaGiangVien = gv.MaNguoiDung
                      WHERE dk.MaHocVien = @MaHocVien AND ld.NgayDay = @Today
                      ORDER BY ld.GioBatDau",
                    CreateIntParameter("@MaHocVien", _session.MaNguoiDung),
                    new SqlParameter("@Today", today)).ToListAsync();

                var courses = await db.Database.SqlQuery<StudentCourseSummaryRow>(
                    @"SELECT kh.TenKhoaHoc, dk.TrangThai, dk.HocPhiThoiDiem, dk.NgayDangKy
                      FROM DangKyKhoaHoc dk
                      JOIN KhoaHoc kh ON dk.MaKhoaHoc = kh.MaKhoaHoc
                      WHERE dk.MaHocVien = @MaHocVien
                      ORDER BY dk.NgayDangKy DESC",
                    CreateIntParameter("@MaHocVien", _session.MaNguoiDung)).ToListAsync();

                var grades = await db.Database.SqlQuery<BangDiemViewRow>(
                    BuildStudentBangDiemSql(),
                    CreateIntParameter("@MaHocVien", _session.MaNguoiDung)).ToListAsync();

                ReplaceItems(StudentTodaySchedule, schedules);
                ReplaceItems(StudentCourses, courses);
                ReplaceItems(StudentGrades, grades);
            }
        }

        private async Task<IList> LoadGradeChangeRequestsAsync(HeThongQuanLyTrungTamNgoaiNguEntities db)
        {
            return await db.Database.SqlQuery<GradeChangeRequestRow>(
                @"SELECT yc.MaYeuCau, yc.MaDiem, yc.MaGiangVien, nd.HoTen AS TenGiangVien,
                         yc.DiemCu, yc.DiemMoi, yc.NhanXetMoi, yc.LyDo, yc.TrangThai, yc.NgayYeuCau
                  FROM YeuCauSuaDiem yc
                  JOIN NguoiDung nd ON yc.MaGiangVien = nd.MaNguoiDung
                  ORDER BY yc.NgayYeuCau DESC").ToListAsync();
        }

        private static TableMenuItem[] BuildTablesForSession(UserSession session)
        {
            if (session.IsAdmin)
            {
                return new[]
                {
                    new TableMenuItem("NguoiDung", "Nguoi dung", "Tai khoan, email, so dien thoai"),
                    new TableMenuItem("VaiTro", "Vai tro", "Nhom quyen he thong"),
                    new TableMenuItem("KhoaHoc", "Khoa hoc", "Danh muc khoa va hoc phi"),
                    new TableMenuItem("DangKyKhoaHoc", "Dang ky khoa hoc", "Lich su dang ky hoc"),
                    new TableMenuItem("HoaDonHocPhi", "Hoa don hoc phi", "Cong no va han thanh toan"),
                    new TableMenuItem("GiaoDichThanhToan", "Giao dich thanh toan", "Bien nhan va chung tu"),
                    new TableMenuItem("ThongTinGiangVien", "Giang vien", "Ho so giang vien"),
                    new TableMenuItem("PhongBan", "Phong ban", "Co cau phong ban"),
                    new TableMenuItem("PhongHoc", "Phong hoc", "Phong va suc chua"),
                    new TableMenuItem("LichDay", "Lich day", "Lich giang day"),
                    new TableMenuItem("LichHocVien", "Diem danh", "Lich hoc vien theo buoi"),
                    new TableMenuItem("DiemSo", "Diem so", "Ket qua kiem tra"),
                    new TableMenuItem("YeuCauSuaDiem", "Yeu cau sua diem", "Admin duyet yeu cau tu giang vien"),
                    new TableMenuItem("vw_CongNoHocPhi", "Bao cao cong no", "Cong no hoc phi"),
                    new TableMenuItem("vw_DoanhThuTheoThang", "Doanh thu thang", "Doanh thu theo thang"),
                };
            }

            if (session.IsAccountant)
            {
                return new[]
                {
                    new TableMenuItem("BaoCaoKeToan", "Bao cao dong tien", "Bieu do hoc phi va hoc vien"),
                    new TableMenuItem("vw_CongNoHocPhi", "Cong no hoc phi", "Cong no hoc phi"),
                    new TableMenuItem("vw_DoanhThuTheoThang", "Doanh thu thang", "Doanh thu theo thang"),
                    new TableMenuItem("HoaDonHocPhi", "Hoa don hoc phi", "Danh sach hoa don"),
                    new TableMenuItem("GiaoDichThanhToan", "Bien lai", "Giao dich da ghi nhan"),
                    new TableMenuItem("NguoiDung", "Hoc vien", "Tham chieu hoc vien"),
                    new TableMenuItem("DangKyKhoaHoc", "Dang ky", "Tham chieu dang ky")
                };
            }

            if (session.IsTeacher)
            {
                return new[]
                {
                    new TableMenuItem("vw_LichDay", "Lich day cua toi", "Lich trinh giang day"),
                    new TableMenuItem("vw_BangDiem", "Bang diem", "Diem da cham"),
                    new TableMenuItem("vw_DiemDanh", "Diem danh", "Theo doi buoi hoc"),
                    new TableMenuItem("KhoaHoc", "Lop hoc", "Khoa hoc phu trach"),
                    new TableMenuItem("PhongBan", "Phong ban", "Phong ban cua toi"),
                    new TableMenuItem("LichDay", "Lich day", "Du lieu lich day"),
                    new TableMenuItem("DiemSo", "Nhap diem", "Them diem va yeu cau sua diem")
                };
            }

            return new[]
            {
                new TableMenuItem("DashboardHocVien", "Dashboard hoc vien", "Lich hoc, phong, giang vien, bang diem"),
                new TableMenuItem("KhoaHoc", "Khoa hoc cua toi", "Khoa hoc dang theo hoc"),
                new TableMenuItem("DangKyKhoaHoc", "Dang ky cua toi", "Dang ky va trang thai hoc"),
                new TableMenuItem("HoaDonHocPhi", "Hoa don cua toi", "Hoc phi va cong no"),
                new TableMenuItem("GiaoDichThanhToan", "Thanh toan cua toi", "Bien lai thanh toan"),
                new TableMenuItem("LichDay", "Lich hoc", "Lich hoc lien quan"),
                new TableMenuItem("vw_CongNoHocPhi", "Cong no cua toi", "Cong no hoc phi"),
                new TableMenuItem("vw_BangDiem", "Bang diem cua toi", "Ket qua hoc tap"),
                new TableMenuItem("vw_DiemDanh", "Diem danh cua toi", "Tinh hinh tham gia")
            };
        }

        public async Task SaveEntityAsync(IDictionary<string, string> values, bool isEdit)
        {
            if (!CanUseCrud)
            {
                throw new InvalidOperationException("Tai khoan hien tai khong co quyen CRUD.");
            }

            if (_session.IsTeacher && SelectedTable?.Key == "DiemSo")
            {
                await SaveTeacherScoreAsync(values, isEdit);
                await LoadSelectedTableAsync();
                return;
            }

            if (_session.IsTeacher && SelectedTable?.Key == "vw_BangDiem")
            {
                await RequestTeacherGradeChangeFromBangDiemAsync(values);
                await LoadSelectedTableAsync();
                return;
            }

            if (_session.IsAdmin && SelectedTable?.Key == "YeuCauSuaDiem")
            {
                await SaveGradeChangeDecisionAsync(values);
                await LoadSelectedTableAsync();
                return;
            }

            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                var entityType = CurrentEntityType;
                var entity = Activator.CreateInstance(entityType);
                foreach (var property in entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite && IsScalarType(p.PropertyType)))
                {
                    if (values.TryGetValue(property.Name, out var rawValue))
                    {
                        property.SetValue(entity, ConvertValue(rawValue, property.PropertyType));
                    }
                }

                db.Set(entityType).Add(entity);
                if (isEdit)
                {
                    db.Entry(entity).State = EntityState.Modified;
                }

                await db.SaveChangesAsync();
            }

            await LoadSelectedTableAsync();
        }

        private async Task SaveTeacherScoreAsync(IDictionary<string, string> values, bool isEdit)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                if (!isEdit)
                {
                    var score = new DiemSo();
                    ApplyValues(score, values);
                    score.MaGiangVien = _session.MaNguoiDung;
                    db.DiemSoes.Add(score);
                    await db.SaveChangesAsync();
                    SetSuccessToast("Da nhap diem moi.");
                    return;
                }

                var maDiem = int.Parse(values["MaDiem"], CultureInfo.InvariantCulture);
                var current = await db.DiemSoes.AsNoTracking().FirstOrDefaultAsync(item => item.MaDiem == maDiem && item.MaGiangVien == _session.MaNguoiDung);
                if (current == null)
                {
                    throw new InvalidOperationException("Chi duoc yeu cau sua diem do chinh giang vien nay nhap.");
                }

                await db.Database.ExecuteSqlCommandAsync(
                    @"INSERT INTO YeuCauSuaDiem (MaDiem, MaGiangVien, DiemCu, DiemMoi, NhanXetMoi, LyDo, TrangThai, NgayYeuCau)
                      VALUES (@MaDiem, @MaGiangVien, @DiemCu, @DiemMoi, @NhanXetMoi, @LyDo, N'Chờ duyệt', GETDATE())",
                    new SqlParameter("@MaDiem", maDiem),
                    new SqlParameter("@MaGiangVien", _session.MaNguoiDung),
                    new SqlParameter("@DiemCu", (object)current.Diem ?? DBNull.Value),
                    new SqlParameter("@DiemMoi", ParseNullableDecimal(values.ContainsKey("Diem") ? values["Diem"] : null) ?? (object)DBNull.Value),
                    new SqlParameter("@NhanXetMoi", values.ContainsKey("NhanXet") ? (object)values["NhanXet"] : DBNull.Value),
                    new SqlParameter("@LyDo", "Giang vien yeu cau sua diem tu man hinh DiemSo"));

                SetSuccessToast("Da gui yeu cau sua diem cho Admin duyet. Diem goc chua bi thay doi.");
            }
        }

        public async Task RequestTeacherGradeChangeFromBangDiemAsync(IDictionary<string, string> values)
        {
            if (!_session.IsTeacher || SelectedTable?.Key != "vw_BangDiem")
            {
                throw new InvalidOperationException("Chi giang vien moi duoc gui yeu cau sua diem tu bang diem.");
            }

            var maDiem = int.Parse(values["MaDiem"], CultureInfo.InvariantCulture);
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                var current = await db.DiemSoes.AsNoTracking().FirstOrDefaultAsync(item => item.MaDiem == maDiem && item.MaGiangVien == _session.MaNguoiDung);
                if (current == null)
                {
                    throw new InvalidOperationException("Chi duoc yeu cau sua diem do chinh giang vien nay cham.");
                }

                await InsertGradeChangeRequestAsync(db, current, values, "Giang vien yeu cau sua diem tu tab Bang diem");
                SetSuccessToast("Da gui yeu cau sua diem cho Admin duyet. Diem goc chua bi thay doi.");
            }
        }

        private async Task InsertGradeChangeRequestAsync(HeThongQuanLyTrungTamNgoaiNguEntities db, DiemSo current, IDictionary<string, string> values, string lyDo)
        {
            await db.Database.ExecuteSqlCommandAsync(
                @"INSERT INTO YeuCauSuaDiem (MaDiem, MaGiangVien, DiemCu, DiemMoi, NhanXetMoi, LyDo, TrangThai, NgayYeuCau)
                  VALUES (@MaDiem, @MaGiangVien, @DiemCu, @DiemMoi, @NhanXetMoi, @LyDo, N'Chờ duyệt', GETDATE())",
                new SqlParameter("@MaDiem", current.MaDiem),
                new SqlParameter("@MaGiangVien", _session.MaNguoiDung),
                new SqlParameter("@DiemCu", (object)current.Diem ?? DBNull.Value),
                new SqlParameter("@DiemMoi", ParseNullableDecimal(values.ContainsKey("Diem") ? values["Diem"] : null) ?? (object)DBNull.Value),
                new SqlParameter("@NhanXetMoi", values.ContainsKey("NhanXet") ? (object)values["NhanXet"] : DBNull.Value),
                new SqlParameter("@LyDo", lyDo));
        }

        private async Task SaveGradeChangeDecisionAsync(IDictionary<string, string> values)
        {
            var maYeuCau = int.Parse(values["MaYeuCau"], CultureInfo.InvariantCulture);
            var trangThai = values.ContainsKey("TrangThai") ? values["TrangThai"] : "Chờ duyệt";
            await SaveGradeChangeDecisionAsync(maYeuCau, trangThai);
        }

        public async Task DecideGradeChangeRequestAsync(GradeChangeRequestRow request, bool approve)
        {
            if (!_session.IsAdmin)
            {
                throw new InvalidOperationException("Chi Admin moi duoc duyet yeu cau sua diem.");
            }

            if (request == null)
            {
                throw new InvalidOperationException("Hay chon mot yeu cau sua diem.");
            }

            if (!string.Equals(request.TrangThai, "Chờ duyệt", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Yeu cau nay da duoc xu ly.");
            }

            await SaveGradeChangeDecisionAsync(request.MaYeuCau, approve ? "Đã duyệt" : "Từ chối");
            await LoadSelectedTableAsync();
        }

        private async Task SaveGradeChangeDecisionAsync(int maYeuCau, string trangThai)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                if (trangThai == "Đã duyệt" || trangThai == "Da duyet")
                {
                    await db.Database.ExecuteSqlCommandAsync(
                        @"UPDATE ds
                          SET ds.Diem = yc.DiemMoi,
                              ds.NhanXet = COALESCE(yc.NhanXetMoi, ds.NhanXet)
                          FROM DiemSo ds
                          JOIN YeuCauSuaDiem yc ON ds.MaDiem = yc.MaDiem
                          WHERE yc.MaYeuCau = @MaYeuCau;

                          UPDATE YeuCauSuaDiem
                          SET TrangThai = N'Đã duyệt', NgayDuyet = GETDATE(), MaAdminDuyet = @MaAdmin
                          WHERE MaYeuCau = @MaYeuCau",
                        new SqlParameter("@MaYeuCau", maYeuCau),
                        new SqlParameter("@MaAdmin", _session.MaNguoiDung));
                    SetSuccessToast("Da duyet yeu cau va cap nhat diem.");
                    return;
                }

                if (trangThai == "Từ chối" || trangThai == "Tu choi")
                {
                    await db.Database.ExecuteSqlCommandAsync(
                        @"UPDATE YeuCauSuaDiem
                          SET TrangThai = N'Từ chối', NgayDuyet = GETDATE(), MaAdminDuyet = @MaAdmin
                          WHERE MaYeuCau = @MaYeuCau",
                        new SqlParameter("@MaYeuCau", maYeuCau),
                        new SqlParameter("@MaAdmin", _session.MaNguoiDung));
                    SetSuccessToast("Da tu choi yeu cau. Diem goc duoc giu nguyen.");
                    return;
                }

                await db.Database.ExecuteSqlCommandAsync(
                    "UPDATE YeuCauSuaDiem SET TrangThai = @TrangThai WHERE MaYeuCau = @MaYeuCau",
                    new SqlParameter("@TrangThai", trangThai),
                    new SqlParameter("@MaYeuCau", maYeuCau));
                    SetSuccessToast("Da cap nhat trang thai yeu cau.");
            }
        }

        public async Task DeleteEntitiesAsync(IList selectedItems)
        {
            if (!_session.IsAdmin)
            {
                throw new InvalidOperationException("Chi Admin moi co quyen xoa.");
            }

            if (selectedItems == null || selectedItems.Count == 0)
            {
                throw new InvalidOperationException("Hay chon it nhat mot dong de xoa.");
            }

            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                foreach (var item in selectedItems.Cast<object>().ToList())
                {
                    var attached = db.Set(item.GetType()).Attach(item);
                    db.Set(item.GetType()).Remove(attached);
                }

                await db.SaveChangesAsync();
            }

            await LoadSelectedTableAsync();
        }

        public async Task PayAsync(int maHoaDon, int soTien, string phuongThuc, string maChungTu, string ghiChu)
        {
            if (!CanUsePayment)
            {
                throw new InvalidOperationException("Tai khoan hien tai khong co quyen thanh toan o tab nay.");
            }

            if (_session.IsStudent && !_session.IsAdmin)
            {
                await EnsureStudentOwnsInvoiceAsync(maHoaDon);
            }

            var result = await new PaymentService().ThanhToanAsync(maHoaDon, soTien, phuongThuc, maChungTu, ghiChu, _session.MaNguoiDung);
            SetSuccessToast($"Hoa don {result.MaHoaDon}: {result.TrangThaiThanhToan}.");
            await LoadSelectedTableAsync();
        }

        private async Task EnsureStudentOwnsInvoiceAsync(int maHoaDon)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                var ownsInvoice = await db.HoaDonHocPhis
                    .AsNoTracking()
                    .AnyAsync(item => item.MaHoaDon == maHoaDon && item.DangKyKhoaHoc.MaHocVien == _session.MaNguoiDung);

                if (!ownsInvoice)
                {
                    throw new InvalidOperationException("Hoc vien chi duoc thanh toan hoa don cua chinh minh.");
                }
            }
        }

        private async Task EnsureSupportTablesAsync()
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities())
            {
                await db.Database.ExecuteSqlCommandAsync(
                    @"IF OBJECT_ID(N'dbo.YeuCauSuaDiem', N'U') IS NULL
                      CREATE TABLE dbo.YeuCauSuaDiem (
                          MaYeuCau INT IDENTITY(1,1) PRIMARY KEY,
                          MaDiem INT NOT NULL,
                          MaGiangVien INT NOT NULL,
                          DiemCu DECIMAL(5,2) NULL,
                          DiemMoi DECIMAL(5,2) NULL,
                          NhanXetMoi NVARCHAR(500) NULL,
                          LyDo NVARCHAR(500) NULL,
                          TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
                          NgayYeuCau DATETIME NOT NULL DEFAULT GETDATE(),
                          NgayDuyet DATETIME NULL,
                          MaAdminDuyet INT NULL,
                          GhiChuAdmin NVARCHAR(500) NULL,
                          FOREIGN KEY (MaDiem) REFERENCES dbo.DiemSo(MaDiem),
                          FOREIGN KEY (MaGiangVien) REFERENCES dbo.NguoiDung(MaNguoiDung),
                          FOREIGN KEY (MaAdminDuyet) REFERENCES dbo.NguoiDung(MaNguoiDung)
                      )");
            }
        }

        private bool FilterRow(object row)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            var keyword = SearchText.Trim();
            return row.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => IsScalarType(property.PropertyType))
                .Select(property => property.GetValue(row))
                .Where(value => value != null)
                .Any(value => value.ToString().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private async Task ExportCsvAsync()
        {
            try
            {
                var path = await _csvExportService.ExportAsync(RowsView?.Cast<object>(), SelectedTable?.Key ?? "DuLieu");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    SetSuccessToast($"Da xuat CSV: {path}");
                }
            }
            catch (Exception ex)
            {
                SetErrorToast(ex);
            }
        }

        private static void ReplaceChartItems(ObservableCollection<ChartItem> target, IEnumerable<ChartItem> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

        private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

        private static SqlParameter CreateIntParameter(string name, int value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value };
        }

        private static Type GetEntityType(string tableKey)
        {
            switch (tableKey)
            {
                case "NguoiDung": return typeof(NguoiDung);
                case "KhoaHoc": return typeof(KhoaHoc);
                case "VaiTro": return typeof(VaiTro);
                case "DangKyKhoaHoc": return typeof(DangKyKhoaHoc);
                case "HoaDonHocPhi": return typeof(HoaDonHocPhi);
                case "GiaoDichThanhToan": return typeof(GiaoDichThanhToan);
                case "ThongTinGiangVien": return typeof(ThongTinGiangVien);
                case "PhongBan": return typeof(PhongBan);
                case "PhongHoc": return typeof(PhongHoc);
                case "LichDay": return typeof(LichDay);
                case "LichHocVien": return typeof(LichHocVien);
                case "DiemSo": return typeof(DiemSo);
                default: return null;
            }
        }

        private static void ApplyValues(object entity, IDictionary<string, string> values)
        {
            foreach (var property in entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite && IsScalarType(p.PropertyType)))
            {
                if (values.TryGetValue(property.Name, out var rawValue))
                {
                    property.SetValue(entity, ConvertValue(rawValue, property.PropertyType));
                }
            }
        }

        private static object ConvertValue(string rawValue, Type targetType)
        {
            var nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null && string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            targetType = nullableType ?? targetType;
            if (targetType == typeof(string)) return rawValue;
            if (targetType == typeof(int)) return int.Parse(rawValue, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return bool.Parse(rawValue);
            if (targetType == typeof(DateTime)) return DateTime.Parse(rawValue, CultureInfo.CurrentCulture);
            if (targetType == typeof(TimeSpan)) return TimeSpan.Parse(rawValue, CultureInfo.CurrentCulture);
            if (targetType == typeof(decimal)) return decimal.Parse(rawValue, CultureInfo.InvariantCulture);
            return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
        }

        private static decimal? ParseNullableDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        private static bool IsScalarType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(decimal)
                   || type == typeof(DateTime)
                   || type == typeof(TimeSpan)
                   || type == typeof(Guid);
        }

        private void RaiseAsyncCommandStates()
        {
            (RefreshCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ExportCsvCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        public sealed class NguoiDungVaiTroRow
        {
            public int MaNguoiDung { get; set; }
            public string HoTen { get; set; }
            public int MaVaiTro { get; set; }
            public string TenVaiTro { get; set; }
        }

        public sealed class NguoiDungPublicRow
        {
            public int MaHocVien { get; set; }
            public string HoTen { get; set; }
            public string Email { get; set; }
            public string SoDienThoai { get; set; }
            public bool IsActive { get; set; }
            public DateTime NgayTao { get; set; }
        }
    }
}
