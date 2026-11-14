using System;
using System.Collections.Generic;

namespace DocumentSystemApi.Models;

public partial class GeneratedSummary
{
    public Guid Id { get; set; }

    public Guid AnalysisId { get; set; }

    public string SummaryType { get; set; } = null!;

    public int Version { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public string Status { get; set; } = null!;

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public virtual DocumentAnalysis Analysis { get; set; } = null!;
}
