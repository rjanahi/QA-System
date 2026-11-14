using System;
using System.Collections.Generic;

namespace DocumentSystemApi.Models;

public partial class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = null!;

    public int? PageNumber { get; set; }

    public string? SectionTitle { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual Document Document { get; set; } = null!;
}
