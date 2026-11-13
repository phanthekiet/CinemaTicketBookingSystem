using CinemaTicketBookingSystem.Entities;
using CinemaTicketBookingSystem.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CinemaTicketBookingSystem.Views
{
    public partial class AdminMovieWindow : Window
    {
        public class MovieDisplayItem
        {
            public int MovieId { get; set; }
            public string Title { get; set; } = string.Empty;
            public int GenreId { get; set; }
            public string GenreName { get; set; } = string.Empty;
            public int Duration { get; set; }
            public string DurationString => $"{Duration} phút";
            public DateOnly ReleaseDate { get; set; }
            public string ReleaseDateString => ReleaseDate.ToString("dd/MM/yyyy");
            public string AgeRating { get; set; } = string.Empty;
            public string PosterPath { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public bool Status { get; set; }
            public string StatusText => Status ? "Đang chiếu" : "Ngừng chiếu";
            public string StatusBg => Status ? "#14532D" : "#374151";
            public string StatusFg => Status ? "#86EFAC" : "#9CA3AF";
        }

        public class GenreDisplayItem
        {
            public int GenreId { get; set; }
            public string GenreName { get; set; } = string.Empty;
            public int MovieCount { get; set; }
        }

        private List<MovieDisplayItem> _allMovies = new();
        private List<Genre> _allGenres = new();
        private MovieDisplayItem? _selectedMovie;
        private GenreDisplayItem? _selectedGenre;
        private string _currentPosterRelativePath = string.Empty;

        public AdminMovieWindow()
        {
            InitializeComponent();
            Loaded += AdminMovieWindow_Loaded;
        }

        private void AdminMovieWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtAdminName.Text = $"Admin: {UserSession.CurrentUser?.FullName ?? "Quản trị viên"}";

            dpReleaseDate.SelectedDate = DateTime.Today;
            cbAgeRating.SelectedIndex = 0;

            LoadGenres();
            LoadMovies();
        }

        #region Navigation
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

        #region Movie Management
        private void LoadMovies()
        {
            try
            {
                using var db = new CinemaManagementDbContext();
                var movies = db.Movies.Include(m => m.Genre).ToList();

                _allMovies = movies.Select(m => new MovieDisplayItem
                {
                    MovieId = m.MovieId,
                    Title = m.Title,
                    GenreId = m.GenreId,
                    GenreName = m.Genre?.GenreName ?? "Điện ảnh",
                    Duration = m.Duration,
                    ReleaseDate = m.ReleaseDate,
                    AgeRating = string.IsNullOrWhiteSpace(m.AgeRating) ? "P" : m.AgeRating.Trim().ToUpper(),
                    PosterPath = m.PosterPath ?? string.Empty,
                    Description = m.Description ?? string.Empty,
                    Status = m.Status
                }).OrderByDescending(m => m.MovieId).ToList();

                ApplyMovieFilters();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải danh sách phim: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyMovieFilters()
        {
            if (!IsLoaded || dgMovies == null || txtSearchTitle == null || txtMovieCount == null) return;

            var query = _allMovies.AsEnumerable();

            // Search by title (Hỗ trợ tìm kiếm cả tiếng Việt có dấu và không dấu)
            string kw = txtSearchTitle.Text.Trim();
            if (!string.IsNullOrEmpty(kw))
            {
                query = query.Where(m => VietnameseHelper.ContainsUnaccented(m.Title, kw));
            }

            // Filter by Genre
            if (cbFilterGenre?.SelectedItem is Genre selGenre && selGenre.GenreId > 0)
            {
                query = query.Where(m => m.GenreId == selGenre.GenreId);
            }

            // Filter by Status
            if (cbFilterStatus?.SelectedItem is ComboBoxItem cbi)
            {
                string statusVal = cbi.Content?.ToString() ?? "";
                if (statusVal == "Đang chiếu") query = query.Where(m => m.Status);
                else if (statusVal == "Ngừng chiếu") query = query.Where(m => !m.Status);
            }

            var filtered = query.ToList();
            dgMovies.ItemsSource = filtered;
            txtMovieCount.Text = $"Hiển thị: {filtered.Count} / {_allMovies.Count} phim";
        }

        private void TxtSearchTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyMovieFilters();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyMovieFilters();
        }

        private void BtnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearchTitle.Text = string.Empty;
            if (cbFilterGenre.Items.Count > 0) cbFilterGenre.SelectedIndex = 0;
            cbFilterStatus.SelectedIndex = 0;
            ApplyMovieFilters();
        }

        private void DgMovies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgMovies.SelectedItem is not MovieDisplayItem item) return;

            _selectedMovie = item;
            txtTitle.Text = item.Title;
            txtDuration.Text = item.Duration.ToString();
            txtDescription.Text = item.Description;
            chkStatus.IsChecked = item.Status;

            // Set Genre
            foreach (var g in cbGenre.Items)
            {
                if (g is Genre gen && gen.GenreId == item.GenreId)
                {
                    cbGenre.SelectedItem = gen;
                    break;
                }
            }

            // Set Age Rating
            foreach (var itm in cbAgeRating.Items)
            {
                if (itm is ComboBoxItem cbi && cbi.Content.ToString()!.StartsWith(item.AgeRating))
                {
                    cbAgeRating.SelectedItem = cbi;
                    break;
                }
            }

            // Set Release Date
            dpReleaseDate.SelectedDate = item.ReleaseDate.ToDateTime(TimeOnly.MinValue);

            // Set Poster
            _currentPosterRelativePath = item.PosterPath;
            txtPosterPath.Text = string.IsNullOrEmpty(item.PosterPath) ? "Chưa có ảnh poster" : item.PosterPath;
            LoadFormPoster(item.PosterPath, item.MovieId, item.Title);
        }

        private void LoadFormPoster(string? path, int movieId = 0, string movieTitle = "")
        {
            try
            {
                var candidates = new List<string>();

                string lower = movieTitle.ToLower();
                if (movieId == 1 || lower.Contains("lật mặt") || lower.Contains("lat mat"))
                    candidates.Add("latmat8.jpg");
                else if (movieId == 2 || lower.Contains("conan"))
                    candidates.Add("conan28.jpg");
                else if (movieId == 3 || lower.Contains("ám ảnh") || lower.Contains("kinh hoàng") || lower.Contains("conjuring"))
                    candidates.Add("conjuring4.jpg");
                else if (movieId == 4 || lower.Contains("doraemon"))
                    candidates.Add("doraemon.jpg");
                else if (movieId == 5 || lower.Contains("chị chị em em"))
                    candidates.Add("chichiemem3.jpg");

                if (!string.IsNullOrWhiteSpace(path))
                {
                    candidates.Add(path);
                }

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                foreach (var c in candidates)
                {
                    string clean = c.TrimStart('/', '\\');
                    string fileNameOnly = Path.GetFileName(clean);

                    var possiblePaths = new List<string>
                    {
                        c,
                        clean,
                        Path.Combine(baseDir, clean),
                        Path.Combine(baseDir, "Assets", clean),
                        Path.Combine(baseDir, "Images", clean),
                        Path.Combine(baseDir, "Assets", fileNameOnly),
                        Path.Combine(baseDir, "Images", fileNameOnly)
                    };

                    foreach (var p in possiblePaths)
                    {
                        if (File.Exists(p))
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.UriSource = new Uri(Path.GetFullPath(p), UriKind.Absolute);
                            bmp.EndInit();
                            imgFormPoster.Source = bmp;
                            return;
                        }
                    }

                    // Try Pack URIs
                    var packCandidates = new[]
                    {
                        $"pack://application:,,,/Assets/{fileNameOnly}",
                        $"pack://application:,,,/Images/{fileNameOnly}"
                    };

                    foreach (var packUri in packCandidates)
                    {
                        try
                        {
                            var bmp = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                            imgFormPoster.Source = bmp;
                            return;
                        }
                        catch { }
                    }
                }

                imgFormPoster.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/default-poster.png", UriKind.Absolute));
            }
            catch
            {
                imgFormPoster.Source = null;
            }
        }

        private void BtnBrowsePoster_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Chọn ảnh Poster phim",
                Filter = "File hình ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Tất cả các file (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string ext = Path.GetExtension(ofd.FileName);
                    string fileName = $"poster_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                    string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    string destPath = Path.Combine(targetDir, fileName);
                    File.Copy(ofd.FileName, destPath, true);

                    _currentPosterRelativePath = $"/Images/{fileName}";
                    txtPosterPath.Text = _currentPosterRelativePath;
                    imgFormPoster.Source = new BitmapImage(new Uri(destPath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    CinemaMessageBox.Show($"Lỗi lưu file ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddMovie_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateMovieForm(out string title, out int genreId, out int duration, out DateOnly releaseDate, out string ageRating))
            {
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                var movie = new Movie
                {
                    Title = title,
                    GenreId = genreId,
                    Duration = duration,
                    ReleaseDate = releaseDate,
                    AgeRating = ageRating,
                    Description = txtDescription.Text.Trim(),
                    PosterPath = _currentPosterRelativePath,
                    Status = chkStatus.IsChecked == true
                };

                db.Movies.Add(movie);
                db.SaveChanges();

                CinemaMessageBox.Show($"Thêm phim '{title}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadMovies();
                BtnClearForm_Click(sender, e);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi thêm phim: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdateMovie_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMovie == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 bộ phim từ danh sách để cập nhật!", "Chưa chọn phim", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateMovieForm(out string title, out int genreId, out int duration, out DateOnly releaseDate, out string ageRating))
            {
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                var movie = db.Movies.Find(_selectedMovie.MovieId);
                if (movie == null)
                {
                    CinemaMessageBox.Show("Không tìm thấy phim cần sửa trong cơ sở dữ liệu!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                movie.Title = title;
                movie.GenreId = genreId;
                movie.Duration = duration;
                movie.ReleaseDate = releaseDate;
                movie.AgeRating = ageRating;
                movie.Description = txtDescription.Text.Trim();
                if (!string.IsNullOrEmpty(_currentPosterRelativePath))
                {
                    movie.PosterPath = _currentPosterRelativePath;
                }
                movie.Status = chkStatus.IsChecked == true;

                db.SaveChanges();

                CinemaMessageBox.Show($"Cập nhật phim '{title}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadMovies();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi cập nhật phim: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToggleStatusMovie_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMovie == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 bộ phim từ danh sách để đổi trạng thái!", "Chưa chọn phim", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newStatusText = _selectedMovie.Status ? "Ngừng chiếu" : "Đang chiếu";
            var result = CinemaMessageBox.Show(
                $"Bạn có chắc chắn muốn chuyển trạng thái phim '{_selectedMovie.Title}' sang '{newStatusText}'?",
                "Xác nhận thay đổi trạng thái",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new CinemaManagementDbContext();
                    var movie = db.Movies.Find(_selectedMovie.MovieId);
                    if (movie != null)
                    {
                        movie.Status = !movie.Status;
                        db.SaveChanges();
                        LoadMovies();
                    }
                }
                catch (Exception ex)
                {
                    CinemaMessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClearForm_Click(object sender, RoutedEventArgs e)
        {
            _selectedMovie = null;
            dgMovies.SelectedItem = null;
            txtTitle.Text = string.Empty;
            txtDuration.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtPosterPath.Text = "Chưa chọn poster";
            _currentPosterRelativePath = string.Empty;
            imgFormPoster.Source = null;
            chkStatus.IsChecked = true;
            dpReleaseDate.SelectedDate = DateTime.Today;
            if (cbGenre.Items.Count > 0) cbGenre.SelectedIndex = 0;
            cbAgeRating.SelectedIndex = 0;
        }

        private bool ValidateMovieForm(out string title, out int genreId, out int duration, out DateOnly releaseDate, out string ageRating)
        {
            title = txtTitle.Text.Trim();
            genreId = 0;
            duration = 0;
            releaseDate = DateOnly.FromDateTime(DateTime.Today);
            ageRating = "P";

            if (string.IsNullOrWhiteSpace(title))
            {
                CinemaMessageBox.Show("Vui lòng nhập tên phim!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTitle.Focus();
                return false;
            }

            if (cbGenre.SelectedItem is not Genre selGenre)
            {
                CinemaMessageBox.Show("Vui lòng chọn thể loại phim!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbGenre.Focus();
                return false;
            }
            genreId = selGenre.GenreId;

            if (!int.TryParse(txtDuration.Text.Trim(), out duration) || duration <= 0)
            {
                CinemaMessageBox.Show("Thời lượng phim phải là số nguyên dương (> 0 phút)!", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDuration.Focus();
                return false;
            }

            if (!dpReleaseDate.SelectedDate.HasValue)
            {
                CinemaMessageBox.Show("Vui lòng chọn ngày khởi chiếu của phim!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpReleaseDate.Focus();
                return false;
            }
            releaseDate = DateOnly.FromDateTime(dpReleaseDate.SelectedDate.Value);

            if (cbAgeRating.SelectedItem is ComboBoxItem cbi)
            {
                string raw = cbi.Content.ToString() ?? "P";
                ageRating = raw.Split(' ')[0];
            }

            return true;
        }
        #endregion

        #region Genre Management
        private void LoadGenres()
        {
            try
            {
                using var db = new CinemaManagementDbContext();
                _allGenres = db.Genres.Include(g => g.Movies).ToList();

                var display = _allGenres.Select(g => new GenreDisplayItem
                {
                    GenreId = g.GenreId,
                    GenreName = g.GenreName,
                    MovieCount = g.Movies.Count
                }).OrderBy(g => g.GenreName).ToList();

                dgGenres.ItemsSource = display;

                // Update ComboBoxes
                cbGenre.ItemsSource = _allGenres;
                cbGenre.DisplayMemberPath = "GenreName";
                cbGenre.SelectedValuePath = "GenreId";
                if (_allGenres.Any()) cbGenre.SelectedIndex = 0;

                // Filter combo with "All" option
                var filterGenres = new List<Genre> { new Genre { GenreId = 0, GenreName = "-- Tất cả thể loại --" } };
                filterGenres.AddRange(_allGenres);
                cbFilterGenre.ItemsSource = filterGenres;
                cbFilterGenre.DisplayMemberPath = "GenreName";
                cbFilterGenre.SelectedValuePath = "GenreId";
                cbFilterGenre.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải danh mục thể loại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgGenres_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgGenres.SelectedItem is not GenreDisplayItem item) return;
            _selectedGenre = item;
            txtGenreName.Text = item.GenreName;
        }

        private void BtnAddGenre_Click(object sender, RoutedEventArgs e)
        {
            string name = txtGenreName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                CinemaMessageBox.Show("Vui lòng nhập tên thể loại mới!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGenreName.Focus();
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                if (db.Genres.Any(g => g.GenreName.ToLower() == name.ToLower()))
                {
                    CinemaMessageBox.Show($"Thể loại '{name}' đã tồn tại trong hệ thống!", "Trùng dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtGenreName.Focus();
                    return;
                }

                db.Genres.Add(new Genre { GenreName = name });
                db.SaveChanges();

                CinemaMessageBox.Show($"Thêm thể loại '{name}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                txtGenreName.Text = string.Empty;
                LoadGenres();
                LoadMovies(); // Refresh movie genres
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi thêm thể loại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdateGenre_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGenre == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 thể loại từ danh sách để sửa!", "Chưa chọn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string name = txtGenreName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                CinemaMessageBox.Show("Tên thể loại không được để trống!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGenreName.Focus();
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                if (db.Genres.Any(g => g.GenreId != _selectedGenre.GenreId && g.GenreName.ToLower() == name.ToLower()))
                {
                    CinemaMessageBox.Show($"Tên thể loại '{name}' đã bị trùng với thể loại khác!", "Trùng lặp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var genre = db.Genres.Find(_selectedGenre.GenreId);
                if (genre != null)
                {
                    genre.GenreName = name;
                    db.SaveChanges();

                    CinemaMessageBox.Show("Cập nhật tên thể loại thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadGenres();
                    LoadMovies();
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi cập nhật thể loại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearGenreForm_Click(object sender, RoutedEventArgs e)
        {
            _selectedGenre = null;
            dgGenres.SelectedItem = null;
            txtGenreName.Text = string.Empty;
        }
        #endregion
    }
}
