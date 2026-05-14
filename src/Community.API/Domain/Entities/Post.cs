using Community.API.Domain.Enums;

namespace Community.API.Domain.Entities;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AuthorId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> MediaUrls { get; set; } = [];
    public PostType PostType { get; set; } = PostType.General;
    public bool IsHidden { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}