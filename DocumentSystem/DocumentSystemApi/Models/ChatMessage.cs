using System;
using System.Collections.Generic;

namespace DocumentSystemApi.Models;

public partial class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? SourceReferences { get; set; }

    public string? ModelName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;
}
