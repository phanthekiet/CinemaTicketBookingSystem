using CinemaTicketBookingSystem.Entities;
using CinemaTicketBookingSystem.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CinemaTicketBookingSystem.Views
{
    public partial class CustomerHomeWindow : Window
    {
        // Carousel Slider State
        private DispatcherTimer? _sliderTimer;
        private int _currentSlideIndex = 0;
        private readonly (string ImagePath, string Title, string Desc)[] _slides = new[]
        {
            ("Assets/slider1.png", "ĐẠI TIỆC ĐIỆN ẢNH ĐỈNH CAO", "Trải nghiệm âm thanh Dolby Atmos & Màn chiếu Laser 4K sắc nét từng phân cảnh."),
            ("Assets/slider2.png", "BÙNG NỔ CẢM XÚC CÙNG BOM TẤN", "Giữ ngay vị trí ngồi VIP đẹp nhất phòng chiếu cùng combo bắp nước siêu ưu đãi."),
            ("Assets/slider3.png", "COMBO BẮP NƯỚC ĐỘC QUYỀN", "Bắp rang bơ phô mai caramel giòn rụm kết hợp nước ngọt mát lạnh tiếp sức đam mê điện ảnh.")
        };

        // Movie Cache
        private List<MovieItemViewModel> _allMovies = new();
        private List<Genre> _genres = new();
        private MovieItemViewModel? _selectedModalMovie;
        private string _activeTab = "All"; // "All", "Active", "Upcoming"

        public CustomerHomeWindow()
        {
            InitializeComponent();
            Loaded += CustomerHomeWindow_Loaded;
            Closed += CustomerHomeWindow_Closed;
        }

        private void CustomerHomeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitSlider();
            UpdateUserSessionUI();
            LoadData();

            // Force window to foreground so user sees it immediately on screen
            try
            {
                Activate();
                Topmost = true;
                Topmost = false;
                Focus();
            }
            catch
            {
                // Ignore focus errors
            }
        }

        private void CustomerHomeWindow_Closed(object? sender, EventArgs e)
        {
            _sliderTimer?.Stop();
        }

        #region Slider Carousel
        private void InitSlider()
        {
            UpdateSlideDisplay();

            _sliderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _sliderTimer.Tick += (s, e) =>
            {
                _currentSlideIndex = (_currentSlideIndex + 1) % _slides.Length;
                UpdateSlideDisplay();
            };
            _sliderTimer.Start();
        }

        private void UpdateSlideDisplay()
        {
            if (_slides == null || _slides.Length == 0) return;

            var slide = _slides[_currentSlideIndex];
            txtSliderTitle.Text = slide.Title;
            txtSliderDesc.Text = slide.Desc;

            imgSlider.Source = LoadImageSafely(slide.ImagePath, "Assets/default-poster.png");

            // Update Indicator Dots
            var activeBrush = (Brush)FindResource("AccentRed");
            var inactiveBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));

            dot1.Fill = _currentSlideIndex == 0 ? activeBrush : inactiveBrush;
            dot2.Fill = _currentSlideIndex == 1 ? activeBrush : inactiveBrush;
            dot3.Fill = _currentSlideIndex == 2 ? activeBrush : inactiveBrush;
        }

        private void BtnPrevSlide_Click(object sender, RoutedEventArgs e)
        {
            _currentSlideIndex = (_currentSlideIndex - 1 + _slides.Length) % _slides.Length;
            UpdateSlideDisplay();
            RestartSliderTimer();
        }

        private void BtnNextSlide_Click(object sender, RoutedEventArgs e)
        {
            _currentSlideIndex = (_currentSlideIndex + 1) % _slides.Length;
            UpdateSlideDisplay();
            RestartSliderTimer();
        }

        private void Dot1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _currentSlideIndex = 0;
            UpdateSlideDisplay();
            RestartSliderTimer();
        }

        private void Dot2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _currentSlideIndex = 1;
            UpdateSlideDisplay();
            RestartSliderTimer();
        }

        private void Dot3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _currentSlideIndex = 2;
            UpdateSlideDisplay();
            RestartSliderTimer();
        }

        private void RestartSliderTimer()
        {
            _sliderTimer?.Stop();
            _sliderTimer?.Start();
        }

        private void BtnBannerBook_Click(object sender, RoutedEventArgs e)
        {
            var firstActive = _allMovies.FirstOrDefault(m => m.HasShowtimes);
            if (firstActive != null)
            {
                OpenBooking(firstActive);
            }
            else if (_allMovies.Count > 0)
            {
                OpenMovieDetails(_allMovies[0].MovieId);
            }
        }

        private void BtnBannerExplore_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = string.Empty;
            cboGenre.SelectedIndex = 0;
            _activeTab = "All";
            ApplyFilters();
        }
        #endregion

        #region User Session & Auth
        private void UpdateUserSessionUI()
        {
            var user = UserSession.CurrentUser;
            if (user != null)
            {
                txtUserName.Text = user.FullName ?? user.Username;
                txtUserInitial.Text = string.IsNullOrEmpty(user.FullName) ? "U" : user.FullName.Substring(0, 1).ToUpper();
                btnAuth.Content = "Đăng xuất";
                btnAuth.Foreground = (Brush)FindResource("TextSecondary");

                btnAdminPortal.Visibility = (user.Role == "Admin") ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                txtUserName.Text = "Khách hàng";
                txtUserInitial.Text = "👤";
                btnAuth.Content = "Đăng nhập";
                btnAuth.Foreground = (Brush)FindResource("AccentGold");

                btnAdminPortal.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnAdminPortal_Click(object sender, RoutedEventArgs e)
        {
            var adminWin = new AdminMovieWindow();
            adminWin.Show();
            Close();
        }

        private void BtnAuth_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.CurrentUser != null)
            {
                var result = CinemaMessageBox.Show(
                    "Quý khách có chắc chắn muốn đăng xuất tài khoản?",
                    "Xác nhận đăng xuất",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    UserSession.CurrentUser = null;
                    UpdateUserSessionUI();
                }
            }
            else
            {
                var loginWindow = new LoginWindow();
                loginWindow.Owner = this;
                loginWindow.ShowDialog();
                UpdateUserSessionUI();
            }
        }

        private void BtnMyTickets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var historyWin = new CustomerHistoryWindow();
                historyWin.Owner = this;
                historyWin.ShowDialog();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi hiển thị tra cứu vé: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Data Loading
        private void LoadData()
        {
            try
            {
                using var db = new CinemaManagementDbContext();

                // 1. Load Genres
                _genres = db.Genres.OrderBy(g => g.GenreName).ToList();

                // Populate Genre Combobox
                var genreOptions = new List<GenreOption>
                {
                    new GenreOption { GenreId = 0, GenreName = "Tất cả thể loại" }
                };
                foreach (var g in _genres)
                {
                    genreOptions.Add(new GenreOption { GenreId = g.GenreId, GenreName = g.GenreName });
                }
                cboGenre.ItemsSource = genreOptions;
                cboGenre.DisplayMemberPath = "GenreName";
                cboGenre.SelectedValuePath = "GenreId";
                cboGenre.SelectedIndex = 0;

                // 2. Load Movies with Genre & Showtimes
                var rawMovies = db.Movies
                    .Include(m => m.Genre)
                    .Include(m => m.Showtimes)
                    .ThenInclude(s => s.Room)
                    .ToList();

                _allMovies = rawMovies.Select(m => MapToViewModel(m)).ToList();

                statusIndicator.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                txtDbStatus.Text = $"Đã kết nối cơ sở dữ liệu rạp phim • {_allMovies.Count} phim hiện có";

                ApplyFilters();
            }
            catch (Exception ex)
            {
                statusIndicator.Fill = new SolidColorBrush(Color.FromRgb(245, 166, 35)); // Warning Amber
                txtDbStatus.Text = $"Chế độ xem dự phòng ({ex.Message})";

                // Generate graceful fallback movies for testing so UI never looks empty
                LoadFallbackSampleMovies();
                ApplyFilters();
            }
        }

        private MovieItemViewModel MapToViewModel(Movie m)
        {
            var vm = new MovieItemViewModel
            {
                MovieId = m.MovieId,
                Title = m.Title,
                GenreId = m.GenreId,
                GenreName = m.Genre?.GenreName ?? "Điện ảnh",
                Duration = m.Duration,
                ReleaseDate = m.ReleaseDate,
                PosterPath = m.PosterPath ?? string.Empty,
                AgeRating = string.IsNullOrWhiteSpace(m.AgeRating) ? "P" : m.AgeRating.Trim().ToUpper(),
                Description = string.IsNullOrWhiteSpace(m.Description) ? "Phim chiếu rạp chất lượng cao tại hệ thống rạp KPID CINEMA." : m.Description,
                Status = m.Status
            };

            // Format release date
            vm.ReleaseDateFormatted = $"Khởi chiếu: {m.ReleaseDate:dd/MM/yyyy}";

            // Map Age Rating styling
            vm.AgeRatingBackground = GetAgeRatingBrush(vm.AgeRating);

            // Resolve Poster Image
            vm.PosterSource = ResolveMoviePoster(m.PosterPath, m.MovieId);

            // Map Showtimes
            var now = DateTime.Now;
            var activeShowtimes = m.Showtimes
                .Where(s => s.Status)
                .OrderBy(s => s.StartTime)
                .ToList();

            vm.ShowtimesCount = activeShowtimes.Count;
            vm.HasShowtimes = activeShowtimes.Count > 0;
            vm.ShowtimesList = activeShowtimes.Select(s => new ShowtimeDisplayModel
            {
                ShowtimeId = s.ShowtimeId,
                StartTime = s.StartTime,
                RoomName = s.Room?.RoomName ?? "Phòng chiếu",
                BasePrice = s.BasePrice
            }).ToList();

            return vm;
        }

        private void LoadFallbackSampleMovies()
        {
            _allMovies = new List<MovieItemViewModel>
            {
                new MovieItemViewModel
                {
                    MovieId = 1,
                    Title = "Lật Mặt 8: Vòng Tay Nắng",
                    GenreId = 1,
                    GenreName = "Hành động",
                    Duration = 125,
                    ReleaseDateFormatted = "Khởi chiếu: 27/04/2025",
                    AgeRating = "T16",
                    AgeRatingBackground = (Brush)FindResource("AccentRed"),
                    Description = "Có thể cách ba dạy con chưa đúng, nhưng tình yêu ba dành cho con chưa bao giờ sai. Một bộ phim gia đình giàu cảm xúc của đạo diễn Lý Hải.",
                    Status = true,
                    PosterSource = ResolveMoviePoster("latmat8.jpg", 1),
                    ShowtimesCount = 2,
                    HasShowtimes = true,
                    ShowtimesList = new List<ShowtimeDisplayModel>
                    {
                        new ShowtimeDisplayModel { ShowtimeId = 1, StartTime = DateTime.Today.AddHours(18), RoomName = "Phòng 01 (Standard)", BasePrice = 85000 },
                        new ShowtimeDisplayModel { ShowtimeId = 2, StartTime = DateTime.Today.AddHours(20).AddMinutes(30), RoomName = "Phòng 02 (IMAX)", BasePrice = 100000 }
                    }
                },
                new MovieItemViewModel
                {
                    MovieId = 2,
                    Title = "Thám Tử Lừng Danh Conan: Movie 28",
                    GenreId = 5,
                    GenreName = "Trinh thám",
                    Duration = 110,
                    ReleaseDateFormatted = "Khởi chiếu: 15/07/2026",
                    AgeRating = "T13",
                    AgeRatingBackground = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                    Description = "Cuộc đối đầu nghẹt thở của Conan và tổ chức Áo Đen trong màn sương bí ẩn tại tháp Tokyo.",
                    Status = true,
                    PosterSource = ResolveMoviePoster("conan28.jpg", 2),
                    ShowtimesCount = 1,
                    HasShowtimes = true,
                    ShowtimesList = new List<ShowtimeDisplayModel>
                    {
                        new ShowtimeDisplayModel { ShowtimeId = 3, StartTime = DateTime.Today.AddHours(17).AddMinutes(30), RoomName = "Phòng 02 (IMAX)", BasePrice = 90000 }
                    }
                },
                new MovieItemViewModel
                {
                    MovieId = 3,
                    Title = "Ám Ảnh Kinh Hoàng 4",
                    GenreId = 2,
                    GenreName = "Kinh dị",
                    Duration = 105,
                    ReleaseDateFormatted = "Khởi chiếu: 10/09/2026",
                    AgeRating = "T18",
                    AgeRatingBackground = (Brush)FindResource("AccentRed"),
                    Description = "Hồ sơ tâm linh rùng rợn tiếp theo của vợ chồng nhà ngoại cảm Ed và Lorraine Warren.",
                    Status = true,
                    PosterSource = ResolveMoviePoster("conjuring4.jpg", 3),
                    ShowtimesCount = 1,
                    HasShowtimes = true,
                    ShowtimesList = new List<ShowtimeDisplayModel>
                    {
                        new ShowtimeDisplayModel { ShowtimeId = 4, StartTime = DateTime.Today.AddHours(21).AddMinutes(30), RoomName = "Phòng 03 (VIP)", BasePrice = 110000 }
                    }
                },
                new MovieItemViewModel
                {
                    MovieId = 4,
                    Title = "Doraemon: Bản Giao Hưởng Địa Cầu",
                    GenreId = 3,
                    GenreName = "Hoạt hình",
                    Duration = 115,
                    ReleaseDateFormatted = "Khởi chiếu: 25/05/2026",
                    AgeRating = "P",
                    AgeRatingBackground = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    Description = "Hành trình âm nhạc giải cứu thế giới tràn ngập tiếng cười và sắc màu của nhóm bạn Nobita.",
                    Status = true,
                    PosterSource = ResolveMoviePoster("doraemon.jpg", 4),
                    ShowtimesCount = 0,
                    HasShowtimes = false,
                    ShowtimesList = new List<ShowtimeDisplayModel>()
                }
            };
        }

        private Brush GetAgeRatingBrush(string ageRating)
        {
            return ageRating switch
            {
                "P" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),     // Green
                "K" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),     // Blue
                "T13" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // Amber
                "T16" => new SolidColorBrush(Color.FromRgb(249, 115, 22)),   // Orange
                "T18" => (Brush)FindResource("AccentRed"),                  // Red
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))       // Gray
            };
        }

        private ImageSource ResolveMoviePoster(string? posterPath, int movieId)
        {
            // Try explicit candidate paths
            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(posterPath))
            {
                string clean = posterPath.TrimStart('/', '\\');
                candidates.Add(posterPath);
                candidates.Add(clean);
                candidates.Add(Path.Combine(AppContext.BaseDirectory, clean));
                candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", clean));
                candidates.Add(Path.Combine(AppContext.BaseDirectory, "Images", clean));
                candidates.Add($"pack://application:,,,/Assets/{clean}");
                candidates.Add($"pack://application:,,,/Images/{clean}");
            }

            // Also check standard poster filenames if path was relative or missing
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "latmat8.jpg"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "conan28.jpg"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "conjuring4.jpg"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "doraemon.jpg"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", "chichiemem3.jpg"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Images", "chichiemem3.jpg"));

            foreach (var candidate in candidates)
            {
                try
                {
                    if (candidate.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                    {
                        var uri = new Uri(candidate, UriKind.Absolute);
                        return new BitmapImage(uri);
                    }
                    if (File.Exists(candidate))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(candidate, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }
                catch
                {
                    // Continue to next candidate
                }
            }

            // Fallback to default-poster.png (TASK-09 requirement)
            return LoadImageSafely("Assets/default-poster.png", string.Empty);
        }

        private ImageSource LoadImageSafely(string relativeOrPackPath, string fallbackPath)
        {
            try
            {
                // Check pack URI first
                var packUri = new Uri($"pack://application:,,,/{relativeOrPackPath.TrimStart('/')}", UriKind.Absolute);
                return new BitmapImage(packUri);
            }
            catch
            {
                try
                {
                    // Check local filesystem
                    var localPath = Path.Combine(AppContext.BaseDirectory, relativeOrPackPath);
                    if (File.Exists(localPath))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(localPath, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }
                catch
                {
                    // Ignore
                }
            }

            if (!string.IsNullOrEmpty(fallbackPath))
            {
                try
                {
                    var fallbackUri = new Uri($"pack://application:,,,/{fallbackPath.TrimStart('/')}", UriKind.Absolute);
                    return new BitmapImage(fallbackUri);
                }
                catch
                {
                    // Ignore
                }
            }

            return new BitmapImage();
        }
        #endregion

        #region Search & Filtering (TASK-08 & TASK-09)
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var hasText = !string.IsNullOrWhiteSpace(txtSearch.Text);
            txtSearchPlaceholder.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            btnClearSearch.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            ApplyFilters();
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = string.Empty;
        }

        private void CboGenre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnTabAll_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "All";
            UpdateTabStyles();
            ApplyFilters();
        }

        private void BtnTabActive_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "Active";
            UpdateTabStyles();
            ApplyFilters();
        }

        private void BtnTabUpcoming_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "Upcoming";
            UpdateTabStyles();
            ApplyFilters();
        }

        private void UpdateTabStyles()
        {
            var activeBg = new SolidColorBrush(Color.FromRgb(43, 49, 66));
            var inactiveBg = Brushes.Transparent;

            btnTabAll.Background = _activeTab == "All" ? activeBg : inactiveBg;
            btnTabAll.Foreground = _activeTab == "All" ? Brushes.White : (Brush)FindResource("TextSecondary");

            btnTabActive.Background = _activeTab == "Active" ? activeBg : inactiveBg;
            btnTabActive.Foreground = _activeTab == "Active" ? Brushes.White : (Brush)FindResource("TextSecondary");

            btnTabUpcoming.Background = _activeTab == "Upcoming" ? activeBg : inactiveBg;
            btnTabUpcoming.Foreground = _activeTab == "Upcoming" ? Brushes.White : (Brush)FindResource("TextSecondary");
        }

        private void ApplyFilters()
        {
            var query = _allMovies.AsEnumerable();

            // 1. Text Search Filter (Hỗ trợ tìm kiếm cả tiếng Việt có dấu và không dấu)
            var keyword = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(m =>
                    VietnameseHelper.ContainsUnaccented(m.Title, keyword) ||
                    VietnameseHelper.ContainsUnaccented(m.GenreName, keyword) ||
                    VietnameseHelper.ContainsUnaccented(m.Description, keyword));
            }

            // 2. Genre Dropdown Filter
            if (cboGenre.SelectedValue is int selectedGenreId && selectedGenreId > 0)
            {
                query = query.Where(m => m.GenreId == selectedGenreId);
            }

            // 3. Tab Status Filter (Phân loại chính xác theo ngày khởi chiếu)
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (_activeTab == "Active")
            {
                // Phim đang chiếu: Đã đến hoặc qua ngày khởi chiếu và Status = true
                query = query.Where(m => m.Status && m.ReleaseDate <= today);
            }
            else if (_activeTab == "Upcoming")
            {
                // Phim sắp chiếu: Ngày khởi chiếu lớn hơn ngày hiện tại
                query = query.Where(m => m.Status && m.ReleaseDate > today);
            }

            var filteredList = query.ToList();
            icMovies.ItemsSource = filteredList;
            txtMovieCount.Text = $"{filteredList.Count} phim";

            // Empty state display
            if (filteredList.Count == 0)
            {
                bdrEmptyState.Visibility = Visibility.Visible;
                txtEmptyDetail.Text = !string.IsNullOrEmpty(keyword)
                    ? $"Không có kết quả nào cho từ khóa \"{keyword}\"."
                    : "Không có bộ phim nào trong danh mục đã chọn.";
            }
            else
            {
                bdrEmptyState.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = string.Empty;
            cboGenre.SelectedIndex = 0;
            _activeTab = "All";
            UpdateTabStyles();
            ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
        #endregion

        #region Movie Details Modal (TASK-10)
        private void CardPoster_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is int movieId)
            {
                OpenMovieDetails(movieId);
            }
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int movieId)
            {
                OpenMovieDetails(movieId);
            }
        }

        private void OpenMovieDetails(int movieId)
        {
            var movie = _allMovies.FirstOrDefault(m => m.MovieId == movieId);
            if (movie == null) return;

            _selectedModalMovie = movie;

            txtModalTitle.Text = movie.Title;
            imgModalPoster.Source = movie.PosterSource;
            txtModalGenre.Text = movie.GenreName;
            txtModalDuration.Text = movie.DurationFormatted;
            txtModalReleaseDate.Text = movie.ReleaseDateFormatted;
            txtModalDesc.Text = movie.Description;

            // Age Rating Badge & Notice
            txtModalAge.Text = movie.AgeRating;
            bdrModalAge.Background = movie.AgeRatingBackground;

            txtModalAgeNotice.Text = movie.AgeRating switch
            {
                "T18" => "⚠️ Phim giới hạn độ tuổi T18: Dành cho khán giả từ 18 tuổi trở lên. Rạp sẽ kiểm tra giấy tờ tùy thân (CCCD) trước khi vào phòng chiếu.",
                "T16" => "⚠️ Phim giới hạn độ tuổi T16: Dành cho khán giả từ 16 tuổi trở lên theo quy định của Cục Điện ảnh.",
                "T13" => "ℹ️ Phim T13: Phim cấm phổ biến đến khán giả dưới 13 tuổi.",
                _ => "✅ Phim P: Phim được phép phổ biến rộng rãi đến mọi lứa tuổi khán giả."
            };

            // Showtimes List in Modal
            if (movie.ShowtimesList.Count > 0)
            {
                icModalShowtimes.ItemsSource = movie.ShowtimesList;
                icModalShowtimes.Visibility = Visibility.Visible;
                txtNoShowtimesInModal.Visibility = Visibility.Collapsed;
            }
            else
            {
                icModalShowtimes.ItemsSource = null;
                icModalShowtimes.Visibility = Visibility.Collapsed;
                txtNoShowtimesInModal.Visibility = Visibility.Visible;
            }

            gridModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseModal_Click(object sender, RoutedEventArgs e)
        {
            gridModalOverlay.Visibility = Visibility.Collapsed;
            _selectedModalMovie = null;
        }

        private void ModalBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            gridModalOverlay.Visibility = Visibility.Collapsed;
            _selectedModalMovie = null;
        }

        private void BtnModalBookNow_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedModalMovie != null)
            {
                gridModalOverlay.Visibility = Visibility.Collapsed;
                OpenBooking(_selectedModalMovie);
            }
        }
        #endregion

        #region Booking & Age Verification (TASK-10)
        private void BtnBookTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int movieId)
            {
                var movie = _allMovies.FirstOrDefault(m => m.MovieId == movieId);
                if (movie != null)
                {
                    OpenBooking(movie);
                }
            }
        }

        private void OpenBooking(MovieItemViewModel movie)
        {
            // 1. Age Rating Verification (TASK-10 requirement)
            if (movie.AgeRating == "T16" || movie.AgeRating == "T18")
            {
                var minAge = movie.AgeRating == "T18" ? 18 : 16;
                var ageResult = CinemaMessageBox.Show(
                    $"XÁC NHẬN ĐỘ TUỔI ({movie.AgeRating}):\n\n" +
                    $"Bộ phim \"{movie.Title}\" được phân loại {movie.AgeRating} (Dành cho khán giả từ đủ {minAge} tuổi trở lên).\n" +
                    "Nhân viên soát vé có quyền yêu cầu xuất trình CCCD/giấy tờ hợp lệ trước khi vào phòng chiếu.\n\n" +
                    "Quý khách có xác nhận mình đủ điều kiện độ tuổi để tiếp tục đặt vé?",
                    "Cảnh báo độ tuổi",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (ageResult != MessageBoxResult.OK)
                {
                    return; // User canceled
                }
            }

            // 2. Showtime Availability Check (TASK-10 requirement)
            if (!movie.HasShowtimes || movie.ShowtimesCount == 0)
            {
                CinemaMessageBox.Show(
                    $"Rất tiếc! Bộ phim \"{movie.Title}\" hiện chưa có suất chiếu khả dụng hoặc đã hết suất chiếu trong ngày.\n\n" +
                    "Quý khách vui lòng chọn bộ phim khác hoặc quay lại sau!",
                    "Thông báo lịch chiếu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // 3. Navigate to SeatSelectionWindow (TASK-10 & Kiệt's Window)
            try
            {
                var seatWin = new SeatSelectionWindow(movie.MovieId);
                seatWin.Owner = this;
                seatWin.ShowDialog();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show(
                    $"Không thể mở màn hình Chọn ghế: {ex.Message}",
                    "Lỗi điều hướng",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        #endregion
    }

    #region Helper ViewModels
    public class MovieItemViewModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string DurationFormatted => $"{Duration} phút";
        public DateOnly ReleaseDate { get; set; }
        public string ReleaseDateFormatted { get; set; } = string.Empty;
        public string PosterPath { get; set; } = string.Empty;
        public ImageSource? PosterSource { get; set; }
        public string AgeRating { get; set; } = "P";
        public Brush? AgeRatingBackground { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool Status { get; set; }
        public int ShowtimesCount { get; set; }
        public bool HasShowtimes { get; set; }
        public string ShowtimesCountText
        {
            get
            {
                if (ShowtimesCount > 0)
                {
                    return $"{ShowtimesCount} suất chiếu";
                }

                var today = DateOnly.FromDateTime(DateTime.Today);
                if (ReleaseDate > today)
                {
                    return "Sắp chiếu";
                }

                return "Chưa có suất";
            }
        }
        public List<ShowtimeDisplayModel> ShowtimesList { get; set; } = new();
    }

    public class ShowtimeDisplayModel
    {
        public int ShowtimeId { get; set; }
        public DateTime StartTime { get; set; }
        public string StartTimeFormatted => StartTime.ToString("HH:mm - dd/MM");
        public string RoomName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string PriceFormatted => $"{BasePrice:N0} đ";
    }

    public class GenreOption
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;
    }
    #endregion
}
