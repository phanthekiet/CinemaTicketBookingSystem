using CinemaTicketBookingSystem.Entities;
using CinemaTicketBookingSystem.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CinemaTicketBookingSystem.Views
{
    public partial class CustomerHistoryWindow : Window
    {
        public class BookingHistoryItem
        {
            public int BookingId { get; set; }
            public string QrcodeString { get; set; } = string.Empty;
            public string MovieTitle { get; set; } = string.Empty;
            public string BuyerInfo { get; set; } = string.Empty;
            public string RoomName { get; set; } = string.Empty;
            public string ShowtimeString { get; set; } = string.Empty;
            public string SeatsString { get; set; } = string.Empty;
            public string TotalAmountString { get; set; } = string.Empty;
            public string BookingDateString { get; set; } = string.Empty;
            public string StatusText { get; set; } = string.Empty;
            public string StatusBg { get; set; } = string.Empty;
            public string StatusFg { get; set; } = string.Empty;
        }

        public CustomerHistoryWindow()
        {
            InitializeComponent();
            Loaded += CustomerHistoryWindow_Loaded;
        }

        private void CustomerHistoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (UserSession.CurrentUser != null)
            {
                txtCustomerHeader.Text = $"Tài khoản: {UserSession.CurrentUser.FullName} ({UserSession.CurrentUser.Username})";
                txtSearchPlaceholder.Text = "Nhập SĐT hoặc Mã vé để lọc danh sách...";
                LoadUserBookings();
            }
            else
            {
                // Khách vãng lai: Không tự động hiện vé, để trống danh sách và yêu cầu nhập SĐT
                txtCustomerHeader.Text = "Chế độ: Tra Cứu Vé Khách Vãng Lai";
                txtSearchPlaceholder.Text = "Mời nhập số điện thoại để truy xuất vé...";
                txtSearchPhoneOrCode.Text = string.Empty;

                dgBookings.ItemsSource = null;
                dgBookings.Visibility = Visibility.Collapsed;
                pnlEmptyState.Visibility = Visibility.Visible;
                txtEmptyTitle.Text = "Mời nhập số điện thoại để truy xuất vé";
                txtEmptyDesc.Text = "Nhập số điện thoại quý khách đã dùng khi đặt vé để tra cứu mã QR và thông tin vé điện tử.";
                txtTotalBookingsCount.Text = "Chờ nhập SĐT";
                txtSearchPhoneOrCode.Focus();
            }
        }

        private void TxtSearchPhoneOrCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtSearchPlaceholder.Visibility = string.IsNullOrEmpty(txtSearchPhoneOrCode.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void LoadUserBookings()
        {
            if (UserSession.CurrentUser == null) return;

            try
            {
                using var db = new CinemaManagementDbContext();
                int currentUserId = UserSession.CurrentUser.UserId;

                var bookings = db.Bookings
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Include(b => b.Showtime).ThenInclude(s => s.Room)
                    .Include(b => b.Tickets)
                    .Include(b => b.User)
                    .Where(b => b.UserId == currentUserId)
                    .OrderByDescending(b => b.BookingDate)
                    .ToList();

                DisplayBookings(bookings, isSearch: false);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải lịch sử đặt vé: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBookings(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                if (UserSession.CurrentUser != null)
                {
                    LoadUserBookings();
                }
                else
                {
                    CinemaMessageBox.Show("Mời quý khách nhập số điện thoại để truy xuất vé!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtSearchPhoneOrCode.Focus();
                }
                return;
            }

            try
            {
                string cleanQuery = query.Trim().ToLower();
                using var db = new CinemaManagementDbContext();

                var allBookings = db.Bookings
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Include(b => b.Showtime).ThenInclude(s => s.Room)
                    .Include(b => b.Tickets)
                    .Include(b => b.User)
                    .OrderByDescending(b => b.BookingDate)
                    .ToList();

                var bookings = allBookings
                    .Where(b => b.QrcodeString.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase)
                             || (b.User != null && b.User.Phone != null && b.User.Phone.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
                             || (b.User != null && VietnameseHelper.ContainsUnaccented(b.User.FullName, cleanQuery))
                             || (b.Showtime?.Movie != null && VietnameseHelper.ContainsUnaccented(b.Showtime.Movie.Title, cleanQuery)))
                    .ToList();

                DisplayBookings(bookings, isSearch: true);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi khi tra cứu vé: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayBookings(System.Collections.Generic.List<Booking> bookings, bool isSearch)
        {
            var displayList = bookings.Select(b => new BookingHistoryItem
            {
                BookingId = b.BookingId,
                QrcodeString = b.QrcodeString,
                MovieTitle = b.Showtime?.Movie?.Title ?? "Phim điện ảnh",
                BuyerInfo = $"{b.User?.FullName ?? "Khách vãng lai"} • {b.User?.Phone ?? ""}",
                RoomName = b.Showtime?.Room?.RoomName ?? "Phòng 01",
                ShowtimeString = $"{b.Showtime?.StartTime:HH:mm dd/MM/yyyy}",
                SeatsString = string.Join(", ", b.Tickets.Select(t => t.SeatCode).OrderBy(s => s)),
                TotalAmountString = $"{b.TotalAmount:N0} đ",
                BookingDateString = $"{b.BookingDate:HH:mm dd/MM/yyyy}",
                StatusText = b.IsCheckedIn ? "✓ Đã soát vé" : "● Chưa sử dụng",
                StatusBg = b.IsCheckedIn ? "#262626" : "#14532D",
                StatusFg = b.IsCheckedIn ? "#9CA3AF" : "#86EFAC"
            }).ToList();

            dgBookings.ItemsSource = displayList;
            txtTotalBookingsCount.Text = $"Tổng cộng: {displayList.Count} đơn vé";

            if (displayList.Any())
            {
                dgBookings.Visibility = Visibility.Visible;
                pnlEmptyState.Visibility = Visibility.Collapsed;
            }
            else
            {
                dgBookings.Visibility = Visibility.Collapsed;
                pnlEmptyState.Visibility = Visibility.Visible;
                if (isSearch)
                {
                    txtEmptyTitle.Text = $"Không tìm thấy đơn vé nào khớp với '{txtSearchPhoneOrCode.Text.Trim()}'";
                    txtEmptyDesc.Text = "Quý khách vui lòng kiểm tra lại số điện thoại hoặc mã vé đã nhập.";
                }
                else
                {
                    txtEmptyTitle.Text = "Mời nhập số điện thoại để truy xuất vé";
                    txtEmptyDesc.Text = "Nhập số điện thoại quý khách đã dùng khi đặt vé để tra cứu mã QR và vé điện tử.";
                }
            }

            btnViewQR.IsEnabled = false;
        }

        private void TxtSearchPhoneOrCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchBookings(txtSearchPhoneOrCode.Text);
            }
        }

        private void BtnSearchGuestTickets_Click(object sender, RoutedEventArgs e)
        {
            SearchBookings(txtSearchPhoneOrCode.Text);
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearchPhoneOrCode.Clear();
            if (UserSession.CurrentUser != null)
            {
                LoadUserBookings();
            }
            else
            {
                dgBookings.ItemsSource = null;
                dgBookings.Visibility = Visibility.Collapsed;
                pnlEmptyState.Visibility = Visibility.Visible;
                txtEmptyTitle.Text = "Mời nhập số điện thoại để truy xuất vé";
                txtEmptyDesc.Text = "Nhập số điện thoại quý khách đã dùng khi đặt vé để tra cứu mã QR và thông tin vé điện tử.";
                txtTotalBookingsCount.Text = "Chờ nhập SĐT";
                txtSearchPhoneOrCode.Focus();
            }
        }

        private void DgBookings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnViewQR.IsEnabled = dgBookings.SelectedItem != null;
        }

        private void DgBookings_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedTicketQR();
        }

        private void BtnViewQR_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedTicketQR();
        }

        private void OpenSelectedTicketQR()
        {
            if (dgBookings.SelectedItem is not BookingHistoryItem item) return;

            var ticketWin = new TicketQRWindow(item.BookingId);
            ticketWin.Owner = this;
            ticketWin.ShowDialog();

            // Refresh list
            if (!string.IsNullOrWhiteSpace(txtSearchPhoneOrCode.Text))
            {
                SearchBookings(txtSearchPhoneOrCode.Text);
            }
            else if (UserSession.CurrentUser != null)
            {
                LoadUserBookings();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSearchPhoneOrCode.Text))
            {
                SearchBookings(txtSearchPhoneOrCode.Text);
            }
            else if (UserSession.CurrentUser != null)
            {
                LoadUserBookings();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
