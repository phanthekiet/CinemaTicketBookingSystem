using CinemaTicketBookingSystem.Entities;
using CinemaTicketBookingSystem.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CinemaTicketBookingSystem.Views
{
    public partial class CheckoutWindow : Window
    {
        public bool IsBookingSuccessful { get; private set; } = false;

        private int _showtimeId;
        private List<string> _selectedSeats = new();
        private decimal _totalSeatPrice = 0;
        private Showtime? _showtime;
        private readonly Dictionary<int, int> _foodQuantities = new();
        private readonly Dictionary<int, FoodAndDrink> _foodItems = new();
        private readonly Dictionary<int, TextBlock> _foodQtyLabels = new();

        private readonly VnPayService _vnPayService = new();
        private CancellationTokenSource? _vnPayCts;

        public CheckoutWindow() : this(0, new List<string>(), 0)
        {
        }

        public CheckoutWindow(int showtimeId, List<string> selectedSeats, decimal totalSeatPrice)
        {
            InitializeComponent();
            _showtimeId = showtimeId;
            _selectedSeats = selectedSeats ?? new List<string>();
            _totalSeatPrice = totalSeatPrice;

            Loaded += CheckoutWindow_Loaded;
        }

        private void CheckoutWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Để trống 2 ô họ tên và số điện thoại để người dùng tự nhập
            txtCustomerName.Text = (UserSession.CurrentUser != null) ? UserSession.CurrentUser.FullName : "Khách vãng lai";
            txtBuyerName.Text = string.Empty;
            txtBuyerPhone.Text = string.Empty;
            txtBuyerName.Focus();

            // 2. Load Showtime Details
            try
            {
                using var db = new CinemaManagementDbContext();
                _showtime = db.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.Room)
                    .FirstOrDefault(s => s.ShowtimeId == _showtimeId);

                if (_showtime != null)
                {
                    txtSummaryMovie.Text = _showtime.Movie?.Title?.ToUpper() ?? "Phim chiếu rạp";
                    txtSummaryRoom.Text = _showtime.Room?.RoomName ?? "Phòng 01";
                    txtSummaryTime.Text = $"{_showtime.StartTime:HH:mm} • {_showtime.StartTime:dd/MM/yyyy}";
                }

                txtSummarySeats.Text = _selectedSeats.Any() ? string.Join(", ", _selectedSeats) : "Chưa chọn ghế";
                txtTotalSeatsMoney.Text = $"{_totalSeatPrice:N0} đ";

                // 3. Load Food & Drinks
                LoadFoodItems(db);

                // 4. Update Summary & QR
                UpdateTotalsAndQR();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải thông tin thanh toán: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFoodItems(CinemaManagementDbContext db)
        {
            spFoodList.Children.Clear();
            _foodItems.Clear();
            _foodQuantities.Clear();
            _foodQtyLabels.Clear();

            var foods = db.FoodAndDrinks.Where(f => f.Status && f.Stock > 0).ToList();

            if (!foods.Any())
            {
                spFoodList.Children.Add(new TextBlock
                {
                    Text = "Hiện tại các món bắp nước đang tạm ngưng phục vụ trực tuyến.",
                    Foreground = (Brush)FindResource("TextMuted"),
                    FontSize = 12,
                    Margin = new Thickness(10)
                });
                return;
            }

            foreach (var food in foods)
            {
                _foodItems[food.FoodId] = food;
                _foodQuantities[food.FoodId] = 0;

                var card = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C202C")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B3142")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 12, 14, 12),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) }); // Icon
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Stepper

                // Icon
                string icon = food.FoodName.ToLower().Contains("bắp") ? "🍿" :
                              food.FoodName.ToLower().Contains("nước") ? "🥤" : "🌭";
                var tbIcon = new TextBlock
                {
                    Text = icon,
                    FontSize = 24,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(tbIcon, 0);

                // Info
                var spInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
                var tbName = new TextBlock
                {
                    Text = food.FoodName,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = Brushes.White
                };
                var spSub = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
                var tbPrice = new TextBlock
                {
                    Text = $"{food.Price:N0} đ",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("AccentGold")
                };
                var tbStock = new TextBlock
                {
                    Text = $" • Còn {food.Stock} phần",
                    FontSize = 11,
                    Foreground = (Brush)FindResource("TextMuted")
                };
                spSub.Children.Add(tbPrice);
                spSub.Children.Add(tbStock);
                spInfo.Children.Add(tbName);
                spInfo.Children.Add(spSub);
                Grid.SetColumn(spInfo, 1);

                // Stepper Buttons [-] [0] [+]
                var spStepper = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var btnMinus = new Button
                {
                    Content = "－",
                    Width = 32,
                    Height = 32,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B3142")),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                var tbQty = new TextBlock
                {
                    Text = "0",
                    Width = 36,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _foodQtyLabels[food.FoodId] = tbQty;

                var btnPlus = new Button
                {
                    Content = "＋",
                    Width = 32,
                    Height = 32,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Background = (Brush)FindResource("AccentRed"),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                int currentFoodId = food.FoodId;
                int currentMaxStock = food.Stock;

                btnMinus.Click += (s, ev) =>
                {
                    if (_foodQuantities[currentFoodId] > 0)
                    {
                        _foodQuantities[currentFoodId]--;
                        tbQty.Text = _foodQuantities[currentFoodId].ToString();
                        UpdateTotalsAndQR();
                    }
                };

                btnPlus.Click += (s, ev) =>
                {
                    if (_foodQuantities[currentFoodId] < currentMaxStock)
                    {
                        _foodQuantities[currentFoodId]++;
                        tbQty.Text = _foodQuantities[currentFoodId].ToString();
                        UpdateTotalsAndQR();
                    }
                    else
                    {
                        CinemaMessageBox.Show($"Món này chỉ còn lại {currentMaxStock} phần trong kho!", "Số lượng tồn kho", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                };

                spStepper.Children.Add(btnMinus);
                spStepper.Children.Add(tbQty);
                spStepper.Children.Add(btnPlus);
                Grid.SetColumn(spStepper, 2);

                grid.Children.Add(tbIcon);
                grid.Children.Add(spInfo);
                grid.Children.Add(spStepper);
                card.Child = grid;

                spFoodList.Children.Add(card);
            }
        }

        private void UpdateTotalsAndQR()
        {
            decimal totalFood = 0;
            var chosenFoodNames = new List<string>();

            foreach (var kvp in _foodQuantities)
            {
                if (kvp.Value > 0 && _foodItems.TryGetValue(kvp.Key, out var food))
                {
                    totalFood += (kvp.Value * food.Price);
                    chosenFoodNames.Add($"{food.FoodName} (x{kvp.Value})");
                }
            }

            decimal grandTotal = _totalSeatPrice + totalFood;

            txtTotalFoodMoney.Text = $"{totalFood:N0} đ";
            txtGrandTotal.Text = $"{grandTotal:N0} đ";

            txtSummaryFood.Text = chosenFoodNames.Any() ? string.Join(", ", chosenFoodNames) : "Không chọn";

            // Update VietQR image if checked
            if (rbVietQR.IsChecked == true)
            {
                try
                {
                    imgVietQR.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/vietqr_mb.png"));
                }
                catch
                {
                    // Fallback
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _vnPayCts?.Cancel();
            _vnPayService.StopListener();
        }

        private void PaymentMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (pnlVnPay == null || pnlVietQR == null || pnlCash == null || pnlWaitingVnPay == null || btnConfirmPayment == null) return;

            pnlVnPay.Visibility = Visibility.Collapsed;
            pnlVietQR.Visibility = Visibility.Collapsed;
            pnlCash.Visibility = Visibility.Collapsed;
            pnlWaitingVnPay.Visibility = Visibility.Collapsed;

            if (rbVnPay.IsChecked == true)
            {
                pnlVnPay.Visibility = Visibility.Visible;
                btnConfirmPayment.Content = "THANH TOÁN QUA VNPAY";
            }
            else if (rbVietQR.IsChecked == true)
            {
                pnlVietQR.Visibility = Visibility.Visible;
                btnConfirmPayment.Content = "XÁC NHẬN ĐÃ CHUYỂN KHOẢN";
            }
            else
            {
                pnlCash.Visibility = Visibility.Visible;
                btnConfirmPayment.Content = "XÁC NHẬN GIỮ VÉ VÀ TRẢ TẠI QUẦY";
            }
        }

        private void BtnCancelVnPay_Click(object sender, RoutedEventArgs e)
        {
            _vnPayCts?.Cancel();
            _vnPayService.StopListener();

            ResetVnPayUi();
        }

        private void ResetVnPayUi()
        {
            pnlWaitingVnPay.Visibility = Visibility.Collapsed;
            pnlVnPay.Visibility = Visibility.Visible;
            btnConfirmPayment.IsEnabled = true;
            btnConfirmPayment.Content = "THANH TOÁN QUA VNPAY";
        }

        private decimal CalculateGrandTotal()
        {
            decimal totalFood = 0;
            foreach (var kvp in _foodQuantities)
            {
                if (kvp.Value > 0 && _foodItems.TryGetValue(kvp.Key, out var food))
                {
                    totalFood += (kvp.Value * food.Price);
                }
            }
            return _totalSeatPrice + totalFood;
        }

        private async void BtnConfirmPayment_Click(object sender, RoutedEventArgs e)
        {
            var buyerName = txtBuyerName.Text.Trim();
            var buyerPhone = txtBuyerPhone.Text.Trim();

            if (string.IsNullOrWhiteSpace(buyerName) && string.IsNullOrWhiteSpace(buyerPhone))
            {
                CinemaMessageBox.Show(
                    "Quý khách vui lòng nhập đầy đủ Họ và tên cùng Số điện thoại để hệ thống xác nhận và xuất vé điện tử QR Code!",
                    "Thiếu thông tin người đặt vé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtBuyerName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(buyerName))
            {
                CinemaMessageBox.Show(
                    "Quý khách vui lòng nhập Họ và tên người đặt vé!",
                    "Thiếu thông tin họ tên",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtBuyerName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(buyerPhone))
            {
                CinemaMessageBox.Show(
                    "Quý khách vui lòng nhập Số điện thoại để nhận thông tin vé điện tử!",
                    "Thiếu thông tin số điện thoại",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtBuyerPhone.Focus();
                return;
            }

            if (buyerPhone.Length != 10 || !buyerPhone.All(char.IsDigit))
            {
                CinemaMessageBox.Show(
                    "Số điện thoại không hợp lệ! Vui lòng nhập đúng 10 chữ số (VD: 0912345678).",
                    "Số điện thoại không hợp lệ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtBuyerPhone.Focus();
                return;
            }

            decimal grandTotal = CalculateGrandTotal();

            // TRƯỜNG HỢP 1: CỔNG VNPAY SANDBOX THẬT
            if (rbVnPay.IsChecked == true)
            {
                string txnRef = "KPID" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string orderInfo = $"Thanh toan ve KPID Cinema - Ma {txnRef}";
                string paymentUrl = _vnPayService.CreatePaymentUrl(grandTotal, orderInfo, txnRef);

                // Chuyển giao diện sang trạng thái chờ
                pnlVnPay.Visibility = Visibility.Collapsed;
                pnlWaitingVnPay.Visibility = Visibility.Visible;
                btnConfirmPayment.IsEnabled = false;
                btnConfirmPayment.Content = "⏳ Đang chờ xác nhận từ VNPAY...";

                try
                {
                    // Mở trình duyệt web của máy
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = paymentUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    CinemaMessageBox.Show($"Không thể mở trình duyệt: {ex.Message}\nURL: {paymentUrl}", "Lỗi mở cổng VNPAY", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetVnPayUi();
                    return;
                }

                // Chờ callback từ HttpListener localhost:8888
                _vnPayCts = new CancellationTokenSource();
                var callbackResult = await Task.Run(() => _vnPayService.WaitForCallbackAsync(txnRef, _vnPayCts.Token));

                if (callbackResult.Success)
                {
                    // Thanh toán thành công 100%
                    ProcessSuccessfulBooking(buyerName, buyerPhone, grandTotal, "VNPAY", callbackResult.TransactionNo);
                }
                else
                {
                    ResetVnPayUi();
                    if (callbackResult.ResponseCode != "CANCELLED")
                    {
                        CinemaMessageBox.Show(
                            $"Giao dịch VNPAY chưa hoàn tất!\n\nChi tiết: {callbackResult.Message}",
                            "Thông báo VNPAY",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                // TRƯỜNG HỢP 2: VIETQR HOẶC TIỀN MẶT
                string method = rbVietQR.IsChecked == true ? "VietQR" : "Cash";
                ProcessSuccessfulBooking(buyerName, buyerPhone, grandTotal, method);
            }
        }

        private void ProcessSuccessfulBooking(string buyerName, string buyerPhone, decimal grandTotal, string paymentMethodName, string? transactionNo = null)
        {
            try
            {
                using var db = new CinemaManagementDbContext();
                using var transaction = db.Database.BeginTransaction();

                // 0. Seat Limit Check: Mỗi lần đặt vé chỉ được tối đa 8 vé
                if (_selectedSeats.Count > 8)
                {
                    transaction.Rollback();
                    CinemaMessageBox.Show("Mỗi lần đặt vé chỉ được chọn tối đa 8 vé theo quy định rạp!", "Vượt quá giới hạn vé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                // 1. Race Condition Check: Kiểm tra ghế đã bị khách khác đặt trước đó hay chưa
                var alreadySoldSeats = db.Tickets
                    .Where(t => t.ShowtimeId == _showtimeId && _selectedSeats.Contains(t.SeatCode))
                    .Select(t => t.SeatCode)
                    .ToList();

                if (alreadySoldSeats.Any())
                {
                    transaction.Rollback();
                    CinemaMessageBox.Show(
                        $"Rất tiếc! Ghế [{string.Join(", ", alreadySoldSeats)}] vừa có khách hàng khác hoàn tất thanh toán trước!\n\n" +
                        "Giao dịch đã được hủy an toàn để tránh trùng ghế. Quý khách vui lòng chọn lại ghế khác.",
                        "Ghế đã có người đặt",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Close(); // Đóng thanh toán để quay lại sơ đồ ghế đã làm mới
                    return;
                }

                // 2. Race Condition Check: Kiểm tra tồn kho bắp nước trước khi trừ
                foreach (var kvp in _foodQuantities)
                {
                    if (kvp.Value > 0)
                    {
                        var dbFood = db.FoodAndDrinks.Find(kvp.Key);
                        if (dbFood == null || dbFood.Stock < kvp.Value)
                        {
                            transaction.Rollback();
                            CinemaMessageBox.Show(
                                $"Rất tiếc! Món \"{dbFood?.FoodName ?? "bắp nước"}\" hiện chỉ còn {dbFood?.Stock ?? 0} phần trong kho (bạn chọn {kvp.Value} phần) do có khách vừa mua trước.\n\nVui lòng điều chỉnh lại số lượng bắp nước!",
                                "Hết hàng tồn kho",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                // 3. Resolve User
                int userId;
                if (UserSession.CurrentUser != null)
                {
                    userId = UserSession.CurrentUser.UserId;
                }
                else
                {
                    string guestDisplayName = string.IsNullOrWhiteSpace(buyerName) ? "Khách vãng lai" : buyerName;
                    var existingUser = db.Users.FirstOrDefault(u => u.Phone == buyerPhone);
                    if (existingUser != null)
                    {
                        userId = existingUser.UserId;
                        if (!string.IsNullOrWhiteSpace(buyerName))
                        {
                            existingUser.FullName = buyerName;
                        }
                    }
                    else
                    {
                        var guestUser = new User
                        {
                            FullName = guestDisplayName,
                            Username = "guest_" + Guid.NewGuid().ToString("N")[..6],
                            Phone = buyerPhone,
                            Password = "123",
                            Role = "Customer",
                            Status = true
                        };
                        db.Users.Add(guestUser);
                        db.SaveChanges();
                        userId = guestUser.UserId;
                    }
                }

                // 4. Create Booking
                string qrCode = "CINEMA-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
                var newBooking = new Booking
                {
                    UserId = userId,
                    ShowtimeId = _showtimeId,
                    BookingDate = DateTime.Now,
                    TotalAmount = grandTotal,
                    QrcodeString = qrCode,
                    PaymentMethod = paymentMethodName,
                    IsCheckedIn = false
                };

                db.Bookings.Add(newBooking);
                db.SaveChanges(); // Lấy BookingId

                // 5. Create Tickets
                decimal basePrice = _showtime?.BasePrice ?? 75000m;
                foreach (var seat in _selectedSeats)
                {
                    char r = seat[0];
                    decimal seatPrice = (r == 'C' || r == 'D' || r == 'E') ? (basePrice + 15000m) : basePrice;

                    var ticket = new Ticket
                    {
                        BookingId = newBooking.BookingId,
                        ShowtimeId = _showtimeId,
                        SeatCode = seat,
                        SeatPrice = seatPrice
                    };
                    db.Tickets.Add(ticket);
                }

                // 6. Create BookingFoods and deduct stock
                foreach (var kvp in _foodQuantities)
                {
                    if (kvp.Value > 0)
                    {
                        int foodId = kvp.Key;
                        int qty = kvp.Value;

                        var dbFood = db.FoodAndDrinks.Find(foodId);
                        if (dbFood != null)
                        {
                            dbFood.Stock -= qty;

                            var bf = new BookingFood
                            {
                                BookingId = newBooking.BookingId,
                                FoodId = foodId,
                                Quantity = qty,
                                UnitPrice = dbFood.Price
                            };
                            db.BookingFoods.Add(bf);
                        }
                    }
                }

                db.SaveChanges();
                transaction.Commit(); // Commit an toàn tuyệt đối mọi dữ liệu

                // Lưu lại thông tin vé gần nhất cho khách vãng lai để dễ dàng tra cứu / mở lại nếu lỡ tắt
                if (UserSession.CurrentUser == null)
                {
                    UserSession.LastGuestBookingId = newBooking.BookingId;
                    UserSession.LastGuestPhone = buyerPhone;
                }

                string extraTxn = string.IsNullOrEmpty(transactionNo) ? "" : $"\nMã giao dịch VNPAY: {transactionNo}";
                CinemaMessageBox.Show(
                    $"🎉 ĐẶT VÉ VÀ THANH TOÁN THÀNH CÔNG!\n\nPhương thức: {paymentMethodName}{extraTxn}\nSố tiền: {grandTotal:N0} đ\nMã vé: {qrCode}\n\nĐang chuyển sang màn hình vé xem phim...",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // 7. Open TicketQRWindow: Ẩn màn hình thanh toán và hiển thị vé điện tử kèm mã QR
                IsBookingSuccessful = true;
                this.Visibility = Visibility.Collapsed;

                var ticketWin = new TicketQRWindow(newBooking.BookingId);
                ticketWin.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                ticketWin.ShowDialog();

                Close();
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("UQ_Ticket_Showtime_Seat") == true || dbEx.Message.Contains("UQ_Ticket_Showtime_Seat"))
            {
                CinemaMessageBox.Show(
                    "Rất tiếc! Một trong các ghế bạn chọn vừa được một khách hàng khác hoàn tất thanh toán trước đúng thời điểm này.\n\n" +
                    "Hệ thống đã tự động kích hoạt bảo vệ chống trùng vé và hủy giao dịch an toàn. Vui lòng chọn ghế khác!",
                    "Trùng ghế (Bảo vệ Race Condition)",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Close();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi trong quá trình lưu đặt vé: {ex.Message}", "Lỗi thanh toán", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBackToSeats_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
