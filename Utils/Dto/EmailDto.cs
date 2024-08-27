using Microsoft.EntityFrameworkCore;

namespace AuthService.Models;

[Keyless]
public class EmailDto
{
    public required string Email { get; set; }
}
