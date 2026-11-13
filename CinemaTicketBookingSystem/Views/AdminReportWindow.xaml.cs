using CinemaTicketBookingSystem.Entities;
using CinemaTicketBookingSystem.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CinemaTicketBookingSystem.Views
{
    public partial class AdminReportWindow : Window
    {
        public class TopMovieStatItem
        {
            public int Rank { get; set; }
            public string MovieTitle { get; set; } = string.Empty;
            public int TicketsCount { get; set; }
            public decimal Revenue { get; set; }
            public string RevenueString => $"{Revenue:N0} đ";
            public double Percentage { get; set; }
            public string PercentageString => $"{Percentage:0.1}%";
        }

        public class UserDisplayItem
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool Status { get; set; }
            public string StatusText => Status ? "Hoạt động" : "Đã khóa";
            public string StatusBg => Status ? "#14532D" : "#7F1D1D";
            public string StatusFg => Status ? "#86EFAC" : "#FCA5A5";
        }

        private Booking? _currentCheckInBooking;
        private List<UserDisplayItem> _allUsers = new();
        private UserDisplayItem? _selectedUser;

        public AdminReportWindow()
        {
            InitializeComponent();
            Loaded += AdminReportWindow_Loaded;
        }

        private void AdminReportWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtAdminName.Text = $"Admin: {UserSession.CurrentUser?.FullName ?? "Quản trị viên"}";

            // Initialize Report Dates: Last 30 days to Today
            dpFromDate.SelectedDate = DateTime.Today.AddDays(-30);
            dpToDate.SelectedDate = DateTime.Today;

            LoadRecentTicketsForQuickCheckIn();
            RunReport();
            LoadUsers();
        }

        #region Navigation
        private void NavToMovies_Click(object sender, RoutedEventArgs e)
        {
            var win = new AdminMovieWindow();
            win.Show();
            Close();
        }

        private void NavToShowtimes_Click(object sender, RoutedEventArgs e)
        {
            var win = new AdminShowtimeWindow();
            win.Show();
            Close();
        }

        private void NavToFood_Click(object sender, RoutedEventArgs e)
        {
            var win = new AdminFoodWindow();
            win.Show();
            Close();
        }

        private void BtnCustomerHome_Click(object sender, RoutedEventArgs e)
        {
            var home = new CustomerHomeWindow();
            home.Show();
            Close();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.CurrentUser = null;
            var login = new LoginWindow();
            login.Show();
            Close();
        }
        #endregion

        #region QR Check-in (TASK-20)
        private void LoadRecentTicketsForQuickCheckIn()
        {
            spRecentTickets.Children.Clear();
            try
            {
                using var db = new CinemaManagementDbContext();
                var recentBookings = db.Bookings
                    .Include(b => b.Showtime.Movie)
                    .OrderByDescending(b => b.BookingId)
                    .Take(4)
                    .ToList();

                foreach (var b in recentBookings)
                {
                    var btn = new Button
                    {
                        Content = $"🎟️ {b.QrcodeString} - {b.Showtime?.Movie?.Title ?? "Phim"}",
                        Style = (Style)FindResource("GhostBtnStyle"),
                        Height = 34,
                        Margin = new Thickness(0, 0, 0, 6),
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Tag = b.QrcodeString
                    };

                    btn.Click += (s, e) =>
                    {
                        txtCheckInCode.Text = b.QrcodeString;
                        PerformCheckInSearch(b.QrcodeString);
                    };

                    spRecentTickets.Children.Add(btn);
                }
            }
            catch
            {
                // Ignore
            }
        }

        private void TxtCheckInCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformCheckInSearch(txtCheckInCode.Text.Trim());
            }
        }

        private void BtnCheckTicket_Click(object sender, RoutedEventArgs e)
        {
            PerformCheckInSearch(txtCheckInCode.Text.Trim());
        }

        private void PerformCheckInSearch(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                CinemaMessageBox.Show("Vui lòng nhập mã vé cần kiểm tra!", "Thiếu mã vé", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCheckInCode.Focus();
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                var booking = db.Bookings
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Include(b => b.Showtime).ThenInclude(s => s.Room)
                    .Include(b => b.Tickets)
                    .Include(b => b.BookingFoods).ThenInclude(bf => bf.Food)
                    .Include(b => b.User)
                    .FirstOrDefault(b => b.QrcodeString.ToLower() == code.ToLower());

                if (booking == null)
                {
                    pnlNoTicket.Visibility = Visibility.Visible;
                    pnlTicketResult.Visibility = Visibility.Collapsed;
                    CinemaMessageBox.Show($"Không tìm thấy vé với mã '{code}' trong hệ thống!\nVui lòng kiểm tra lại mã QR.", "Vé không tồn tại", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _currentCheckInBooking = booking;
                pnlNoTicket.Visibility = Visibility.Collapsed;
                pnlTicketResult.Visibility = Visibility.Visible;

                // Populate Result Details
                txtResultMovie.Text = booking.Showtime?.Movie?.Title?.ToUpper() ?? "VÉ XEM PHIM";
                txtResultCode.Text = booking.QrcodeString;
                txtResultCustomer.Text = $"{booking.User?.FullName ?? "Khách"} ({booking.User?.Phone ?? ""})";
                txtResultRoom.Text = booking.Showtime?.Room?.RoomName ?? "Phòng 01";
                txtResultShowtime.Text = $"{booking.Showtime?.StartTime:HH:mm • dd/MM/yyyy}";

                var seats = booking.Tickets.Select(t => t.SeatCode).OrderBy(s => s).ToList();
                txtResultSeats.Text = seats.Any() ? string.Join(", ", seats) : "Không xác định";

                var foods = booking.BookingFoods.Select(bf => $"{bf.Food?.FoodName ?? "Món"} (x{bf.Quantity})").ToList();
                txtResultFoods.Text = foods.Any() ? string.Join(", ", foods) : "Không có";

                txtResultAmount.Text = $"{booking.TotalAmount:N0} đ";

                // Hiển thị Phương thức thanh toán & Cảnh báo thu tiền
                string payMethod = booking.PaymentMethod ?? "VNPAY";
                bool isCash = payMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase);

                if (isCash)
                {
                    // Tiền mặt tại quầy - Chưa thu tiền
                    bdrResultPaymentBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#451A1A"));
                    txtResultPaymentMethod.Text = "💵 TIỀN MẶT (CHƯA THU)";
                    txtResultPaymentMethod.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));

                    bdrCashAlert.Visibility = Visibility.Visible;
                    txtCashAlertDetail.Text = $"Đơn vé thanh toán tại quầy! Vui lòng thu đúng số tiền: {booking.TotalAmount:N0} đ trước khi soát vé!";
                }
                else if (payMethod.Equals("VietQR", StringComparison.OrdinalIgnoreCase))
                {
                    // VietQR - Đã chuyển khoản
                    bdrResultPaymentBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A"));
                    txtResultPaymentMethod.Text = "⚡ VietQR (Đã chuyển khoản)";
                    txtResultPaymentMethod.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#93C5FD"));

                    bdrCashAlert.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // VNPAY - Đã thanh toán online
                    bdrResultPaymentBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14532D"));
                    txtResultPaymentMethod.Text = "💳 VNPAY (Đã thanh toán online)";
                    txtResultPaymentMethod.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));

                    bdrCashAlert.Visibility = Visibility.Collapsed;
                }

                // Check-in status validation
                if (!booking.IsCheckedIn)
                {
                    // Valid and not checked in yet
                    bdrCheckInBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14532D"));
                    txtCheckInIcon.Text = "✅";
                    txtCheckInTitle.Text = "VÉ HỢP LỆ - CHƯA SỬ DỤNG";
                    txtCheckInTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    txtCheckInSub.Text = isCash 
                        ? $"Đơn vé chưa thu tiền mặt! Vui lòng thu {booking.TotalAmount:N0} đ rồi bấm xác nhận."
                        : "Khách hàng hợp lệ. Vui lòng bấm 'Xác nhận soát vé' để cho khách vào phòng chiếu.";
                    txtCheckInSub.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A7F3D0"));

                    btnConfirmCheckIn.IsEnabled = true;
                    btnConfirmCheckIn.Visibility = Visibility.Visible;
                    btnConfirmCheckIn.Content = isCash
                        ? $"💵 ĐÃ THU {booking.TotalAmount:N0} Đ & CHO VÀO PHÒNG"
                        : "✓ XÁC NHẬN SOÁT VÉ (CHO VÀO PHÒNG)";
                }
                else
                {
                    // Warning: Already checked in!
                    bdrCheckInBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                    txtCheckInIcon.Text = "⚠️";
                    txtCheckInTitle.Text = "CẢNH BÁO: VÉ NÀY ĐÃ ĐƯỢC SỬ DỤNG TRƯỚC ĐÓ!";
                    txtCheckInTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
                    txtCheckInSub.Text = "Mã vé này đã hoàn tất thủ tục soát vé vào rạp. Vui lòng kiểm tra lại để tránh gian lận.";
                    txtCheckInSub.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECACA"));

                    btnConfirmCheckIn.IsEnabled = false;
                    btnConfirmCheckIn.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi kiểm tra vé: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnConfirmCheckIn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCheckInBooking == null) return;

            try
            {
                using var db = new CinemaManagementDbContext();
                var b = db.Bookings.Find(_currentCheckInBooking.BookingId);
                if (b != null)
                {
                    b.IsCheckedIn = true;
                    db.SaveChanges();

                    CinemaMessageBox.Show(
                        $"🎉 SOÁT VÉ THÀNH CÔNG!\n\nĐã xác nhận cho khách vào phòng chiếu. Chúc quý khách xem phim vui vẻ!",
                        "Soát vé hoàn tất",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Re-query to show updated state
                    PerformCheckInSearch(_currentCheckInBooking.QrcodeString);
                    LoadRecentTicketsForQuickCheckIn();
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi cập nhật soát vé: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Revenue Statistics (TASK-20)
        private void BtnRunReport_Click(object sender, RoutedEventArgs e)
        {
            RunReport();
        }

        private void BtnReportToday_Click(object sender, RoutedEventArgs e)
        {
            dpFromDate.SelectedDate = DateTime.Today;
            dpToDate.SelectedDate = DateTime.Today;
            RunReport();
        }

        private void BtnReportThisMonth_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Today;
            dpFromDate.SelectedDate = new DateTime(now.Year, now.Month, 1);
            dpToDate.SelectedDate = now;
            RunReport();
        }

        private void BtnReportAllTime_Click(object sender, RoutedEventArgs e)
        {
            dpFromDate.SelectedDate = new DateTime(2020, 1, 1);
            dpToDate.SelectedDate = DateTime.Today.AddDays(1);
            RunReport();
        }

        private void RunReport()
        {
            if (!dpFromDate.SelectedDate.HasValue || !dpToDate.SelectedDate.HasValue)
            {
                CinemaMessageBox.Show("Vui lòng chọn ngày bắt đầu và ngày kết thúc để thống kê!", "Thiếu thời gian", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var from = dpFromDate.SelectedDate.Value.Date;
            var to = dpToDate.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1); // End of day

            // Validation: FromDate <= ToDate
            if (from > to)
            {
                CinemaMessageBox.Show("Ngày bắt đầu (Từ ngày) phải nhỏ hơn hoặc bằng ngày kết thúc (Đến ngày)!", "Khoảng thời gian không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                var bookings = db.Bookings
                    .Include(b => b.Tickets)
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Where(b => b.BookingDate >= from && b.BookingDate <= to)
                    .ToList();

                decimal totalRevenue = bookings.Sum(b => b.TotalAmount);
                int totalTickets = bookings.SelectMany(b => b.Tickets).Count();
                int totalOrders = bookings.Count;

                txtKpiRevenue.Text = $"{totalRevenue:N0} đ";
                txtKpiTickets.Text = $"{totalTickets:N0} vé";
                txtKpiOrders.Text = $"{totalOrders:N0} đơn";

                // Group by Movie for Top Movies Ranking
                var movieGroups = bookings
                    .Where(b => b.Showtime?.Movie != null)
                    .GroupBy(b => b.Showtime.Movie)
                    .Select(g => new
                    {
                        Movie = g.Key,
                        Revenue = g.Sum(b => b.TotalAmount),
                        Tickets = g.SelectMany(b => b.Tickets).Count()
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToList();

                int rank = 1;
                var topMovies = movieGroups.Select(mg => new TopMovieStatItem
                {
                    Rank = rank++,
                    MovieTitle = mg.Movie.Title,
                    TicketsCount = mg.Tickets,
                    Revenue = mg.Revenue,
                    Percentage = totalRevenue > 0 ? (double)(mg.Revenue / totalRevenue * 100m) : 0
                }).ToList();

                dgTopMovies.ItemsSource = topMovies;
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tính toán báo cáo thống kê: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region User Management (TASK-17)
        private void LoadUsers()
        {
            try
            {
                using var db = new CinemaManagementDbContext();
                var users = db.Users.OrderBy(u => u.UserId).ToList();

                _allUsers = users.Select(u => new UserDisplayItem
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName,
                    Phone = u.Phone ?? string.Empty,
                    Role = u.Role,
                    Password = u.Password,
                    Status = u.Status
                }).ToList();

                ApplyUserFilter();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải danh sách người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyUserFilter()
        {
            if (!IsLoaded || dgUsers == null || txtSearchUser == null || txtUserStats == null) return;

            string kw = txtSearchUser.Text.Trim();
            var query = _allUsers.AsEnumerable();

            if (!string.IsNullOrEmpty(kw))
            {
                query = query.Where(u => VietnameseHelper.ContainsUnaccented(u.FullName, kw) || u.Username.ToLower().Contains(kw.ToLower()) || u.Phone.Contains(kw));
            }

            if (cbFilterRole?.SelectedItem is ComboBoxItem cbi)
            {
                string roleFilter = cbi.Content?.ToString() ?? "";
                if (roleFilter.Contains("Staff"))
                {
                    query = query.Where(u => u.Role == "Staff");
                }
                else if (roleFilter.Contains("Admin"))
                {
                    query = query.Where(u => u.Role == "Admin");
                }
                else if (roleFilter.Contains("Customer"))
                {
                    query = query.Where(u => u.Role == "Customer");
                }
            }

            var filtered = query.ToList();
            dgUsers.ItemsSource = filtered;
            txtUserStats.Text = $"Hiển thị: {filtered.Count} / {_allUsers.Count} tài khoản";
        }

        private void TxtSearchUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyUserFilter();
        }

        private void CbFilterRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyUserFilter();
        }

        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsers.SelectedItem is not UserDisplayItem item) return;

            _selectedUser = item;
            txtUserFullName.Text = item.FullName;
            txtUserUsername.Text = item.Username;
            txtUserPhone.Text = item.Phone;
            txtUserPassword.Text = item.Password;
            chkUserStatus.IsChecked = item.Status;

            cbUserRole.SelectedIndex = item.Role switch
            {
                "Staff" => 1,
                "Admin" => 2,
                _ => 0
            };
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateUserForm(out string fullName, out string username, out string phone, out string password, out string role))
            {
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                if (db.Users.Any(u => u.Username.ToLower() == username.ToLower()))
                {
                    CinemaMessageBox.Show($"Tên đăng nhập '{username}' đã được sử dụng!", "Trùng tài khoản", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtUserUsername.Focus();
                    return;
                }

                var user = new User
                {
                    FullName = fullName,
                    Username = username,
                    Phone = phone,
                    Password = password,
                    Role = role,
                    Status = chkUserStatus.IsChecked == true
                };

                db.Users.Add(user);
                db.SaveChanges();

                CinemaMessageBox.Show($"Thêm tài khoản '{username}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadUsers();
                BtnClearUserForm_Click(sender, e);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi thêm người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdateUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 tài khoản từ bảng để sửa!", "Chưa chọn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateUserForm(out string fullName, out string username, out string phone, out string password, out string role))
            {
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                if (db.Users.Any(u => u.UserId != _selectedUser.UserId && u.Username.ToLower() == username.ToLower()))
                {
                    CinemaMessageBox.Show($"Tên đăng nhập '{username}' đã bị trùng!", "Trùng tài khoản", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = db.Users.Find(_selectedUser.UserId);
                if (user != null)
                {
                    user.FullName = fullName;
                    user.Username = username;
                    user.Phone = phone;
                    user.Password = password;
                    user.Role = role;
                    user.Status = chkUserStatus.IsChecked == true;

                    db.SaveChanges();

                    CinemaMessageBox.Show($"Cập nhật tài khoản '{username}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToggleStatusUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 tài khoản từ bảng để Khóa/Mở khóa!", "Chưa chọn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TASK-17 Critical Rule: Cannot lock or delete currently logged-in Admin
            if (UserSession.CurrentUser != null && _selectedUser.UserId == UserSession.CurrentUser.UserId)
            {
                CinemaMessageBox.Show(
                    "⚠️ QUY TẮC BẢO MẬT: Bạn không thể tự Khóa hoặc Xóa tài khoản Quản trị viên (Admin) đang đăng nhập của chính mình!",
                    "Hành động bị chặn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Hand);
                return;
            }

            string action = _selectedUser.Status ? "KHÓA" : "MỞ KHÓA";
            var result = CinemaMessageBox.Show(
                $"Bạn có chắc chắn muốn {action} tài khoản '{_selectedUser.Username}' ({_selectedUser.FullName})?",
                "Xác nhận thay đổi trạng thái",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new CinemaManagementDbContext();
                    var user = db.Users.Find(_selectedUser.UserId);
                    if (user != null)
                    {
                        user.Status = !user.Status;
                        db.SaveChanges();
                        LoadUsers();
                    }
                }
                catch (Exception ex)
                {
                    CinemaMessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClearUserForm_Click(object sender, RoutedEventArgs e)
        {
            _selectedUser = null;
            dgUsers.SelectedItem = null;
            txtUserFullName.Text = string.Empty;
            txtUserUsername.Text = string.Empty;
            txtUserPhone.Text = string.Empty;
            txtUserPassword.Text = string.Empty;
            cbUserRole.SelectedIndex = 0;
            chkUserStatus.IsChecked = true;
        }

        private bool ValidateUserForm(out string fullName, out string username, out string phone, out string password, out string role)
        {
            fullName = txtUserFullName.Text.Trim();
            username = txtUserUsername.Text.Trim();
            phone = txtUserPhone.Text.Trim();
            password = txtUserPassword.Text.Trim();
            role = cbUserRole.SelectedIndex switch
            {
                1 => "Staff",
                2 => "Admin",
                _ => "Customer"
            };

            if (string.IsNullOrWhiteSpace(fullName))
            {
                CinemaMessageBox.Show("Vui lòng nhập họ và tên người dùng!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUserFullName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                CinemaMessageBox.Show("Tên đăng nhập phải có ít nhất 3 ký tự!", "Tên đăng nhập không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUserUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(phone) || phone.Length != 10 || !phone.All(char.IsDigit))
            {
                CinemaMessageBox.Show("Số điện thoại phải có đúng 10 chữ số (VD: 0912345678)!", "Số điện thoại không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUserPhone.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 3)
            {
                CinemaMessageBox.Show("Mật khẩu phải có ít nhất 3 ký tự!", "Mật khẩu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUserPassword.Focus();
                return false;
            }

            return true;
        }

        private void BtnResetBookingData_Click(object sender, RoutedEventArgs e)
        {
            var confirm = CinemaMessageBox.Show(
                "BẠN CÓ CHẮC CHẮN MUỐN RESET TOÀN BỘ DỮ LIỆU ĐẶT VÉ?\n\n" +
                "• Toàn bộ vé đã đặt sẽ bị xóa (tất cả các ghế trên sơ đồ sẽ trở về trạng thái trống).\n" +
                "• Lịch sử đặt vé và danh sách hóa đơn sẽ được xóa sạch.\n" +
                "• Số lượng tồn kho bắp nước được khôi phục về 50 phần.\n" +
                "• Danh sách Phim, Phòng chiếu và Suất chiếu vẫn được giữ nguyên vẹn.\n\n" +
                "Thao tác này giúp bạn đưa hệ thống về trạng thái ban đầu để chuẩn bị demo mới!",
                "Xác nhận Reset Hệ Thống",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var db = new CinemaManagementDbContext();
                db.Database.ExecuteSqlRaw(@"
                    DELETE FROM BookingFood;
                    DELETE FROM Ticket;
                    DELETE FROM Booking;
                    DBCC CHECKIDENT ('BookingFood', RESEED, 0);
                    DBCC CHECKIDENT ('Ticket', RESEED, 0);
                    DBCC CHECKIDENT ('Booking', RESEED, 0);
                    UPDATE FoodAndDrink SET Stock = 50, [Status] = 1;
                ");

                CinemaMessageBox.Show(
                    "Đã reset toàn bộ thông tin đặt vé thành công!\n\nHiện tại hệ thống không còn bất kỳ đơn vé nào, toàn bộ ghế đã trống và lịch sử đặt vé đã sạch hoàn toàn.",
                    "Reset thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                RunReport();
                LoadRecentTicketsForQuickCheckIn();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi khi reset dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
