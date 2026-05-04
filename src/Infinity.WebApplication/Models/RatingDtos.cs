namespace Infinity.WebApplication.Models;

public record AttractionRatingResponse(int? Value, double? Average, int Count);
public record RateRequest(Guid AttractionId, int Value);
public record RateResponse(int Value, double NewAverage, int NewCount);
