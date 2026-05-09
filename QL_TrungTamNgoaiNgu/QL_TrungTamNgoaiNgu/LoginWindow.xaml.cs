using QL_TrungTamNgoaiNgu.Services;
using System;
using System.Windows;

namespace QL_TrungTamNgoaiNgu
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService = new AuthService();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                LoginMessageTextBlock.Text = "Dang kiem tra tai khoan...";
                var roleGroup = ((System.Windows.Controls.ComboBoxItem)RoleComboBox.SelectedItem).Tag.ToString();
                var session = await _authService.LoginAsync(EmailTextBox.Text, PasswordBox.Password, roleGroup);
                var mainWindow = new MainWindow(session);
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                LoginMessageTextBlock.Text = ex.Message;
            }
        }

        private async void ResetPasswordButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
                {
                    throw new InvalidOperationException("Mat khau moi khong duoc de trong.");
                }

                ResetMessageTextBlock.Text = "Dang dat lai mat khau...";
                await _authService.ResetPasswordAsync(ResetEmailTextBox.Text, ResetPhoneTextBox.Text, NewPasswordBox.Password);
                ResetMessageTextBlock.Text = "Da cap nhat mat khau moi. Ban co the dang nhap lai.";
            }
            catch (Exception ex)
            {
                ResetMessageTextBlock.Text = ex.Message;
            }
        }
    }
}
