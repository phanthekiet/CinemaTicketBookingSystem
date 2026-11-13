using CinemaTicketBookingSystem.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CinemaTicketBookingSystem.Views
{
    public partial class AdminShowtimeWindow : Window
    {
        public class ShowtimeDisplayItem
        {
            public int ShowtimeId { get; set; }
            public int MovieId { get; set; }
            public string MovieTitle { get; set; } = string.Empty;
            public int RoomId { get; set; }
            public string RoomName { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public string StartTimeString => StartTime.ToString("HH:mm • dd/MM/yyyy");
            public DateTime EndTime { get; set; }
            public string EndTimeString => EndTime.ToString("HH:mm • dd/MM/yyyy");
            public decimal BasePrice { get; set; }
            public string BasePriceString => $"{BasePrice:N0} đ";
            public bool Status { get; set; }
            public string StatusText => Status ? "Đang mở bán" : "Đã hủy";
            public string StatusBg => Status ? "#14532D" : "#374151";
            public string StatusFg => Status ? "#86EFAC" : "#9CA3AF";
        }

        public class RoomDisplayItem
        {
            public int RoomId { get; set; }
            public string RoomName { get; set; } = string.Empty;
            public int TotalSeats { get; set; }
            public bool Status { get; set; }
            public string StatusText => Status ? "Hoạt động" : "Bảo trì";
            public string StatusBg => Status ? "#14532D" : "#7F1D1D";
            public string StatusFg => Status ? "#86EFAC" : "#FCA5A5";
        }

        private List<ShowtimeDisplayItem> _allShowtimes = new();
        private List<Movie> _activeMovies = new();
        private List<Room> _activeRooms = new();
        private ShowtimeDisplayItem? _selectedShowtime;
        private RoomDisplayItem? _selectedRoom;

        public AdminShowtimeWindow()
        {
            InitializeComponent();
            Loaded += AdminShowtimeWindow_Loaded;
        }

        private void AdminShowtimeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtAdminName.Text = $"Admin: {UserSession.CurrentUser?.FullName ?? "Quản trị viên"}";

            // Populate Hours (08 - 23)
            cbHour.Items.Clear();
            for (int h = 8; h <= 23; h++)
            {
                cbHour.Items.Add(h.ToString("00"));
            }
            cbHour.SelectedIndex = 11; // 19:00

            // Populate Minutes (00, 15, 30, 45)
            cbMinute.Items.Clear();
            cbMinute.Items.Add("00");
            cbMinute.Items.Add("15");
            cbMinute.Items.Add("30");
            cbMinute.Items.Add("45");
            cbMinute.SelectedIndex = 0;

            dpShowDate.SelectedDate = DateTime.Today;
            txtBasePrice.Text = "75000";

            LoadFormData();
            LoadShowtimes();
            LoadRooms();
        }

        #region Navigation
        private void NavToMovies_Click(object sender, RoutedEventArgs e)
        {
            var win = new AdminMovieWindow();
            win.Show();
            Close();
        }

        private void NavToFood_Click(object sender, RoutedEventArgs e)
        {
            var win = new AdminFoodWindow();
            win.Show();
            Close();
        }

        private void NavToReports_Click(object sender, RoutedEventArgs e)
        {
            var win = new AdminReportWindow();
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

        #region Load Data
        private void LoadFormData()
        {
            try
            {
                using var db = new CinemaManagementDbContext();
                _activeMovies = db.Movies.Where(m => m.Status).OrderBy(m => m.Title).ToList();
                _activeRooms = db.Rooms.Where(r => r.Status).OrderBy(r => r.RoomName).ToList();

                cbMovie.ItemsSource = _activeMovies;
                cbMovie.DisplayMemberPath = "Title";
                cbMovie.SelectedValuePath = "MovieId";
                if (_activeMovies.Any()) cbMovie.SelectedIndex = 0;

                cbRoom.ItemsSource = _activeRooms;
                cbRoom.DisplayMemberPath = "RoomName";
                cbRoom.SelectedValuePath = "RoomId";
                if (_activeRooms.Any()) cbRoom.SelectedIndex = 0;

                // Filter Room combo
                var filterRooms = new List<Room> { new Room { RoomId = 0, RoomName = "-- Tất cả phòng --" } };
                filterRooms.AddRange(_activeRooms);
                cbFilterRoom.ItemsSource = filterRooms;
                cbFilterRoom.DisplayMemberPath = "RoomName";
                cbFilterRoom.SelectedValuePath = "RoomId";
                cbFilterRoom.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải dữ liệu form: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadShowtimes()
        {
            try
            {
                using var db = new CinemaManagementDbContext();
                var showtimes = db.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.Room)
                    .OrderByDescending(s => s.StartTime)
                    .ToList();

                _allShowtimes = showtimes.Select(s => new ShowtimeDisplayItem
                {
                    ShowtimeId = s.ShowtimeId,
                    MovieId = s.MovieId,
                    MovieTitle = s.Movie?.Title ?? "Phim",
                    RoomId = s.RoomId,
                    RoomName = s.Room?.RoomName ?? "Phòng",
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    BasePrice = s.BasePrice,
                    Status = s.Status
                }).ToList();

                ApplyShowtimeFilters();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải danh sách suất chiếu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyShowtimeFilters()
        {
            if (!IsLoaded || dgShowtimes == null || txtShowtimeCount == null) return;

            var query = _allShowtimes.AsEnumerable();

            if (dpFilterDate != null && dpFilterDate.SelectedDate.HasValue)
            {
                var d = dpFilterDate.SelectedDate.Value.Date;
                query = query.Where(s => s.StartTime.Date == d);
            }

            if (cbFilterRoom?.SelectedItem is Room selRoom && selRoom.RoomId > 0)
            {
                query = query.Where(s => s.RoomId == selRoom.RoomId);
            }

            var list = query.ToList();
            dgShowtimes.ItemsSource = list;
            txtShowtimeCount.Text = $"Hiển thị: {list.Count} / {_allShowtimes.Count} suất chiếu";
        }

        private void FilterShowtime_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded) return;
            ApplyShowtimeFilters();
        }

        private void BtnResetShowtimeFilter_Click(object sender, RoutedEventArgs e)
        {
            dpFilterDate.SelectedDate = null;
            if (cbFilterRoom.Items.Count > 0) cbFilterRoom.SelectedIndex = 0;
            ApplyShowtimeFilters();
        }
        #endregion

        #region Showtime Form Logic & Anti-conflict
        private void CbMovieOrTime_Changed(object sender, EventArgs e)
        {
            if (cbMovie?.SelectedItem is not Movie selMovie || !dpShowDate.SelectedDate.HasValue) return;

            int hour = cbHour.SelectedIndex >= 0 ? int.Parse(cbHour.SelectedItem.ToString()!) : 19;
            int minute = cbMinute.SelectedIndex >= 0 ? int.Parse(cbMinute.SelectedItem.ToString()!) : 0;

            DateTime startTime = dpShowDate.SelectedDate.Value.Date.AddHours(hour).AddMinutes(minute);
            DateTime endTime = startTime.AddMinutes(selMovie.Duration + 15); // Duration + 15 mins cleaning

            txtCalculatedEndTime.Text = $"{endTime:HH:mm} • {endTime:dd/MM/yyyy} ({selMovie.Duration}p chiếu + 15p dọn phòng)";

            // Golden Hour Rule: Weekend or 18h-22h
            bool isWeekend = (startTime.DayOfWeek == DayOfWeek.Saturday || startTime.DayOfWeek == DayOfWeek.Sunday);
            bool isPeakHour = (startTime.Hour >= 18 && startTime.Hour <= 22);

            if (isWeekend || isPeakHour)
            {
                txtPriceSuggestionHint.Visibility = Visibility.Visible;
                if (txtBasePrice.Text == "75000" || string.IsNullOrWhiteSpace(txtBasePrice.Text))
                {
                    txtBasePrice.Text = "90000"; // +20%
                }
            }
            else
            {
                txtPriceSuggestionHint.Visibility = Visibility.Collapsed;
                if (txtBasePrice.Text == "90000")
                {
                    txtBasePrice.Text = "75000";
                }
            }
        }

        private bool CheckShowtimeConflict(int roomId, DateTime newStart, DateTime newEnd, int currentShowtimeId, out string conflictMsg)
        {
            conflictMsg = string.Empty;
            try
            {
                using var db = new CinemaManagementDbContext();
                var conflict = db.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.Room)
                    .FirstOrDefault(s => s.RoomId == roomId
                        && s.Status
                        && s.ShowtimeId != currentShowtimeId
                        && newStart < s.EndTime
                        && newEnd > s.StartTime);

                if (conflict != null)
                {
                    conflictMsg = $"⚠️ TRÙNG LỊCH CHIẾU TẠI {conflict.Room?.RoomName?.ToUpper()}!\n\nPhòng này đã có lịch chiếu phim '{conflict.Movie?.Title}' từ {conflict.StartTime:HH:mm} đến {conflict.EndTime:HH:mm} ngày {conflict.StartTime:dd/MM/yyyy}.\n\nVui lòng chọn khung giờ khác hoặc phòng chiếu khác.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                conflictMsg = ex.Message;
                return true;
            }
            return false;
        }

        private void DgShowtimes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgShowtimes.SelectedItem is not ShowtimeDisplayItem item) return;

            _selectedShowtime = item;

            // Set Movie
            foreach (var m in cbMovie.Items)
            {
                if (m is Movie mov && mov.MovieId == item.MovieId)
                {
                    cbMovie.SelectedItem = mov;
                    break;
                }
            }

            // Set Room
            foreach (var r in cbRoom.Items)
            {
                if (r is Room rm && rm.RoomId == item.RoomId)
                {
                    cbRoom.SelectedItem = rm;
                    break;
                }
            }

            // Set Date & Time
            dpShowDate.SelectedDate = item.StartTime.Date;
            cbHour.SelectedItem = item.StartTime.Hour.ToString("00");
            cbMinute.SelectedItem = item.StartTime.Minute.ToString("00");

            txtBasePrice.Text = item.BasePrice.ToString("0");
            chkShowtimeStatus.IsChecked = item.Status;

            CbMovieOrTime_Changed(sender, e);
        }

        private void BtnAddShowtime_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateShowtimeForm(out int movieId, out int roomId, out DateTime startTime, out DateTime endTime, out decimal basePrice))
            {
                return;
            }

            // Anti-conflict algorithm (TASK-14)
            if (CheckShowtimeConflict(roomId, startTime, endTime, 0, out string conflictMsg))
            {
                CinemaMessageBox.Show(conflictMsg, "Xung đột lịch chiếu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                using var transaction = db.Database.BeginTransaction();

                // Kiểm tra lại lần 2 bên trong Transaction để ngăn chặn Race Condition khi 2 Admin cùng xếp lịch đồng thời
                if (CheckShowtimeConflict(roomId, startTime, endTime, 0, out string conflictRaceMsg))
                {
                    transaction.Rollback();
                    CinemaMessageBox.Show(conflictRaceMsg, "Xung đột lịch chiếu (Bảo vệ Race Condition)", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var st = new Showtime
                {
                    MovieId = movieId,
                    RoomId = roomId,
                    StartTime = startTime,
                    EndTime = endTime,
                    BasePrice = basePrice,
                    Status = chkShowtimeStatus.IsChecked == true
                };

                db.Showtimes.Add(st);
                db.SaveChanges();
                transaction.Commit();

                CinemaMessageBox.Show("Thêm suất chiếu mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadShowtimes();
                BtnClearShowtimeForm_Click(sender, e);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi thêm suất chiếu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdateShowtime_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShowtime == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 suất chiếu từ danh sách để cập nhật!", "Chưa chọn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateShowtimeForm(out int movieId, out int roomId, out DateTime startTime, out DateTime endTime, out decimal basePrice))
            {
                return;
            }

            // Anti-conflict algorithm (excluding currentId)
            if (CheckShowtimeConflict(roomId, startTime, endTime, _selectedShowtime.ShowtimeId, out string conflictMsg))
            {
                CinemaMessageBox.Show(conflictMsg, "Xung đột lịch chiếu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                using var transaction = db.Database.BeginTransaction();

                if (CheckShowtimeConflict(roomId, startTime, endTime, _selectedShowtime.ShowtimeId, out string conflictRaceMsg))
                {
                    transaction.Rollback();
                    CinemaMessageBox.Show(conflictRaceMsg, "Xung đột lịch chiếu (Bảo vệ Race Condition)", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var st = db.Showtimes.Find(_selectedShowtime.ShowtimeId);
                if (st != null)
                {
                    st.MovieId = movieId;
                    st.RoomId = roomId;
                    st.StartTime = startTime;
                    st.EndTime = endTime;
                    st.BasePrice = basePrice;
                    st.Status = chkShowtimeStatus.IsChecked == true;

                    db.SaveChanges();
                    transaction.Commit();

                    CinemaMessageBox.Show("Cập nhật thông tin suất chiếu thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadShowtimes();
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToggleStatusShowtime_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShowtime == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 suất chiếu để hủy/đổi trạng thái!", "Chưa chọn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string action = _selectedShowtime.Status ? "HỦY SUẤT CHIẾU" : "MỞ BÁN LẠI";
            var confirm = CinemaMessageBox.Show(
                $"Bạn có chắc chắn muốn {action} suất chiếu lúc {_selectedShowtime.StartTime:HH:mm dd/MM/yyyy} phòng {_selectedShowtime.RoomName}?",
                "Xác nhận thay đổi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new CinemaManagementDbContext();
                    var st = db.Showtimes.Find(_selectedShowtime.ShowtimeId);
                    if (st != null)
                    {
                        st.Status = !st.Status;
                        db.SaveChanges();
                        LoadShowtimes();
                    }
                }
                catch (Exception ex)
                {
                    CinemaMessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClearShowtimeForm_Click(object sender, RoutedEventArgs e)
        {
            _selectedShowtime = null;
            dgShowtimes.SelectedItem = null;
            if (cbMovie.Items.Count > 0) cbMovie.SelectedIndex = 0;
            if (cbRoom.Items.Count > 0) cbRoom.SelectedIndex = 0;
            dpShowDate.SelectedDate = DateTime.Today;
            cbHour.SelectedIndex = 11;
            cbMinute.SelectedIndex = 0;
            txtBasePrice.Text = "75000";
            chkShowtimeStatus.IsChecked = true;
            CbMovieOrTime_Changed(sender, e);
        }

        private bool ValidateShowtimeForm(out int movieId, out int roomId, out DateTime startTime, out DateTime endTime, out decimal basePrice)
        {
            movieId = 0;
            roomId = 0;
            startTime = DateTime.Now;
            endTime = DateTime.Now;
            basePrice = 0;

            if (cbMovie.SelectedItem is not Movie selMovie)
            {
                CinemaMessageBox.Show("Vui lòng chọn phim chiếu!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            movieId = selMovie.MovieId;

            if (cbRoom.SelectedItem is not Room selRoom)
            {
                CinemaMessageBox.Show("Vui lòng chọn phòng chiếu!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            roomId = selRoom.RoomId;

            if (!dpShowDate.SelectedDate.HasValue)
            {
                CinemaMessageBox.Show("Vui lòng chọn ngày chiếu!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            int hour = cbHour.SelectedIndex >= 0 ? int.Parse(cbHour.SelectedItem.ToString()!) : 19;
            int minute = cbMinute.SelectedIndex >= 0 ? int.Parse(cbMinute.SelectedItem.ToString()!) : 0;

            startTime = dpShowDate.SelectedDate.Value.Date.AddHours(hour).AddMinutes(minute);
            endTime = startTime.AddMinutes(selMovie.Duration + 15);

            if (!decimal.TryParse(txtBasePrice.Text.Trim(), out basePrice) || basePrice <= 0)
            {
                CinemaMessageBox.Show("Giá vé cơ bản phải là số dương (> 0 VND)!", "Giá không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBasePrice.Focus();
                return false;
            }

            if (basePrice > 200000m)
            {
                CinemaMessageBox.Show(
                    "QUY ĐỊNH GIÁ VÉ RẠP KPID CINEMA:\n\nGiá vé của phim/suất chiếu không được vượt quá 200,000 VND!\n\nVui lòng nhập mức giá hợp lệ (<= 200,000 VND).",
                    "Giá vé vượt giới hạn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtBasePrice.Focus();
                return false;
            }

            return true;
        }
        #endregion

        #region Room Management
        private void LoadRooms()
        {
            try
            {
                using var db = new CinemaManagementDbContext();
                var rooms = db.Rooms.OrderBy(r => r.RoomName).ToList();

                var display = rooms.Select(r => new RoomDisplayItem
                {
                    RoomId = r.RoomId,
                    RoomName = r.RoomName,
                    TotalSeats = r.TotalSeats,
                    Status = r.Status
                }).ToList();

                dgRooms.ItemsSource = display;
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải danh sách phòng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRooms.SelectedItem is not RoomDisplayItem item) return;

            _selectedRoom = item;
            txtRoomName.Text = item.RoomName;
            txtTotalSeats.Text = item.TotalSeats.ToString();
            chkRoomStatus.IsChecked = item.Status;
        }

        private void BtnAddRoom_Click(object sender, RoutedEventArgs e)
        {
            string name = txtRoomName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                CinemaMessageBox.Show("Vui lòng nhập tên phòng chiếu!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRoomName.Focus();
                return;
            }

            if (!int.TryParse(txtTotalSeats.Text.Trim(), out int seats) || seats <= 0)
            {
                CinemaMessageBox.Show("Tổng số ghế phải là số nguyên dương (chuẩn 40 ghế)!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                if (db.Rooms.Any(r => r.RoomName.ToLower() == name.ToLower()))
                {
                    CinemaMessageBox.Show($"Phòng '{name}' đã tồn tại trong danh sách!", "Trùng tên phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var room = new Room
                {
                    RoomName = name,
                    TotalSeats = seats,
                    Status = chkRoomStatus.IsChecked == true
                };

                db.Rooms.Add(room);
                db.SaveChanges();

                CinemaMessageBox.Show($"Thêm phòng '{name}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRooms();
                LoadFormData();
                BtnClearRoomForm_Click(sender, e);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi thêm phòng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdateRoom_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoom == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 phòng chiếu để cập nhật!", "Chưa chọn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string name = txtRoomName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                CinemaMessageBox.Show("Tên phòng chiếu không được để trống!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtTotalSeats.Text.Trim(), out int seats) || seats <= 0)
            {
                CinemaMessageBox.Show("Số ghế phải là số nguyên dương!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                if (db.Rooms.Any(r => r.RoomId != _selectedRoom.RoomId && r.RoomName.ToLower() == name.ToLower()))
                {
                    CinemaMessageBox.Show($"Tên phòng '{name}' đã bị trùng!", "Trùng tên", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var room = db.Rooms.Find(_selectedRoom.RoomId);
                if (room != null)
                {
                    room.RoomName = name;
                    room.TotalSeats = seats;
                    room.Status = chkRoomStatus.IsChecked == true;

                    db.SaveChanges();

                    CinemaMessageBox.Show("Cập nhật phòng chiếu thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadRooms();
                    LoadFormData();
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearRoomForm_Click(object sender, RoutedEventArgs e)
        {
            _selectedRoom = null;
            dgRooms.SelectedItem = null;
            txtRoomName.Text = string.Empty;
            txtTotalSeats.Text = "40";
            chkRoomStatus.IsChecked = true;
        }
        #endregion
    }
}
