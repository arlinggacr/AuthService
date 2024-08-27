using System;
using System.Collections.Concurrent;

namespace AuthService.Services
{
    public class OtpService
    {
        private readonly ConcurrentDictionary<string, string> _otpStorage =
            new ConcurrentDictionary<string, string>();
        private readonly Random _random = new Random();

        public string GenerateOtp(string key)
        {
            try
            {
                // Generate a 6-digit OTP
                var otp = _random.Next(100000, 999999).ToString("D6");
                _otpStorage[key] = otp;
                return otp;
            }
            catch (Exception ex)
            {
                // Log or handle the error appropriately
                Console.WriteLine($"Error generating OTP: {ex.Message}");
                throw;
            }
        }

        public bool VerifyOtp(string key, string otp)
        {
            if (_otpStorage.TryGetValue(key, out var storedOtp))
            {
                // Check if the provided OTP matches the stored OTP
                return storedOtp == otp;
            }

            return false;
        }
    }
}
