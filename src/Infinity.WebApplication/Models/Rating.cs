namespace Infinity.WebApplication.Models;

public class Rating
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AttractionId { get; set; }
    public int Value { get; set; }
    public User User { get; set; } = null!;
}
