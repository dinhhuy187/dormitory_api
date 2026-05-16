namespace Community.API.Domain.Entities;

public class PostComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsHidden { get; set; } = false;
    public int LikeCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Post? Post { get; set; }
}