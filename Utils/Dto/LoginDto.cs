using Microsoft.EntityFrameworkCore;

namespace AuthService.Models;

[Keyless]
public class LoginDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
