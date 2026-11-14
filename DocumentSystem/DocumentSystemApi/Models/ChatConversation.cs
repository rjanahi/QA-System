using System;
using System.Collections.Generic;

namespace DocumentSystemApi.Models;

public partial class ChatConversation
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsArchived { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual Document Document { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
