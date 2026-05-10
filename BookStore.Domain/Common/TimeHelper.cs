using System;

namespace BookStore.Domain.Common
{
    public static class TimeHelper
    {
        public static DateTime GetVnTime()
        {
            try
            {
                // Cho Windows
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
            }
            catch (TimeZoneNotFoundException)
            {
                // Cho Linux/Mac
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
            }
            catch (Exception)
            {
                // Fallback nếu không tìm thấy múi giờ
                return DateTime.UtcNow.AddHours(7);
            }
        }
    }
}
