using BookStore.Application.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace BookStore.Application.Services
{
    public class ZaloPayService
    {
        private readonly ZaloPayConfig _config;
        private readonly HttpClient _httpClient;

        public ZaloPayService(IOptions<ZaloPayConfig> config, HttpClient httpClient)
        {
            _config = config.Value;
            _httpClient = httpClient;
        }

        public async Task<string?> CreateOrderAsync(int orderId, decimal amount, string orderNumber)
        {
            var app_trans_id = $"{DateTime.Now:yyMMdd}_{orderId}_{DateTime.Now.Millisecond}"; 
            var embed_data = "{}";
            var item = "[]";
            var app_time = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            
            var param = new Dictionary<string, string>
            {
                { "app_id", _config.AppId },
                { "app_user", "user123" },
                { "app_time", app_time },
                { "amount", ((long)amount).ToString() },
                { "app_trans_id", app_trans_id },
                { "embed_data", embed_data },
                { "item", item },
                { "description", $"Thanh toan don hang {orderNumber}" },
                { "bank_code", "" },
                { "callback_url", _config.CallbackUrl }
            };

            var data = $"{_config.AppId}|{app_trans_id}|user123|{param["amount"]}|{app_time}|{embed_data}|{item}";
            param.Add("mac", ComputeHmacSha256(data, _config.Key1));

            var content = new FormUrlEncodedContent(param);
            var response = await _httpClient.PostAsync(_config.Endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                if (result?.return_code == 1)
                {
                    return result.order_url;
                }
                else
                {
                    throw new Exception($"ZaloPay Error ({result?.return_code}): {result?.sub_return_message ?? result?.return_message}. MAC Data: [{data}] Response: {responseContent}");
                }
            }
            else
            {
                throw new Exception($"ZaloPay HTTP Error: {response.StatusCode} - {responseContent}");
            }
        }

        public bool ValidateCallback(string data, string requestMac)
        {
            var mac = ComputeHmacSha256(data, _config.Key2);
            return mac.Equals(requestMac, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeHmacSha256(string data, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
