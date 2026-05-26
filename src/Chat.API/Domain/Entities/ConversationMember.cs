using Chat.API.Domain.Enums;

namespace Chat.API.Domain.Entities;

public class ConversationMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public MemberRole Role { get; set; } = MemberRole.Member;
    public bool IsDeleted { get; set; } = false;  // rời nhóm

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public virtual Conversation? Conversation { get; set; }
}