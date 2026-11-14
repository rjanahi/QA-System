using System;
using System.Collections.Generic;

namespace DocumentSystemApi.Models;

public partial class DocumentFinding
{
    public Guid Id { get; set; }

    public Guid AnalysisId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Category { get; set; }

    public string? Severity { get; set; }

    public string? AffectedArea { get; set; }

    public string? Impact { get; set; }

    public string? Recommendation { get; set; }

    public string? SourceLocation { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual DocumentAnalysis Analysis { get; set; } = null!;
}
