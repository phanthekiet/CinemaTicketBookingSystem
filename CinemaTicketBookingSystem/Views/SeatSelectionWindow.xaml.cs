using CinemaTicketBookingSystem.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CinemaTicketBookingSystem.Views
{
    public partial class SeatSelectionWindow : Window
    {
        private int _movieId;
        private Showtime? _selectedShowtime;
        private readonly List<string> _selectedSeats = new();
        private readonly HashSet<string> _soldSeats = new();
        private readonly Dictionary<string, Button> _seatButtons = new();
        private decimal _basePrice = 75000m;
        private const decimal VipSurcharge = 15000m;

        public SeatSelectionWindow() : this(0)
        {
        }

        public SeatSelectionWindow(int movieId)
        {
            InitializeComponent();
            _movieId = movieId;
            Loaded += SeatSelectionWindow_Loaded;
        }

        public SeatSelectionWindow(int movieId, int showtimeId) : this(movieId)
        {
            // Optional direct showtime pre-selection
        }

        private void SeatSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set User Greeting
            if (UserSession.CurrentUser != null)
            {
                txtUserGreeting.Text = $"Xin chào, {UserSession.CurrentUser.FullName}";
            }
            else
            {
                txtUserGreeting.Text = "Khách tham quan";
            }

            try
            {
                using var db = new CinemaManagementDbContext();

                // If movieId was not passed or 0, pick first active movie
                if (_movieId <= 0)
                {
                    var firstMovie = db.Movies.FirstOrDefault(m => m.Status);
                    if (firstMovie != null)
                    {
                        _movieId = firstMovie.MovieId;
                    }
                }

                var movie = db.Movies.Include(m => m.Genre).FirstOrDefault(m => m.MovieId == _movieId);
                if (movie != null)
                {
                    txtMovieTitle.Text = movie.Title.ToUpper();
                    txtMovieGenre.Text = movie.Genre?.GenreName ?? "Điện ảnh";
                    txtMovieDuration.Text = $"{movie.Duration} phút";
                    txtAgeRating.Text = string.IsNullOrWhiteSpace(movie.AgeRating) ? "P" : movie.AgeRating.Trim().ToUpper();

                    // Age rating badge color
                    bdrAgeRating.Background = txtAgeRating.Text switch
                    {
                        "T18" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B")),
                        "T16" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C2410C")),
                        "T13" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")),
                        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"))
                    };
                }

                // Auto-detect optimal showtime date for this movie
                DateTime targetDate = DateTime.Today;
                var hasToday = db.Showtimes.Any(s => s.MovieId == _movieId && s.Status && s.StartTime.Date == DateTime.Today);

                if (hasToday)
                {
                    targetDate = DateTime.Today;
                }
                else
                {
                    var nextDate = db.Showtimes
                        .Where(s => s.MovieId == _movieId && s.Status && s.StartTime.Date >= DateTime.Today)
                        .OrderBy(s => s.StartTime)
                        .Select(s => (DateTime?)s.StartTime.Date)
                        .FirstOrDefault();

                    if (nextDate.HasValue)
                    {
                        targetDate = nextDate.Value;
                    }
                    else
                    {
                        var anyDate = db.Showtimes
                            .Where(s => s.MovieId == _movieId && s.Status)
                            .OrderByDescending(s => s.StartTime)
                            .Select(s => (DateTime?)s.StartTime.Date)
                            .FirstOrDefault();

                        if (anyDate.HasValue)
                        {
                            targetDate = anyDate.Value;
                        }
                    }
                }

                dpShowDate.SelectedDate = targetDate;
                LoadShowtimes(targetDate);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải thông tin phim: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DpShowDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpShowDate.SelectedDate.HasValue)
            {
                LoadShowtimes(dpShowDate.SelectedDate.Value);
            }
        }

        private void LoadShowtimes(DateTime date)
        {
            spShowtimes.Children.Clear();
            _selectedShowtime = null;
            _selectedSeats.Clear();
            UpdatePriceAndSummary();

            try
            {
                using var db = new CinemaManagementDbContext();
                var showtimes = db.Showtimes
                    .Include(s => s.Room)
                    .Where(s => s.MovieId == _movieId && s.Status && s.StartTime.Date == date.Date)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                if (!showtimes.Any())
                {
                    // Query other dates that have showtimes for this movie
                    var availableDates = db.Showtimes
                        .Where(s => s.MovieId == _movieId && s.Status)
                        .Select(s => s.StartTime.Date)
                        .Distinct()
                        .OrderBy(d => d)
                        .Take(8)
                        .ToList();

                    var emptyPanel = new StackPanel { Margin = new Thickness(8) };

                    var emptyCard = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1B22")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A2E35")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 14)
                    };

                    var spCard = new StackPanel();
                    var emptyTitle = new TextBlock
                    {
                        Text = $"⚠️ Không có suất chiếu",
                        FontWeight = FontWeights.Bold,
                        Foreground = (Brush)FindResource("AccentRed"),
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    var emptyDesc = new TextBlock
                    {
                        Text = $"Phim chưa có suất chiếu vào ngày {date:dd/MM/yyyy}.",
                        Foreground = (Brush)FindResource("TextSecondary"),
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap
                    };
                    spCard.Children.Add(emptyTitle);
                    spCard.Children.Add(emptyDesc);
                    emptyCard.Child = spCard;
                    emptyPanel.Children.Add(emptyCard);

                    if (availableDates.Any())
                    {
                        var tipText = new TextBlock
                        {
                            Text = "📅 CÁC NGÀY CÓ SUẤT CHIẾU KHẢ DỤNG:",
                            Foreground = (Brush)FindResource("AccentGold"),
                            FontSize = 11,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 8)
                        };
                        emptyPanel.Children.Add(tipText);

                        var wpDates = new WrapPanel();
                        foreach (var avDate in availableDates)
                        {
                            string label = avDate.Date == DateTime.Today
                                ? $"Hôm nay ({avDate:dd/MM})"
                                : avDate.Date == DateTime.Today.AddDays(1)
                                    ? $"Ngày mai ({avDate:dd/MM})"
                                    : $"{avDate:dd/MM/yyyy}";

                            var btnDate = new Button
                            {
                                Content = label,
                                Margin = new Thickness(0, 0, 6, 8),
                                Padding = new Thickness(10, 6, 10, 6),
                                FontSize = 11,
                                FontWeight = FontWeights.SemiBold,
                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181B26")),
                                Foreground = Brushes.White,
                                BorderBrush = (Brush)FindResource("AccentGold"),
                                BorderThickness = new Thickness(1),
                                Cursor = System.Windows.Input.Cursors.Hand
                            };

                            var dtTarget = avDate;
                            btnDate.Click += (s, e) =>
                            {
                                dpShowDate.SelectedDate = dtTarget;
                            };
                            wpDates.Children.Add(btnDate);
                        }
                        emptyPanel.Children.Add(wpDates);
                    }

                    spShowtimes.Children.Add(emptyPanel);
                    ClearSeatGrid();
                    return;
                }

                bool isFirst = true;
                foreach (var st in showtimes)
                {
                    var btnShowtime = new Button
                    {
                        Tag = st,
                        Margin = new Thickness(0, 0, 0, 8),
                        Padding = new Thickness(12, 10, 12, 10),
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161922")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B3142")),
                        BorderThickness = new Thickness(1)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var spLeft = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    var tbTime = new TextBlock
                    {
                        Text = $"{st.StartTime:HH:mm} - {st.EndTime:HH:mm}",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Foreground = Brushes.White
                    };
                    var tbRoom = new TextBlock
                    {
                        Text = $"🏛️ {st.Room?.RoomName ?? "Phòng chiếu"} • 2D Phụ Đề",
                        FontSize = 11,
                        Foreground = (Brush)FindResource("TextSecondary"),
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    spLeft.Children.Add(tbTime);
                    spLeft.Children.Add(tbRoom);

                    var tbPrice = new TextBlock
                    {
                        Text = $"{st.BasePrice:N0} đ",
                        FontWeight = FontWeights.Bold,
                        FontSize = 13,
                        Foreground = (Brush)FindResource("AccentGold"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(tbPrice, 1);

                    grid.Children.Add(spLeft);
                    grid.Children.Add(tbPrice);
                    btnShowtime.Content = grid;

                    btnShowtime.Click += (s, ev) =>
                    {
                        SelectShowtime(st);
                        HighlightSelectedShowtimeButton(btnShowtime);
                    };

                    spShowtimes.Children.Add(btnShowtime);

                    if (isFirst)
                    {
                        SelectShowtime(st);
                        HighlightSelectedShowtimeButton(btnShowtime);
                        isFirst = false;
                    }
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải suất chiếu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HighlightSelectedShowtimeButton(Button selectedBtn)
        {
            foreach (var child in spShowtimes.Children)
            {
                if (child is Button b)
                {
                    b.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B3142"));
                    b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161922"));
                }
            }

            selectedBtn.BorderBrush = (Brush)FindResource("AccentRed");
            selectedBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A1215"));
        }

        private void SelectShowtime(Showtime showtime)
        {
            _selectedShowtime = showtime;
            _basePrice = showtime.BasePrice;
            _selectedSeats.Clear();

            txtSelectedRoom.Text = showtime.Room?.RoomName ?? "Phòng 01";
            txtSelectedTime.Text = $"{showtime.StartTime:dd/MM/yyyy HH:mm}";
            txtSelectedBasePrice.Text = $"{showtime.BasePrice:N0} đ";

            // Load sold tickets for this showtime from DB
            _soldSeats.Clear();
            try
            {
                using var db = new CinemaManagementDbContext();
                var soldList = db.Tickets
                    .Where(t => t.ShowtimeId == showtime.ShowtimeId)
                    .Select(t => t.SeatCode)
                    .ToList();

                foreach (var s in soldList)
                {
                    _soldSeats.Add(s.Trim().ToUpper());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading tickets: " + ex.Message);
            }

            RenderSeatsMatrix();
            UpdatePriceAndSummary();
        }

        private void ClearSeatGrid()
        {
            _seatButtons.Clear();
            _selectedSeats.Clear();
            txtSelectedRoom.Text = "Chưa chọn";
            txtSelectedTime.Text = "Chưa chọn";
            txtSelectedBasePrice.Text = "0 đ";
            UpdatePriceAndSummary();
            RenderPlaceholderSeats();
        }

        private void RenderPlaceholderSeats(string reason = "Vui lòng chọn suất chiếu để chọn ghế")
        {
            ugSeats.Children.Clear();
            _seatButtons.Clear();

            char[] rows = { 'A', 'B', 'C', 'D', 'E' };
            var seatStyle = (Style)FindResource("SeatButtonStyle");

            for (int r = 0; r < rows.Length; r++)
            {
                char rowChar = rows[r];
                bool isVip = (rowChar == 'C' || rowChar == 'D' || rowChar == 'E');

                for (int col = 1; col <= 8; col++)
                {
                    string seatCode = $"{rowChar}{col}";

                    var btn = new Button
                    {
                        Content = seatCode,
                        Style = seatStyle,
                        Tag = seatCode,
                        IsEnabled = false,
                        Opacity = 0.4,
                        ToolTip = reason
                    };

                    if (isVip)
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D1812"));
                        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A3B18"));
                        btn.Foreground = (Brush)FindResource("AccentGold");
                    }
                    else
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151821"));
                        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#272D3B"));
                        btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                    }

                    ugSeats.Children.Add(btn);
                }
            }
        }

        private void RenderSeatsMatrix()
        {
            ugSeats.Children.Clear();
            _seatButtons.Clear();

            char[] rows = { 'A', 'B', 'C', 'D', 'E' };
            var seatStyle = (Style)FindResource("SeatButtonStyle");

            for (int r = 0; r < rows.Length; r++)
            {
                char rowChar = rows[r];
                bool isVip = (rowChar == 'C' || rowChar == 'D' || rowChar == 'E');

                for (int col = 1; col <= 8; col++)
                {
                    string seatCode = $"{rowChar}{col}";
                    bool isSold = _soldSeats.Contains(seatCode);

                    var btn = new Button
                    {
                        Content = seatCode,
                        Style = seatStyle,
                        Tag = seatCode
                    };

                    if (isSold)
                    {
                        // Booked / Sold Seat (Đã có người đặt - Khớp 100% với chú thích nền đỏ thẫm viền đỏ kèm icon ✕)
                        btn.Content = "✕";
                        btn.FontSize = 13;
                        btn.FontWeight = FontWeights.Bold;
                        btn.Cursor = System.Windows.Input.Cursors.No;
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D1215"));
                        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                        btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                        btn.ToolTip = $"Ghế {seatCode} - Đã có người đặt";
                    }
                    else if (isVip)
                    {
                        // VIP Seat (Rows C, D, E)
                        btn.Cursor = System.Windows.Input.Cursors.Hand;
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A1F10"));
                        btn.BorderBrush = (Brush)FindResource("AccentGold");
                        btn.Foreground = (Brush)FindResource("AccentGold");
                        btn.ToolTip = $"Ghế VIP {seatCode} ({_basePrice + VipSurcharge:N0} đ)";
                    }
                    else
                    {
                        // Standard Seat (Rows A, B)
                        btn.Cursor = System.Windows.Input.Cursors.Hand;
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#131B2E"));
                        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A3B60"));
                        btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                        btn.ToolTip = $"Ghế Thường {seatCode} ({_basePrice:N0} đ)";
                    }

                    btn.Click += SeatButton_Click;

                    _seatButtons[seatCode] = btn;
                    ugSeats.Children.Add(btn);
                }
            }
        }

        private void SeatButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string seatCode) return;

            if (_soldSeats.Contains(seatCode))
            {
                // Ghế đã có người đặt -> không cho chọn
                return;
            }

            if (_selectedSeats.Contains(seatCode))
            {
                // Deselect
                _selectedSeats.Remove(seatCode);
                RevertSeatAppearance(btn, seatCode);
            }
            else
            {
                // Giới hạn tối đa 8 vé cho mỗi lần đặt theo quy định
                if (_selectedSeats.Count >= 8)
                {
                    CinemaMessageBox.Show(
                        "QUY ĐỊNH RẠP KPID CINEMA: Mỗi lần đặt vé chỉ được chọn tối đa 8 ghế!\n\n" +
                        "Quý khách đã chọn đủ 8 ghế. Nếu có nhu cầu đặt thêm vé cho người thân/bạn bè, vui lòng hoàn tất đơn này và thực hiện thêm đơn mới.",
                        "Giới hạn số lượng vé",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Select (Khớp 100% với chú thích Đang chọn: Nền xanh lá Emerald #15803D viền Neon #4ADE80)
                _selectedSeats.Add(seatCode);
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
                btn.Foreground = Brushes.White;
                btn.FontWeight = FontWeights.Black;
            }

            UpdatePriceAndSummary();
        }

        private void RevertSeatAppearance(Button btn, string seatCode)
        {
            char rowChar = seatCode[0];
            bool isVip = (rowChar == 'C' || rowChar == 'D' || rowChar == 'E');

            if (isVip)
            {
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A1F10"));
                btn.BorderBrush = (Brush)FindResource("AccentGold");
                btn.Foreground = (Brush)FindResource("AccentGold");
                btn.FontWeight = FontWeights.Bold;
            }
            else
            {
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#131B2E"));
                btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A3B60"));
                btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                btn.FontWeight = FontWeights.Bold;
            }
        }

        private void UpdatePriceAndSummary()
        {
            if (!_selectedSeats.Any())
            {
                txtSelectedSeatsList.Text = "Chưa chọn ghế nào";
                txtSeatCount.Text = "(0/8 ghế)";
                txtTotalSeatPrice.Text = "0 đ";
                btnProceedToCheckout.IsEnabled = false;
                return;
            }

            decimal total = 0;
            foreach (var seat in _selectedSeats)
            {
                char row = seat[0];
                if (row == 'C' || row == 'D' || row == 'E')
                {
                    total += (_basePrice + VipSurcharge);
                }
                else
                {
                    total += _basePrice;
                }
            }

            var sortedSeats = _selectedSeats.OrderBy(s => s[0]).ThenBy(s => int.Parse(s[1..])).ToList();
            txtSelectedSeatsList.Text = string.Join(", ", sortedSeats);
            txtSeatCount.Text = $"({sortedSeats.Count}/8 ghế)";
            txtTotalSeatPrice.Text = $"{total:N0} đ";
            btnProceedToCheckout.IsEnabled = true;
        }

        private bool CheckForIsolatedSingleGap()
        {
            // Business Rule: Tiêu chuẩn rạp chiếu phim (CGV / Galaxy) - Tuyệt đối không được để lại đúng 1 ghế trống đơn lẻ
            // (bao gồm: giữa 2 ghế đã đặt/đang chọn, giữa ghế chọn với ghế đã bán, hoặc giữa ghế chọn với mép/đầu/cuối hàng)
            const int totalCols = 8;
            var affectedRows = _selectedSeats.Select(s => s[0]).Distinct();

            foreach (char row in affectedRows)
            {
                // 1. Trạng thái TRƯỚC khi chọn của hàng này (1..8)
                bool[] takenBefore = new bool[totalCols + 1];
                for (int col = 1; col <= totalCols; col++)
                {
                    takenBefore[col] = _soldSeats.Contains($"{row}{col}");
                }

                // 2. Trạng thái SAU khi chọn của hàng này (1..8)
                bool[] takenAfter = new bool[totalCols + 1];
                for (int col = 1; col <= totalCols; col++)
                {
                    string code = $"{row}{col}";
                    takenAfter[col] = _soldSeats.Contains(code) || _selectedSeats.Contains(code);
                }

                // 3. Quét các khoảng ghế trống sau khi chọn
                var isolatedSeatsCreated = new List<string>();

                int colIdx = 1;
                while (colIdx <= totalCols)
                {
                    if (!takenAfter[colIdx])
                    {
                        int start = colIdx;
                        while (colIdx <= totalCols && !takenAfter[colIdx])
                        {
                            colIdx++;
                        }
                        int end = colIdx - 1;
                        int length = end - start + 1;

                        // Nếu sau khi chọn để lại khoảng trống có độ dài đúng bằng 1 ghế
                        if (length == 1)
                        {
                            int orphanCol = start;

                            // Kiểm tra xem ghế này có phải đã lẻ loi từ trước không
                            int beforeStart = orphanCol;
                            while (beforeStart > 1 && !takenBefore[beforeStart - 1])
                            {
                                beforeStart--;
                            }
                            int beforeEnd = orphanCol;
                            while (beforeEnd < totalCols && !takenBefore[beforeEnd + 1])
                            {
                                beforeEnd++;
                            }
                            int lengthBefore = beforeEnd - beforeStart + 1;

                            // Nếu trước khi khách chọn, ghế này không phải là ghế lẻ đơn độc (khoảng trống trước đó > 1),
                            // tức là do khách chọn ghế đã TẠO RA ghế lẻ mới:
                            if (lengthBefore > 1)
                            {
                                isolatedSeatsCreated.Add($"{row}{orphanCol}");
                            }
                        }
                    }
                    else
                    {
                        colIdx++;
                    }
                }

                if (isolatedSeatsCreated.Any())
                {
                    string seatList = string.Join(", ", isolatedSeatsCreated);
                    CinemaMessageBox.Show(
                        $"QUY ĐỊNH RẠP KPID CINEMA: Không được để lại ghế trống đơn lẻ!\n\n" +
                        $"Lựa chọn hiện tại của quý khách sẽ để lại ghế trống đơn lẻ: [{seatList}] ở hàng {row}.\n\n" +
                        $"Vui lòng chọn ghế liền kề nhau hoặc chọn sát mép hàng / ghế đã bán để tối ưu chỗ ngồi cho các khán giả khác.",
                        "Quy định chọn ghế rạp chiếu phim",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void BtnProceedToCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShowtime == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn suất chiếu trước khi tiếp tục!", "Chưa chọn suất chiếu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_selectedSeats.Any())
            {
                CinemaMessageBox.Show("Vui lòng chọn ít nhất 1 ghế xem phim!", "Chưa chọn ghế", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedSeats.Count > 8)
            {
                CinemaMessageBox.Show(
                    "QUY ĐỊNH RẠP KPID CINEMA:\n\nMỗi lần đặt vé chỉ được chọn tối đa 8 ghế/vé!\nVui lòng bỏ bớt ghế trước khi tiếp tục.",
                    "Giới hạn số lượng vé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!CheckForIsolatedSingleGap())
            {
                return;
            }

            // Calculate total seat price
            decimal totalSeatPrice = 0;
            foreach (var seat in _selectedSeats)
            {
                char r = seat[0];
                totalSeatPrice += (r == 'C' || r == 'D' || r == 'E') ? (_basePrice + VipSurcharge) : _basePrice;
            }

            // Navigate to CheckoutWindow
            var checkoutWin = new CheckoutWindow(_selectedShowtime.ShowtimeId, _selectedSeats.OrderBy(s => s[0]).ThenBy(s => int.Parse(s[1..])).ToList(), totalSeatPrice);
            checkoutWin.Owner = this;
            checkoutWin.ShowDialog();

            if (checkoutWin.IsBookingSuccessful)
            {
                // Đặt vé hoàn tất và khách đã xem mã QR / bấm Về Trang Chủ -> đóng SeatSelectionWindow để trở về trang chủ
                Close();
            }
            else
            {
                // Refresh sold seats in case another booking was made
                SelectShowtime(_selectedShowtime);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
