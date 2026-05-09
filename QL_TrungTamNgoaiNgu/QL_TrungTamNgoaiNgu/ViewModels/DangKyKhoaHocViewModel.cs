using QL_TrungTamNgoaiNgu.Models;
using QL_TrungTamNgoaiNgu.Services;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QL_TrungTamNgoaiNgu.ViewModels
{
    public sealed class DangKyKhoaHocViewModel : BaseViewModel
    {
        private readonly IKhoaHocRegistrationService _registrationService;
        private int? _maHocVien;
        private int? _maKhoaHoc;
        private DateTime _hanThanhToan = DateTime.Today.AddDays(7);
        private DangKyKhoaHocResult _ketQuaDangKy;

        public DangKyKhoaHocViewModel()
            : this(new KhoaHocRegistrationService())
        {
        }

        public DangKyKhoaHocViewModel(IKhoaHocRegistrationService registrationService)
        {
            _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
            DangKyCommand = new AsyncRelayCommand(DangKyAsync, CanDangKy);
        }

        public int? MaHocVien
        {
            get => _maHocVien;
            set
            {
                if (SetProperty(ref _maHocVien, value))
                {
                    RaiseDangKyCommandCanExecuteChanged();
                }
            }
        }

        public int? MaKhoaHoc
        {
            get => _maKhoaHoc;
            set
            {
                if (SetProperty(ref _maKhoaHoc, value))
                {
                    RaiseDangKyCommandCanExecuteChanged();
                }
            }
        }

        public DateTime HanThanhToan
        {
            get => _hanThanhToan;
            set
            {
                if (SetProperty(ref _hanThanhToan, value))
                {
                    RaiseDangKyCommandCanExecuteChanged();
                }
            }
        }

        public DangKyKhoaHocResult KetQuaDangKy
        {
            get => _ketQuaDangKy;
            private set => SetProperty(ref _ketQuaDangKy, value);
        }

        public ICommand DangKyCommand { get; }

        private bool CanDangKy()
        {
            return !IsBusy
                   && MaHocVien.HasValue
                   && MaHocVien.Value > 0
                   && MaKhoaHoc.HasValue
                   && MaKhoaHoc.Value > 0
                   && HanThanhToan.Date >= DateTime.Today;
        }

        private async Task DangKyAsync()
        {
            if (!CanDangKy())
            {
                return;
            }

            try
            {
                IsBusy = true;
                RaiseDangKyCommandCanExecuteChanged();

                KetQuaDangKy = await _registrationService.DangKyKhoaHocAsync(
                    MaHocVien.Value,
                    MaKhoaHoc.Value,
                    HanThanhToan);

                SetSuccessToast(KetQuaDangKy?.ThongBao ?? "Dang ky khoa hoc thanh cong.");
            }
            catch (SqlException ex)
            {
                SetErrorToast(ex);
            }
            catch (Exception ex)
            {
                SetErrorToast(ex);
            }
            finally
            {
                IsBusy = false;
                RaiseDangKyCommandCanExecuteChanged();
            }
        }

        private void RaiseDangKyCommandCanExecuteChanged()
        {
            if (DangKyCommand is AsyncRelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }
}
