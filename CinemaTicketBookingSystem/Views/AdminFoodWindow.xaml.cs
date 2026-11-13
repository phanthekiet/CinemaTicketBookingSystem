using CinemaTicketBookingSystem.Entities;
using CinemaTicketBookingSystem.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CinemaTicketBookingSystem.Views
{
    public partial class AdminFoodWindow : Window
    {
        public class FoodDisplayItem
        {
            public int FoodId { get; set; }
            public string FoodName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string PriceString => $"{Price:N0} đ";
            public int Stock { get; set; }
            public string StockString => $"{Stock} phần";
            public string StockBg => Stock <= 5 ? "#7F1D1D" : "#1F2937";
            public string StockFg => Stock <= 5 ? "#FCA5A5" : "#F3F4F6";
            public bool Status { get; set; }
            public string StatusText => Status ? "Đang bán" : "Ngừng bán";
            public string StatusBg => Status ? "#14532D" : "#374151";
            public string StatusFg => Status ? "#86EFAC" : "#9CA3AF";
        }

        private List<FoodDisplayItem> _allFoods = new();
        private FoodDisplayItem? _selectedFood;

        public AdminFoodWindow()
        {
            InitializeComponent();
            Loaded += AdminFoodWindow_Loaded;
        }

        private void AdminFoodWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtAdminName.Text = $"Admin: {UserSession.CurrentUser?.FullName ?? "Quản trị viên"}";
            LoadFoods();
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

        #region Food CRUD
        private void LoadFoods()
        {
            try
            {
                using var db = new CinemaManagementDbContext();
                var list = db.FoodAndDrinks.OrderBy(f => f.FoodName).ToList();

                _allFoods = list.Select(f => new FoodDisplayItem
                {
                    FoodId = f.FoodId,
                    FoodName = f.FoodName,
                    Price = f.Price,
                    Stock = f.Stock,
                    Status = f.Status
                }).ToList();

                ApplyFoodFilter();
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi tải danh sách bắp nước: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFoodFilter()
        {
            if (!IsLoaded || dgFoods == null || txtSearchFood == null || txtFoodStats == null) return;

            string kw = txtSearchFood.Text.Trim();
            var filtered = string.IsNullOrEmpty(kw)
                ? _allFoods
                : _allFoods.Where(f => VietnameseHelper.ContainsUnaccented(f.FoodName, kw)).ToList();

            dgFoods.ItemsSource = filtered;
            txtFoodStats.Text = $"Hiển thị: {filtered.Count} / {_allFoods.Count} món ăn & combo";
        }

        private void TxtSearchFood_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFoodFilter();
        }

        private void BtnReloadFood_Click(object sender, RoutedEventArgs e)
        {
            txtSearchFood.Text = string.Empty;
            LoadFoods();
        }

        private void DgFoods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFoods.SelectedItem is not FoodDisplayItem item) return;

            _selectedFood = item;
            txtFoodName.Text = item.FoodName;
            txtPrice.Text = item.Price.ToString("0");
            txtStock.Text = item.Stock.ToString();
            chkFoodStatus.IsChecked = item.Status;
        }

        private void BtnQuickAdd10_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtStock.Text.Trim(), out int current))
            {
                txtStock.Text = (current + 10).ToString();
            }
            else
            {
                txtStock.Text = "10";
            }
        }

        private void BtnQuickAdd50_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtStock.Text.Trim(), out int current))
            {
                txtStock.Text = (current + 50).ToString();
            }
            else
            {
                txtStock.Text = "50";
            }
        }

        private void BtnAddFood_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFoodForm(out string name, out decimal price, out int stock))
            {
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                if (db.FoodAndDrinks.Any(f => f.FoodName.ToLower() == name.ToLower()))
                {
                    CinemaMessageBox.Show($"Món '{name}' đã có trong thực đơn!", "Trùng tên món", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtFoodName.Focus();
                    return;
                }

                var food = new FoodAndDrink
                {
                    FoodName = name,
                    Price = price,
                    Stock = stock,
                    Status = chkFoodStatus.IsChecked == true
                };

                db.FoodAndDrinks.Add(food);
                db.SaveChanges();

                CinemaMessageBox.Show($"Thêm món '{name}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadFoods();
                BtnClearFoodForm_Click(sender, e);
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi thêm món: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdateFood_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFood == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 món từ danh sách để cập nhật!", "Chưa chọn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateFoodForm(out string name, out decimal price, out int stock))
            {
                return;
            }

            try
            {
                using var db = new CinemaManagementDbContext();
                if (db.FoodAndDrinks.Any(f => f.FoodId != _selectedFood.FoodId && f.FoodName.ToLower() == name.ToLower()))
                {
                    CinemaMessageBox.Show($"Tên món '{name}' đã bị trùng với món khác!", "Trùng tên", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var food = db.FoodAndDrinks.Find(_selectedFood.FoodId);
                if (food != null)
                {
                    food.FoodName = name;
                    food.Price = price;
                    food.Stock = stock;
                    food.Status = chkFoodStatus.IsChecked == true;

                    db.SaveChanges();

                    CinemaMessageBox.Show($"Cập nhật món '{name}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadFoods();
                }
            }
            catch (Exception ex)
            {
                CinemaMessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToggleStatusFood_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFood == null)
            {
                CinemaMessageBox.Show("Vui lòng chọn 1 món từ danh sách để đổi trạng thái!", "Chưa chọn", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newStatus = _selectedFood.Status ? "NGỪNG KINH DOANH" : "MỞ BÁN LẠI";
            var result = CinemaMessageBox.Show(
                $"Bạn có chắc chắn muốn {newStatus} món '{_selectedFood.FoodName}'?",
                "Xác nhận thay đổi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new CinemaManagementDbContext();
                    var food = db.FoodAndDrinks.Find(_selectedFood.FoodId);
                    if (food != null)
                    {
                        food.Status = !food.Status;
                        db.SaveChanges();
                        LoadFoods();
                    }
                }
                catch (Exception ex)
                {
                    CinemaMessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClearFoodForm_Click(object sender, RoutedEventArgs e)
        {
            _selectedFood = null;
            dgFoods.SelectedItem = null;
            txtFoodName.Text = string.Empty;
            txtPrice.Text = string.Empty;
            txtStock.Text = string.Empty;
            chkFoodStatus.IsChecked = true;
        }

        private bool ValidateFoodForm(out string name, out decimal price, out int stock)
        {
            name = txtFoodName.Text.Trim();
            price = 0;
            stock = 0;

            if (string.IsNullOrWhiteSpace(name))
            {
                CinemaMessageBox.Show("Vui lòng nhập tên món ăn / combo!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFoodName.Focus();
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out price) || price <= 0)
            {
                CinemaMessageBox.Show("Giá bán món ăn phải là số dương (> 0 VND)!", "Giá không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrice.Focus();
                return false;
            }

            if (!int.TryParse(txtStock.Text.Trim(), out stock) || stock < 0)
            {
                CinemaMessageBox.Show("Số lượng tồn kho phải là số nguyên không âm (>= 0)!", "Tồn kho không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStock.Focus();
                return false;
            }

            return true;
        }
        #endregion
    }
}
