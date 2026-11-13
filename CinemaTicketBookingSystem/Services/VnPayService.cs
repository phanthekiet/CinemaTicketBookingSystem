using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaTicketBookingSystem.Services
{
    public class VnPayConfig
    {
        public string TmnCode { get; set; } = "XAG3UJEO";
        public string HashSecret { get; set; } = "IAEHIMJRECAWCVCTTIWETDYETPXXMLOD";
        public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public string ReturnUrl { get; set; } = "http://localhost:8888/vnpay-return/";

        public static VnPayConfig Load()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                return new VnPayConfig
                {
                    TmnCode = config["VnPay:TmnCode"] ?? "XAG3UJEO",
                    HashSecret = config["VnPay:HashSecret"] ?? "IAEHIMJRECAWCVCTTIWETDYETPXXMLOD",
                    BaseUrl = config["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                    ReturnUrl = config["VnPay:ReturnUrl"] ?? "http://localhost:8888/vnpay-return/"
                };
            }
            catch
            {
                return new VnPayConfig();
            }
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.CompareOrdinal(x, y);
        }
    }

    public class VnPayPaymentResult
    {
        public bool Success { get; set; }
        public string ResponseCode { get; set; } = string.Empty;
        public string TransactionNo { get; set; } = string.Empty;
        public string TxnRef { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class VnPayService
    {
        private readonly VnPayConfig _config;
        private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;

        public VnPayService()
        {
            _config = VnPayConfig.Load();
        }

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _requestData[key] = value;
            }
        }

        public string CreatePaymentUrl(decimal amount, string orderInfo, string txnRef, string ipAddress = "127.0.0.1")
        {
            _requestData.Clear();

            long amountInVndCents = (long)(amount * 100);

            AddRequestData("vnp_Version", "2.1.0");
            AddRequestData("vnp_Command", "pay");
            AddRequestData("vnp_TmnCode", _config.TmnCode);
            AddRequestData("vnp_Amount", amountInVndCents.ToString());
            AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
            AddRequestData("vnp_CurrCode", "VND");
            AddRequestData("vnp_IpAddr", ipAddress);
            AddRequestData("vnp_Locale", "vn");
            AddRequestData("vnp_OrderInfo", orderInfo);
            AddRequestData("vnp_OrderType", "other");
            AddRequestData("vnp_ReturnUrl", _config.ReturnUrl);
            AddRequestData("vnp_TxnRef", txnRef);
            AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));

            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                if (data.Length > 0)
                {
                    data.Append('&');
                }
                data.Append(WebUtility.UrlEncode(kv.Key));
                data.Append('=');
                data.Append(WebUtility.UrlEncode(kv.Value));
            }

            string queryString = data.ToString();
            string secureHash = HmacSHA512(_config.HashSecret, queryString);
            string paymentUrl = $"{_config.BaseUrl}?{queryString}&vnp_SecureHash={secureHash}";

            return paymentUrl;
        }

        public bool ValidateSignature(Dictionary<string, string> responseParams, string inputHash)
        {
            var sortedParams = new SortedList<string, string>(new VnPayCompare());
            foreach (var kv in responseParams)
            {
                if (!string.IsNullOrEmpty(kv.Key) &&
                    kv.Key.StartsWith("vnp_") &&
                    kv.Key != "vnp_SecureHash" &&
                    kv.Key != "vnp_SecureHashType")
                {
                    sortedParams[kv.Key] = kv.Value;
                }
            }

            var data = new StringBuilder();
            foreach (var kv in sortedParams)
            {
                if (data.Length > 0)
                {
                    data.Append('&');
                }
                data.Append(WebUtility.UrlEncode(kv.Key));
                data.Append('=');
                data.Append(WebUtility.UrlEncode(kv.Value));
            }

            string rawHash = HmacSHA512(_config.HashSecret, data.ToString());
            return string.Equals(rawHash, inputHash, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<VnPayPaymentResult> WaitForCallbackAsync(string expectedTxnRef, CancellationToken cancellationToken)
        {
            StopListener();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listener = new HttpListener();
            
            // Format prefix for HttpListener
            string prefix = _config.ReturnUrl;
            if (!prefix.EndsWith("/")) prefix += "/";
            _listener.Prefixes.Add(prefix);

            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                return new VnPayPaymentResult
                {
                    Success = false,
                    Message = $"Không thể mở cổng nhận phản hồi thanh toán ({_config.ReturnUrl}): {ex.Message}"
                };
            }

            try
            {
                using (_cts.Token.Register(() =>
                {
                    try { _listener?.Stop(); } catch { }
                }))
                {
                    var context = await _listener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    // Parse query parameters
                    var queryDict = new Dictionary<string, string>();
                    foreach (string? key in request.QueryString.AllKeys)
                    {
                        if (!string.IsNullOrEmpty(key))
                        {
                            queryDict[key] = request.QueryString[key] ?? string.Empty;
                        }
                    }

                    string responseCode = queryDict.GetValueOrDefault("vnp_ResponseCode", "");
                    string txnRef = queryDict.GetValueOrDefault("vnp_TxnRef", "");
                    string transactionNo = queryDict.GetValueOrDefault("vnp_TransactionNo", "");
                    string bankCode = queryDict.GetValueOrDefault("vnp_BankCode", "");
                    string secureHash = queryDict.GetValueOrDefault("vnp_SecureHash", "");
                    string amountStr = queryDict.GetValueOrDefault("vnp_Amount", "0");

                    decimal.TryParse(amountStr, out decimal parsedAmount);
                    parsedAmount /= 100;

                    bool isValidSig = ValidateSignature(queryDict, secureHash);
                    bool isSuccess = isValidSig && responseCode == "00";

                    // Return friendly HTML page to customer's browser
                    string html = GetCallbackHtml(isSuccess, responseCode, txnRef, parsedAmount);
                    byte[] buffer = Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html; charset=utf-8";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();

                    string message = isSuccess ? "Giao dịch thành công!" :
                                     responseCode == "24" ? "Khách hàng đã hủy giao dịch trên VNPAY." :
                                     !isValidSig ? "Chữ ký bảo mật không hợp lệ (Checksum Error)." :
                                     $"Giao dịch không thành công. Mã lỗi VNPAY: {responseCode}";

                    return new VnPayPaymentResult
                    {
                        Success = isSuccess,
                        ResponseCode = responseCode,
                        TransactionNo = transactionNo,
                        TxnRef = txnRef,
                        Amount = parsedAmount,
                        BankCode = bankCode,
                        Message = message
                    };
                }
            }
            catch (OperationCanceledException)
            {
                return new VnPayPaymentResult
                {
                    Success = false,
                    ResponseCode = "CANCELLED",
                    Message = "Quá trình thanh toán đã bị hủy bởi người dùng."
                };
            }
            catch (Exception ex)
            {
                return new VnPayPaymentResult
                {
                    Success = false,
                    Message = $"Lỗi trong quá trình nhận phản hồi VNPAY: {ex.Message}"
                };
            }
            finally
            {
                StopListener();
            }
        }

        public void StopListener()
        {
            try
            {
                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                }
            }
            catch { }
            finally
            {
                _listener = null;
            }
        }

        private static string HmacSHA512(string key, string inputData)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            var hex = new StringBuilder(hashValue.Length * 2);
            foreach (byte b in hashValue)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        private static string GetCallbackHtml(bool success, string code, string txnRef, decimal amount)
        {
            string icon = success ? "🎉" : "⚠️";
            string title = success ? "THANH TOÁN THÀNH CÔNG!" : "THANH TOÁN CHƯA HOÀN TẤT";
            string titleColor = success ? "#86EFAC" : "#F87171";
            string badgeText = success ? "VÉ ĐIỆN TỬ ĐÃ KHỞI TẠO" : $"MÃ PHẢN HỒI: {code}";
            string badgeBg = success ? "#14532D" : "#451A1A";
            string badgeFg = success ? "#86EFAC" : "#FCA5A5";
            string subtitle = success
                ? $"Đơn hàng <strong>{txnRef}</strong> với số tiền <strong>{amount:N0} đ</strong> đã được VNPAY Sandbox xác nhận thanh toán."
                : "Giao dịch thanh toán chưa hoàn thành hoặc đã bị hủy từ người dùng.";

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>KPID CINEMA - Kết quả thanh toán VNPAY</title>
    <style>
        body {{
            background-color: #0E1015;
            color: #F3F4F6;
            font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, sans-serif;
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100vh;
            margin: 0;
            padding: 16px;
            box-sizing: border-box;
        }}
        .card {{
            background: #161922;
            border: 1px solid #2B3142;
            border-radius: 16px;
            padding: 40px 32px;
            text-align: center;
            max-width: 480px;
            width: 100%;
            box-shadow: 0 20px 40px rgba(0,0,0,0.6);
        }}
        .logo-bar {{
            font-size: 18px;
            font-weight: 900;
            margin-bottom: 24px;
        }}
        .logo-red {{ color: #E50914; }}
        .icon {{ font-size: 56px; margin-bottom: 12px; }}
        h2 {{ color: {titleColor}; margin: 0 0 12px 0; font-size: 22px; }}
        p {{ color: #9CA3AF; font-size: 14px; line-height: 1.6; margin: 0 0 20px 0; }}
        .badge {{
            display: inline-block;
            background: {badgeBg};
            color: {badgeFg};
            padding: 8px 18px;
            border-radius: 20px;
            font-weight: bold;
            font-size: 13px;
            letter-spacing: 0.5px;
            margin-bottom: 24px;
        }}
        .footer-note {{
            border-top: 1px solid #2B3142;
            padding-top: 20px;
            font-size: 13px;
            color: #6B7280;
        }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""logo-bar""><span style=""color: #E50914;"">KPID</span> <span style=""letter-spacing: 2px;"">CINEMA</span></div>
        <div class=""icon"">{icon}</div>
        <h2>{title}</h2>
        <p>{subtitle}</p>
        <div class=""badge"">{badgeText}</div>
        <div class=""footer-note"">
            Bạn có thể đóng tab này và quay lại ứng dụng <strong>KPID Cinema</strong> để tiếp tục.
        </div>
    </div>
</body>
</html>";
        }
    }
}

