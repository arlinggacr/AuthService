using System.ComponentModel.DataAnnotations;

public class OtpRecord
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Email { get; set; }

    public required string OtpCode { get; set; }

    [Required]
    public required DateTime ExpiredAt { get; set; }

    public bool IsUsed { get; set; } = false;
}
