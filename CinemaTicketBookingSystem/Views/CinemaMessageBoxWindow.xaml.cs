using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CinemaTicketBookingSystem.Views
{
    public partial class CinemaMessageBoxWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CinemaMessageBoxWindow(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();

            txtCaption.Text = string.IsNullOrWhiteSpace(caption) ? "Thông Báo KPID CINEMA" : caption;
            txtMessage.Text = message;

            ConfigureDialogType(icon);
            ConfigureButtons(button);

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Result = (button == MessageBoxButton.YesNo) ? MessageBoxResult.No : MessageBoxResult.Cancel;
                    Close();
                }
            };
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnCloseX_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        private void ConfigureDialogType(MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Warning:
                    // Amber Gold Warning (Quy định, lưu ý, cảnh báo)
                    bdrTypeBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A2308"));
                    txtTypeBadge.Text = "QUY ĐỊNH & CẢNH BÁO";
                    txtTypeBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                    bdrIconCircle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A2308"));
                    bdrIconCircle.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    bdrIconCircle.BorderThickness = new Thickness(1.5);
                    txtIcon.Text = "⚠️";
                    break;

                case MessageBoxImage.Error:
                    // Crimson Red Error (Lỗi hệ thống, vé không hợp lệ)
                    bdrTypeBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B1215"));
                    txtTypeBadge.Text = "THÔNG BÁO LỖI";
                    txtTypeBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                    bdrIconCircle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B1215"));
                    bdrIconCircle.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E50914"));
                    bdrIconCircle.BorderThickness = new Thickness(1.5);
                    txtIcon.Text = "🚫";
                    break;

                case MessageBoxImage.Information:
                    // Emerald / Cyan Info (Thành công, thông tin rạp)
                    bdrTypeBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#064E3B"));
                    txtTypeBadge.Text = "THÀNH CÔNG";
                    txtTypeBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
                    bdrIconCircle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#064E3B"));
                    bdrIconCircle.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    bdrIconCircle.BorderThickness = new Thickness(1.5);
                    txtIcon.Text = "🎉";
                    break;

                case MessageBoxImage.Question:
                    // Royal Purple / Blue Question (Xác nhận hành động)
                    bdrTypeBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E1065"));
                    txtTypeBadge.Text = "XÁC NHẬN";
                    txtTypeBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C084FC"));
                    bdrIconCircle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E1065"));
                    bdrIconCircle.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6"));
                    bdrIconCircle.BorderThickness = new Thickness(1.5);
                    txtIcon.Text = "❓";
                    break;

                default:
                    // Fallback Info
                    bdrTypeBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#082F49"));
                    txtTypeBadge.Text = "THÔNG BÁO";
                    txtTypeBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
                    bdrIconCircle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#082F49"));
                    bdrIconCircle.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06B6D4"));
                    bdrIconCircle.BorderThickness = new Thickness(1.5);
                    txtIcon.Text = "ℹ️";
                    break;
            }
        }

        private void ConfigureButtons(MessageBoxButton button)
        {
            spButtons.Children.Clear();
            var primaryStyle = (Style)FindResource("DialogPrimaryBtnStyle");
            var ghostStyle = (Style)FindResource("DialogGhostBtnStyle");

            switch (button)
            {
                case MessageBoxButton.OK:
                    var btnOk = new Button
                    {
                        Content = "ĐÃ HIỂU",
                        Style = primaryStyle,
                        IsDefault = true,
                        MinWidth = 120
                    };
                    btnOk.Click += (s, e) => { Result = MessageBoxResult.OK; Close(); };
                    spButtons.Children.Add(btnOk);
                    break;

                case MessageBoxButton.OKCancel:
                    var btnCancel = new Button
                    {
                        Content = "HỦY BỎ",
                        Style = ghostStyle,
                        IsCancel = true,
                        MinWidth = 100,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    btnCancel.Click += (s, e) => { Result = MessageBoxResult.Cancel; Close(); };

                    var btnOkConfirm = new Button
                    {
                        Content = "XÁC NHẬN",
                        Style = primaryStyle,
                        IsDefault = true,
                        MinWidth = 120
                    };
                    btnOkConfirm.Click += (s, e) => { Result = MessageBoxResult.OK; Close(); };

                    spButtons.Children.Add(btnCancel);
                    spButtons.Children.Add(btnOkConfirm);
                    break;

                case MessageBoxButton.YesNo:
                    var btnNo = new Button
                    {
                        Content = "KHÔNG",
                        Style = ghostStyle,
                        MinWidth = 100,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    btnNo.Click += (s, e) => { Result = MessageBoxResult.No; Close(); };

                    var btnYes = new Button
                    {
                        Content = "ĐỒNG Ý",
                        Style = primaryStyle,
                        IsDefault = true,
                        MinWidth = 120
                    };
                    btnYes.Click += (s, e) => { Result = MessageBoxResult.Yes; Close(); };

                    spButtons.Children.Add(btnNo);
                    spButtons.Children.Add(btnYes);
                    break;

                case MessageBoxButton.YesNoCancel:
                    var btnCancel3 = new Button
                    {
                        Content = "HỦY",
                        Style = ghostStyle,
                        IsCancel = true,
                        MinWidth = 90,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    btnCancel3.Click += (s, e) => { Result = MessageBoxResult.Cancel; Close(); };

                    var btnNo3 = new Button
                    {
                        Content = "KHÔNG",
                        Style = ghostStyle,
                        MinWidth = 100,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    btnNo3.Click += (s, e) => { Result = MessageBoxResult.No; Close(); };

                    var btnYes3 = new Button
                    {
                        Content = "ĐỒNG Ý",
                        Style = primaryStyle,
                        IsDefault = true,
                        MinWidth = 110
                    };
                    btnYes3.Click += (s, e) => { Result = MessageBoxResult.Yes; Close(); };

                    spButtons.Children.Add(btnCancel3);
                    spButtons.Children.Add(btnNo3);
                    spButtons.Children.Add(btnYes3);
                    break;
            }
        }
    }

    /// <summary>
    /// Bộ thông báo hộp thoại tuỳ biến chuẩn Cosmic Dark Glassmorphism KPID CINEMA (Taste Skill v2)
    /// Thay thế hoàn toàn cho MessageBox.Show mặc định của Windows
    /// </summary>
    public static class CinemaMessageBox
    {
        public static MessageBoxResult Show(
            string messageBoxText, 
            string caption = "Thông Báo KPID CINEMA", 
            MessageBoxButton button = MessageBoxButton.OK, 
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            return Show(null, messageBoxText, caption, button, icon);
        }

        public static MessageBoxResult Show(
            Window? owner, 
            string messageBoxText, 
            string caption = "Thông Báo KPID CINEMA", 
            MessageBoxButton button = MessageBoxButton.OK, 
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            var dialog = new CinemaMessageBoxWindow(messageBoxText, caption, button, icon);

            if (owner != null && owner.IsVisible)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                var activeWin = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible)
                             ?? Application.Current?.MainWindow;

                if (activeWin != null && activeWin.IsVisible)
                {
                    dialog.Owner = activeWin;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }

            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}

