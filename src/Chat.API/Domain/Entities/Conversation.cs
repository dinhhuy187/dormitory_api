using Chat.API.Domain.Enums;

namespace Chat.API.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ConversationType Type { get; set; }
    public string? Name { get; set; }           // chỉ dùng cho nhóm
    public string? AvatarUrl { get; set; }      // chỉ dùng cho nhóm
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ConversationMember> Members { get; set; } = [];
    public virtual ICollection<Message> Messages { get; set; } = [];
}