namespace CalisApi.Dtos;

/// <summary>Página acotada de resultados, con metadatos para navegación.</summary>
public record PageDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public record MediaDto(Guid Id, string FileName, string ContentType, long Size);
public record PostDto(int Id, string Title, string Content, string Kind, DateTime CreatedAt,
    DateTime? UpdatedAt, string AuthorName, DateTime? StartsAt, string? Location,
    int LikeCount, bool IsLiked, IReadOnlyList<MediaDto> Media);
public record WritePostRequest(string Title, string Content, string Kind,
    DateTime? StartsAt, string? Location, Guid[] MediaIds);
/// <summary>Purpose: "community" (tablón, default) o "exercise" (video de ejercicio, ≤30s).</summary>
public record BeginUploadRequest(string FileName, string ContentType, long Size, string Purpose = "community");
public record UploadDto(Guid Id, string UploadUrl, DateTime ExpiresAt);
public record MediaLinkDto(string Url, DateTime ExpiresAt);
public record ReviewDto(int Id, int UserId, string AuthorName, int? SessionId, string SessionTitle,
    DateTime SessionDate, int Rating, string? Content, DateTime CreatedAt, bool IsHidden);
public record WriteReviewRequest(int Rating, string? Content);
public record ReviewPageDto(PageDto<ReviewDto> Reviews, double? AverageRating, int RatingCount);
public record ReviewEligibilityDto(bool CanReview, string? Reason, ReviewDto? MyReview);
public record ReviewVisibilityRequest(bool IsHidden);
public record EventDto(int Id, string Title, string Description, string Location, DateTime StartsAt,
    DateTime EndsAt, int? Capacity, int RegisteredCount, bool IsRegistered, bool IsCancelled);
public record WriteEventRequest(string Title, string Description, string Location,
    DateTime StartsAt, DateTime EndsAt, int? Capacity);
public record EventParticipantDto(int UserId, string FullName, DateTime RegisteredAt);
