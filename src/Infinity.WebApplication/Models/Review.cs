namespace Infinity.WebApplication.Models;

public class Review
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AttractionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
