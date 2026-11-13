using CinemaTicketBookingSystem.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CinemaTicketBookingSystem.Views
{
    public partial class StaffTicketScanWindow : Window
    {
        private Booking? _currentCheckInBooking;

        public StaffTicketScanWindow()
        {
            InitializeComponent();
            Loaded += StaffTicketScanWindow_Loaded;
        }

        private void StaffTicketScanWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtStaffName.Text = $"Nhân viên: {UserSession.CurrentUser?.FullName ?? "Soát vé"}";
            LoadRecentTicketsForQuickCheckIn();
            txtCheckInCode.Focus();
        }

        private void LoadRecentTicketsForQuickCheckIn()
        {
            spRecentTickets.Children.Clear();
            try
            {
                using var db = new CinemaManagementDbContext();
                var recentBookings = db.Bookings
                    .Include(b => b.Showtime.Movie)
                    .OrderByDescending(b => b.BookingId)
                    .Take(5)
                    .ToList();

                foreach (var b in recentBookings)
                {
                    string statusIcon = b.IsCheckedIn ? "🟢 (Đã soát)" : "🟡 (Chưa soát)";
                    var btn = new Button
                    {
                        Content = $"{statusIcon} {b.QrcodeString} - {b.Showtime?.Movie?.Title ?? "Phim"}",
                        Style = (Style)FindResource("GhostBtnStyle"),
                        Height = 36,
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
                // Ignore fallback
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

        private void BtnClearCode_Click(object sender, RoutedEventArgs e)
        {
            txtCheckInCode.Clear();
            _currentCheckInBooking = null;
            pnlTicketResult.Visibility = Visibility.Collapsed;
            pnlNoTicket.Visibility = Visibility.Visible;
            txtCheckInCode.Focus();
        }

        private void PerformCheckInSearch(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                CinemaMessageBox.Show("Vui lòng nhập hoặc quét mã vé cần kiểm tra!", "Thiếu mã vé", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    .OrderByDescending(b => b.BookingId)
                    .FirstOrDefault(b => b.QrcodeString.ToLower() == code.ToLower() || (b.User != null && b.User.Phone == code));

                if (booking == null)
                {
                    pnlNoTicket.Visibility = Visibility.Visible;
                    pnlTicketResult.Visibility = Visibility.Collapsed;
                    CinemaMessageBox.Show(
                        $"KHÔNG TÌM THẤY VÉ VỚI MÃ '{code}' TRONG HỆ THỐNG!\n\nVui lòng kiểm tra lại mã QR hoặc hướng dẫn khách hàng liên hệ quầy vé.",
                        "Vé Không Tồn Tại",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    txtCheckInCode.SelectAll();
                    txtCheckInCode.Focus();
                    return;
                }

                _currentCheckInBooking = booking;
                pnlNoTicket.Visibility = Visibility.Collapsed;
                pnlTicketResult.Visibility = Visibility.Visible;

                // Bind Details
                txtResultMovie.Text = booking.Showtime?.Movie?.Title?.ToUpper() ?? "VÉ XEM PHIM";
                txtResultCode.Text = booking.QrcodeString;
                txtResultCustomer.Text = $"{booking.User?.FullName ?? "Khách"} ({booking.User?.Phone ?? ""})";
                txtResultRoom.Text = booking.Showtime?.Room?.RoomName ?? "Phòng 01";
                txtResultShowtime.Text = $"{booking.Showtime?.StartTime:HH:mm • dd/MM/yyyy}";

                var seats = booking.Tickets.Select(t => t.SeatCode).OrderBy(s => s).ToList();
                txtResultSeats.Text = seats.Any() ? string.Join(", ", seats) : "Không xác định";

                var foods = booking.BookingFoods.Select(bf => $"{bf.Food?.FoodName ?? "Món"} (x{bf.Quantity})").ToList();
                txtResultFoods.Text = foods.Any() ? string.Join(", ", foods) : "Không kèm bắp nước";

                txtResultAmount.Text = $"{booking.TotalAmount:N0} đ";

                // Hiển thị Phương thức thanh toán & Cảnh báo thu tiền
                string payMethod = booking.PaymentMethod ?? "VNPAY";
                bool isCash = payMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase);

                if (isCash)
                {
                    // Tiền mặt tại quầy - Chưa thu tiền
                    bdrPaymentMethodBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#451A1A"));
                    txtResultPaymentMethod.Text = "💵 TIỀN MẶT (CHƯA THU)";
                    txtResultPaymentMethod.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));

                    bdrCashAlert.Visibility = Visibility.Visible;
                    txtCashAlertDetail.Text = $"Nhân viên vui lòng thu đúng số tiền: {booking.TotalAmount:N0} đ trước khi xác nhận soát vé!";
                }
                else if (payMethod.Equals("VietQR", StringComparison.OrdinalIgnoreCase))
                {
                    // VietQR - Đã chuyển khoản
                    bdrPaymentMethodBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A"));
                    txtResultPaymentMethod.Text = "⚡ VietQR (Đã chuyển khoản)";
                    txtResultPaymentMethod.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#93C5FD"));

                    bdrCashAlert.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // VNPAY - Đã thanh toán online
                    bdrPaymentMethodBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14532D"));
                    txtResultPaymentMethod.Text = "💳 VNPAY (Đã thanh toán online)";
                    txtResultPaymentMethod.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));

                    bdrCashAlert.Visibility = Visibility.Collapsed;
                }

                // Kiểm tra trạng thái đã soát hay chưa
                if (!booking.IsCheckedIn)
                {
                    // HỢP LỆ - CHƯA SỬ DỤNG
                    bdrCheckInBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14532D"));
                    txtCheckInIcon.Text = "✅";
                    txtCheckInTitle.Text = "VÉ HỢP LỆ - CHƯA SỬ DỤNG";
                    txtCheckInTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    txtCheckInSub.Text = isCash 
                        ? $"Đơn vé chưa thu tiền mặt! Vui lòng thu {booking.TotalAmount:N0} đ rồi bấm xác nhận."
                        : "Khách hàng hợp lệ. Vui lòng bấm nút 'Xác nhận soát vé' để cho khách vào phòng chiếu.";
                    txtCheckInSub.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A7F3D0"));

                    btnConfirmCheckIn.IsEnabled = true;
                    btnConfirmCheckIn.Visibility = Visibility.Visible;
                    btnConfirmCheckIn.Content = isCash
                        ? $"💵 ĐÃ THU {booking.TotalAmount:N0} Đ & XÁC NHẬN VÀO RẠP"
                        : "✔ XÁC NHẬN SOÁT VÉ VÀO PHÒNG CHIẾU";
                }
                else
                {
                    // CẢNH BÁO - ĐÃ SOÁT RỒI
                    bdrCheckInBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                    txtCheckInIcon.Text = "⚠️";
                    txtCheckInTitle.Text = "CẢNH BÁO: VÉ NÀY ĐÃ ĐƯỢC SOÁT TRƯỚC ĐÓ!";
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
                // Thực thi lệnh cập nhật nguyên tử (Atomic Update) - Triệt tiêu 100% Race condition khi nhiều máy cùng soát 1 vé
                int rowsAffected = db.Database.ExecuteSqlRaw(
                    "UPDATE Booking SET IsCheckedIn = 1 WHERE BookingId = {0} AND IsCheckedIn = 0",
                    _currentCheckInBooking.BookingId);

                if (rowsAffected == 0)
                {
                    CinemaMessageBox.Show(
                        "Cảnh báo: Vé này vừa mới được soát thành công bởi nhân viên/cửa soát vé khác ngay tức thì!\n\nHệ thống đã chặn để ngăn chặn việc tái sử dụng vé trùng.",
                        "Vé đã được soát (Bảo vệ Race Condition)",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    PerformCheckInSearch(_currentCheckInBooking.QrcodeString);
                    return;
                }

                bdrCheckInBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"));
                txtCheckInIcon.Text = "🎉";
                txtCheckInTitle.Text = "ĐÃ SOÁT VÉ THÀNH CÔNG!";
                txtCheckInTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EE7B7"));
                txtCheckInSub.Text = "Vé đã được chuyển sang trạng thái ĐÃ VÀO RẠP. Chúc khách hàng xem phim vui vẻ!";
                txtCheckInSub.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));

                btnConfirmCheckIn.IsEnabled = false;
                btnConfirmCheckIn.Visibility = Visibility.Collapsed;

                CinemaMessageBox.Show(
                    $"ĐÃ SOÁT VÉ THÀNH CÔNG!\n\nMã vé: {_currentCheckInBooking.QrcodeString}\nGhế: {txtResultSeats.Text}\nPhòng: {txtResultRoom.Text}\n\nKhách hàng đã được phép vào phòng chiếu.",
                    "Hoàn tất soát vé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadRecentTicketsForQuickCheckIn();

                // Chuẩn bị cho lần quét tiếp theo
                txtCheckInCode.Clear();
                txtCheckInCode.Focus();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi trong quá trình xác nhận soát vé: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.CurrentUser = null;
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}

