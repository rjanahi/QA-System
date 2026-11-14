using System;
using System.Collections.Generic;

namespace DocumentSystemApi.Models;

public partial class ProcessingJob
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public string JobType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int Progress { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public virtual Document Document { get; set; } = null!;
}
