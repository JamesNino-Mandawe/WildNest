using System;
using System.Security.Cryptography;

namespace Project.Booking
{
    internal static class BookingIdGenerator
    {
        public static string NewId()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            int randomPart = RandomNumberGenerator.GetInt32(1000, 10000);
            return $"WN-{timestamp}-{randomPart}";
        }
    }
}
