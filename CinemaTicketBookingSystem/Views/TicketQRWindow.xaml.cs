using CinemaTicketBookingSystem.Entities;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CinemaTicketBookingSystem.Views
{
    public partial class TicketQRWindow : Window
    {
        private int _bookingId;

        public TicketQRWindow() : this(0)
        {
        }

        public TicketQRWindow(int bookingId)
        {
            InitializeComponent();
            _bookingId = bookingId;
            Loaded += TicketQRWindow_Loaded;
        }

        private void TicketQRWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new CinemaManagementDbContext();

                // If bookingId == 0, find latest booking in DB
                if (_bookingId <= 0)
                {
                    var latest = db.Bookings.OrderByDescending(b => b.BookingId).FirstOrDefault();
                    if (latest != null)
                    {
                        _bookingId = latest.BookingId;
                    }
                }

                var booking = db.Bookings
                    .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                    .Include(b => b.Showtime).ThenInclude(s => s.Room)
                    .Include(b => b.Tickets)
                    .Include(b => b.BookingFoods).ThenInclude(bf => bf.Food)
                    .Include(b => b.User)
                    .FirstOrDefault(b => b.BookingId == _bookingId);

                if (booking == null)
                {
                    CinemaMessageBox.Show("Không tìm thấy thông tin đơn vé này trong hệ thống!", "Không tìm thấy vé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Populate UI
                txtMovieTitle.Text = booking.Showtime?.Movie?.Title?.ToUpper() ?? "VÉ XEM PHIM";
                txtShowtime.Text = $"{booking.Showtime?.StartTime:HH:mm} • {booking.Showtime?.StartTime:dd/MM/yyyy}";
                txtRoom.Text = $"{booking.Showtime?.Room?.RoomName ?? "Phòng 01"} (Laser Cinema)";
                
                var seats = booking.Tickets.Select(t => t.SeatCode).OrderBy(s => s).ToList();
                txtSeats.Text = seats.Any() ? string.Join(", ", seats) : "Không xác định";

                var foods = booking.BookingFoods.Select(bf => $"{bf.Food?.FoodName ?? "Món"} (x{bf.Quantity})").ToList();
                txtFoods.Text = foods.Any() ? string.Join(", ", foods) : "Không có bắp nước";

                string? customerName = booking.User?.FullName;
                if (string.IsNullOrWhiteSpace(customerName) || customerName.StartsWith("guest_"))
                    customerName = "Khách vãng lai";
                string customerPhone = string.IsNullOrWhiteSpace(booking.User?.Phone) ? "Chưa cung cấp SĐT" : booking.User.Phone;
                txtCustomer.Text = $"{customerName} • {customerPhone}";

                txtBookingCode.Text = booking.QrcodeString;
                txtTotalAmount.Text = $"{booking.TotalAmount:N0} đ";

                // Check-in status
                if (!booking.IsCheckedIn)
                {
                    txtTicketStatus.Text = "● HỢP LỆ (CHƯA SOÁT VÉ)";
                    txtTicketStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    bdrTicketStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14532D"));
                }
                else
                {
                    txtTicketStatus.Text = "✓ ĐÃ SOÁT VÉ (ĐÃ SỬ DỤNG)";
                    txtTicketStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
                    bdrTicketStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"));
                }

                // Poster: Lấy đúng poster như ở màn hình khách
                string poster = booking.Showtime?.Movie?.PosterPath ?? string.Empty;
                int movieId = booking.Showtime?.Movie?.MovieId ?? 0;
                string movieTitle = booking.Showtime?.Movie?.Title ?? string.Empty;
                LoadPoster(poster, movieId, movieTitle);

                // Generate QR Code
                GenerateQRCodeImage(booking.QrcodeString);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi hiển thị vé: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPoster(string posterPath, int movieId, string movieTitle)
        {
            try
            {
                var candidates = new List<string>();

                // 1. Khớp chính xác theo phim như ở màn hình khách CustomerHomeWindow
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

                if (!string.IsNullOrWhiteSpace(posterPath))
                {
                    candidates.Add(posterPath);
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
                            bmp.Freeze();
                            imgPoster.Source = bmp;
                            return;
                        }
                    }

                    var packCandidates = new[]
                    {
                        $"pack://application:,,,/Assets/{fileNameOnly}",
                        $"pack://application:,,,/Images/{fileNameOnly}"
                    };

                    foreach (var packUri in packCandidates)
                    {
                        try
                        {
                            imgPoster.Source = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                            return;
                        }
                        catch { }
                    }
                }

                imgPoster.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/default-poster.png", UriKind.Absolute));
            }
            catch
            {
                imgPoster.Source = null;
            }
        }

        private void GenerateQRCodeImage(string qrText)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                using var ms = new MemoryStream(qrBytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();

                imgQRCode.Source = bmp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("QR Code generation error: " + ex.Message);
            }
        }

        private void BtnPrintTicket_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    printDlg.PrintVisual(TicketCard, $"Ve KPID CINEMA - {txtBookingCode.Text}");
                    CinemaMessageBox.Show("Đã gửi yêu cầu in vé thành công!", "In vé hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi khi in vé: {ex.Message}", "Lỗi in ấn", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBackHome_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
