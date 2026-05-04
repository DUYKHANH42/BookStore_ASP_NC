using Microsoft.AspNetCore.Http;
using System;

namespace BookStore.Application.VnpayProvider.Extensions
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Lấy địa chỉ IP từ HttpContext của API Controller.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static string GetIpAddress(this HttpContext context)
        {
            var remoteIpAddress = context.Connection.RemoteIpAddress;
            if (remoteIpAddress == null) return "127.0.0.1";

            if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                remoteIpAddress = remoteIpAddress.MapToIPv4();
            }

            var ip = remoteIpAddress.ToString();
            return ip == "0.0.0.1" || ip == "::1" ? "127.0.0.1" : ip;
        }
    }
}
