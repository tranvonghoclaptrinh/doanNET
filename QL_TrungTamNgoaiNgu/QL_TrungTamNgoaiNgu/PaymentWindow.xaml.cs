using System;
using System.Windows;
using System.Windows.Controls;

namespace QL_TrungTamNgoaiNgu
{
    public partial class PaymentWindow : Window
    {
        public PaymentWindow()
        {
            InitializeComponent();
        }

        public PaymentWindow(int maHoaDon, int soTien, string thongTinThanhToan)
            : this()
        {
            MaHoaDonTextBox.Text = maHoaDon.ToString();
            SoTienTextBox.Text = soTien.ToString();
            MaHoaDonTextBox.IsReadOnly = true;
            MaHoaDonTextBox.Opacity = 0.75;

            if (!string.IsNullOrWhiteSpace(thongTinThanhToan))
            {
                PaymentInfoTextBlock.Text = thongTinThanhToan;
                PaymentInfoBorder.Visibility = Visibility.Visible;
            }
        }

        public int MaHoaDon { get; private set; }
        public int SoTien { get; private set; }
        public string PhuongThuc => (PhuongThucComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Bank transfer";
        public string MaChungTu => string.Empty;
        public string GhiChu => GhiChuTextBox.Text;

        private void PayButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(MaHoaDonTextBox.Text, out var maHoaDon) || maHoaDon <= 0)
            {
                MessageBox.Show("Ma hoa don khong hop le.", "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(SoTienTextBox.Text, out var soTien) || soTien <= 0)
            {
                MessageBox.Show("So tien khong hop le.", "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsPrintableAscii(GhiChuTextBox.Text))
            {
                MessageBox.Show("Ghi chu chi duoc dung tieng Anh khong dau va ky tu ASCII.", "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MaHoaDon = maHoaDon;
            SoTien = soTien;
            DialogResult = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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
