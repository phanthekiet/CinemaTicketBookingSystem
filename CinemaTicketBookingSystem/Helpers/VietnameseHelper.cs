using System;
using System.Globalization;
using System.Text;

namespace CinemaTicketBookingSystem.Helpers
{
    public static class VietnameseHelper
    {
        /// <summary>
        /// Chuyển đổi chuỗi tiếng Việt có dấu thành không dấu và viết thường
        /// Ví dụ: "Lật Mặt 8: Vòng Tay Nắng" -> "lat mat 8: vong tay nang"
        /// </summary>
        public static string RemoveDiacritics(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Phân rã ký tự có dấu thành ký tự gốc + ký tự tổ hợp dấu thanh (FormD)
            string normalized = text.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // Chuẩn hóa ngược lại FormC và thay thế thủ công chữ đ/Đ
            string result = sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'd');

            return result.ToLowerInvariant();
        }

        /// <summary>
        /// Kiểm tra xem chuỗi nguồn (source) có chứa từ khóa (keyword) hay không,
        /// hỗ trợ tìm kiếm cả tiếng Việt có dấu và không dấu (diacritics-insensitive).
        /// </summary>
        public static bool ContainsUnaccented(string? source, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            if (string.IsNullOrWhiteSpace(source))
                return false;

            // 1. Kiểm tra trực tiếp có dấu (nhanh)
            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. Kiểm tra dạng không dấu (bỏ dấu tiếng Việt)
            string sourceUnaccented = RemoveDiacritics(source);
            string keywordUnaccented = RemoveDiacritics(keyword);

            return sourceUnaccented.Contains(keywordUnaccented, StringComparison.OrdinalIgnoreCase);
        }
    }
}

