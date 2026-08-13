namespace GymAssistant.Api.Models;
public class RefreshToken
{
    public Guid Id { get; set; }
    public byte[] TokenHash { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;
}