using Microsoft.EntityFrameworkCore;

namespace AuthService.Models;

[Keyless]
public class OtpDto
{
    public required string Email { get; set; }
    public required string Otp { get; set; }
}