using CinemaTicketBookingSystem.Entities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CinemaTicketBookingSystem.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => txtUsername.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        private void TxtField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformLogin();
            }
        }

        private void PerformLogin()
        {
            bdrError.Visibility = Visibility.Collapsed;

            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password.Trim();

            // 1. Validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!");
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                
                // Case-insensitive username lookup
                var user = db.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());

                if (user == null)
                {
                    ShowError($"Tài khoản không tồn tại!");
                    return;
                }

                // Password verification
                bool isPasswordValid = (user.Password == password);

                if (!isPasswordValid)
                {
                    ShowError("Mật khẩu không chính xác! Vui lòng thử lại.");
                    return;
                }

                // 2. Check Account Status
                if (!user.Status)
                {
                    ShowError("Tài khoản của bạn đã bị khóa! Vui lòng liên hệ ban quản lý KPID CINEMA.");
                    return;
                }

                // 3. Save to Global Session
                UserSession.CurrentUser = user;

                // 4. Role Navigation (TASK-16 requirement)
                if (user.Role == "Admin")
                {
                    // Open Admin Window directly
                    var adminWin = new AdminMovieWindow();
                    adminWin.Show();
                    if (Owner is Window ownerWin)
                    {
                        ownerWin.Close();
                    }
                    Close();
                    return;
                }

                if (user.Role == "Staff")
                {
                    // Open Staff Check-in Window directly
                    var staffWin = new StaffTicketScanWindow();
                    staffWin.Show();
                    if (Owner is Window ownerWin)
                    {
                        ownerWin.Close();
                    }
                    Close();
                    return;
                }

                // If Customer opened from CustomerHomeWindow
                if (Owner is CustomerHomeWindow)
                {
                    DialogResult = true;
                    Close();
                    return;
                }

                // Standalone customer login
                var homeWin = new CustomerHomeWindow();
                homeWin.Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            txtErrorMsg.Text = message;
            bdrError.Visibility = Visibility.Visible;
        }

        private void BtnOpenRegister_Click(object sender, RoutedEventArgs e)
        {
            var regWindow = new RegisterWindow();
            regWindow.Owner = this;
            if (regWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(regWindow.RegisteredUsername))
                {
                    txtUsername.Text = regWindow.RegisteredUsername;
                    txtPassword.Focus();
                }
            }
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            UserSession.CurrentUser = null;

            if (Owner is CustomerHomeWindow)
            {
                Close();
                return;
            }

            var homeWin = new CustomerHomeWindow();
            homeWin.Show();
            Close();
        }
    }
}
