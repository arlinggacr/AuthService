using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Username { get; set; }

    [MaxLength(100)]
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsVerified { get; set; } = false;
}
