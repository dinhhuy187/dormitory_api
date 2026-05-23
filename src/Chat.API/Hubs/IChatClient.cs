namespace Chat.API.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(MessagePayload message);
    Task MessageDeleted(MessageDeletedPayload payload);
    Task UserTyping(TypingPayload payload);
    Task UserStopTyping(TypingPayload payload);
}

public record MessagePayload(
    Guid Id,
    Guid ConversationId,
    string SenderId,
    string SenderName,
    string? SenderAvatar,
    string Content,
    List<string> MediaUrls,
    DateTime CreatedAt
);

public record MessageDeletedPayload(
    Guid MessageId,
    Guid ConversationId
);

public record TypingPayload(
    Guid ConversationId,
    string UserId
);