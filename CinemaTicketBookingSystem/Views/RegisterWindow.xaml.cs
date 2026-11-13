using CinemaTicketBookingSystem.Entities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CinemaTicketBookingSystem.Views
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        public string RegisteredUsername { get; private set; } = string.Empty;

        public RegisterWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => txtFullName.Focus();
        }

        private void TxtField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformRegister();
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            PerformRegister();
        }

        private void PerformRegister()
        {
            bdrError.Visibility = Visibility.Collapsed;
            bdrSuccess.Visibility = Visibility.Collapsed;

            var fullName = txtFullName.Text.Trim();
            var username = txtUsername.Text.Trim();
            var phone = txtPhone.Text.Trim();
            var password = txtPassword.Password;
            var confirmPassword = txtConfirmPassword.Password;

            // 1. Validate Non-empty
            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError("Vui lòng nhập họ và tên của bạn!");
                txtFullName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Vui lòng nhập tên đăng nhập!");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                ShowError("Vui lòng nhập số điện thoại liên hệ!");
                txtPhone.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập mật khẩu!");
                txtPassword.Focus();
                return;
            }

            if (string.IsNullOrEmpty(confirmPassword))
            {
                ShowError("Vui lòng nhập xác nhận mật khẩu!");
                txtConfirmPassword.Focus();
                return;
            }

            // 2. Validate Formats & Lengths
            if (username.Length < 3)
            {
                ShowError("Tên đăng nhập phải có ít nhất 3 ký tự!");
                txtUsername.Focus();
                return;
            }

            if (username.Any(char.IsWhiteSpace))
            {
                ShowError("Tên đăng nhập không được chứa khoảng trắng!");
                txtUsername.Focus();
                return;
            }

            // Phone must be exactly 10 digits
            if (phone.Length != 10 || !phone.All(char.IsDigit))
            {
                ShowError("Số điện thoại không hợp lệ! Vui lòng nhập đúng 10 chữ số (VD: 0912345678).");
                txtPhone.Focus();
                return;
            }

            if (password.Length < 3)
            {
                ShowError("Mật khẩu phải có độ dài từ 3 ký tự trở lên!");
                txtPassword.Focus();
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Mật khẩu xác nhận không khớp! Vui lòng kiểm tra lại.");
                txtConfirmPassword.Focus();
                return;
            }

            if (chkAgree.IsChecked != true)
            {
                ShowError("Vui lòng đánh dấu đồng ý với Điều khoản dịch vụ và Chính sách của KPID CINEMA!");
                return;
            }

            // 3. Database operations
            try
            {
                using var db = new CinemaManagementDbContext();

                // Check username duplicate
                if (db.Users.Any(u => u.Username.ToLower() == username.ToLower()))
                {
                    ShowError($"Tên đăng nhập '{username}' đã được sử dụng. Vui lòng chọn tên đăng nhập khác!");
                    txtUsername.Focus();
                    return;
                }

                // Check phone duplicate
                if (db.Users.Any(u => u.Phone == phone))
                {
                    ShowError("Số điện thoại này đã được đăng ký tài khoản khác trên hệ thống!");
                    txtPhone.Focus();
                    return;
                }

                // Create user with default Role = "Customer", Status = true
                var newUser = new User
                {
                    FullName = fullName,
                    Username = username,
                    Phone = phone,
                    Password = password,
                    Role = "Customer",
                    Status = true
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                RegisteredUsername = username;
                ShowSuccess("Chúc mừng bạn! Đăng ký tài khoản KPID CINEMA thành công.");

                btnRegister.IsEnabled = false;

                // Brief delay so user sees feedback before closing
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(800)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    DialogResult = true;
                    Close();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi trong quá trình đăng ký tài khoản: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            txtErrorMsg.Text = message;
            bdrError.Visibility = Visibility.Visible;
            bdrSuccess.Visibility = Visibility.Collapsed;
        }

        private void ShowSuccess(string message)
        {
            txtSuccessMsg.Text = message;
            bdrSuccess.Visibility = Visibility.Visible;
            bdrError.Visibility = Visibility.Collapsed;
        }

        private void BtnOpenLogin_Click(object sender, RoutedEventArgs e)
        {
            if (Owner is LoginWindow)
            {
                DialogResult = false;
                Close();
            }
            else
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
        }
    }
}
