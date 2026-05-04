using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

string vnp_TmnCode = "DUM0HWTJ";
string vnp_HashSecret = "QOSUGDTT8IEATOSGA4VBL002BE6E1WGF";
string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
string vnp_Returnurl = "https://localhost:44326/api/orders/vnpay-callback";

long orderId = DateTime.Now.Ticks;
long amount = 100000;
string createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
string expireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss");
string orderInfo = "Thanh toan don hang:" + orderId;

var parameters = new SortedList<string, string>(new VnPayCompare());
parameters.Add("vnp_Version", "2.1.0");
parameters.Add("vnp_Command", "pay");
parameters.Add("vnp_TmnCode", vnp_TmnCode);
parameters.Add("vnp_Amount", (amount * 100).ToString());
parameters.Add("vnp_CreateDate", createDate);
parameters.Add("vnp_CurrCode", "VND");
parameters.Add("vnp_IpAddr", "127.0.0.1");
parameters.Add("vnp_Locale", "vn");
parameters.Add("vnp_OrderInfo", orderInfo);
parameters.Add("vnp_OrderType", "other");
parameters.Add("vnp_ReturnUrl", vnp_Returnurl);
parameters.Add("vnp_TxnRef", orderId.ToString());
parameters.Add("vnp_ExpireDate", expireDate);

// Test ALL possible approaches
var approaches = new Dictionary<string, Func<string, string>>
{
    ["WebUtility.UrlEncode"] = s => WebUtility.UrlEncode(s),
    ["Uri.EscapeDataString"] = s => Uri.EscapeDataString(s),
    ["PhpUrlEncode"] = s => PhpUrlEncode(s),
    ["NoEncode"] = s => s,
};

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

foreach (var approach in approaches)
{
    Console.WriteLine($"\n=== {approach.Key} ===");
    
    var data = new StringBuilder();
    foreach (var kv in parameters.Where(kv => !string.IsNullOrEmpty(kv.Value)))
    {
        data.Append(approach.Value(kv.Key) + "=" + approach.Value(kv.Value) + "&");
    }
    
    var queryString = data.ToString();
    var signData = queryString;
    if (signData.Length > 0)
    {
        signData = signData.Remove(signData.Length - 1, 1);
    }
    
    var hash = HmacSHA512(vnp_HashSecret, signData);
    
    // URL cho trình duyệt (luôn dùng WebUtility encode)
    var urlData = new StringBuilder();
    foreach (var kv in parameters.Where(kv => !string.IsNullOrEmpty(kv.Value)))
    {
        urlData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
    }
    
    var fullUrl = vnp_Url + "?" + urlData + "vnp_SecureHash=" + hash;
    
    // Test URL
    try
    {
        var response = await httpClient.GetAsync(fullUrl);
        var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? "";
        var body = await response.Content.ReadAsStringAsync();
        
        if (body.Contains("Sai chữ ký") || finalUrl.Contains("code=70"))
        {
            Console.WriteLine($"  ❌ Sai chữ ký");
        }
        else if (body.Contains("PaymentMethod") || body.Contains("Chọn phương thức"))
        {
            Console.WriteLine($"  ✅ THÀNH CÔNG!");
            Console.WriteLine($"  URL: {fullUrl}");
        }
        else
        {
            Console.WriteLine($"  Final URL: {finalUrl}");
            Console.WriteLine($"  Body contains 'Error': {body.Contains("Error")}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
    }
    
    Console.WriteLine($"  Hash: {hash.Substring(0, 32)}...");
    Console.WriteLine($"  SignData snippet: ...{signData.Substring(Math.Max(0, signData.IndexOf("vnp_ReturnUrl")), Math.Min(80, signData.Length - signData.IndexOf("vnp_ReturnUrl")))}...");
}

// PHP-compatible urlencode
static string PhpUrlEncode(string s)
{
    // PHP urlencode: space=+, alphanum kept, everything else %XX
    var sb = new StringBuilder();
    foreach (var c in s)
    {
        if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
            c == '-' || c == '_' || c == '.')
        {
            sb.Append(c);
        }
        else if (c == ' ')
        {
            sb.Append('+');
        }
        else
        {
            foreach (var b in Encoding.UTF8.GetBytes(new[] { c }))
            {
                sb.Append('%');
                sb.Append(b.ToString("X2")); // uppercase hex
            }
        }
    }
    return sb.ToString();
}

static string HmacSHA512(string key, string inputData)
{
    var hash = new StringBuilder();
    byte[] keyBytes = Encoding.UTF8.GetBytes(key);
    byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
    using (var hmac = new HMACSHA512(keyBytes))
    {
        byte[] hashValue = hmac.ComputeHash(inputBytes);
        foreach (var theByte in hashValue)
        {
            hash.Append(theByte.ToString("x2")); // lowercase
        }
    }
    return hash.ToString();
}

public class VnPayCompare : IComparer<string>
{
    public int Compare(string x, string y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}
