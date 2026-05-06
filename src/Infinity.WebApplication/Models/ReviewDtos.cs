namespace Infinity.WebApplication.Models;

public record ReviewResponse(Guid Id, string Author, string Date, string Comment, bool IsOwner);
public record SubmitReviewRequest(Guid AttractionId, string Content);
public record EditReviewRequest(string Content);
