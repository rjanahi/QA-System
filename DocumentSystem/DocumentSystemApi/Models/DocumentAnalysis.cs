using System;
using System.Collections.Generic;

namespace DocumentSystemApi.Models;

public partial class DocumentAnalysis
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int Version { get; set; }

    public string Status { get; set; } = null!;

    public int? TotalTests { get; set; }

    public int? PassedTests { get; set; }

    public int? FailedTests { get; set; }

    public int? BlockedTests { get; set; }

    public int? SkippedTests { get; set; }

    public string? OverallAssessment { get; set; }

    public string? KeyRisks { get; set; }

    public string? Recommendations { get; set; }

    public string? ModelName { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string AnalysisType { get; set; } = null!;

    public virtual Document Document { get; set; } = null!;

    public virtual ICollection<DocumentFinding> DocumentFindings { get; set; } = new List<DocumentFinding>();

    public virtual ICollection<GeneratedSummary> GeneratedSummaries { get; set; } = new List<GeneratedSummary>();
}
