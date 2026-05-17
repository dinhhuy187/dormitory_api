namespace Community.API.Domain.Entities;

public class CommentLike
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CommentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual PostComment? Comment { get; set; }
}