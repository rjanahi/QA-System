using System;
using System.Collections.Generic;

namespace DocumentSystemApi.Models;

public partial class Document
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public string StoredFileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public string Status { get; set; } = null!;

    public DateTimeOffset UploadedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsArchived { get; set; }

    public string DocumentType { get; set; } = null!;

    public virtual ICollection<ChatConversation> ChatConversations { get; set; } = new List<ChatConversation>();

    public virtual ICollection<DocumentAnalysis> DocumentAnalyses { get; set; } = new List<DocumentAnalysis>();

    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

    public virtual ICollection<ProcessingJob> ProcessingJobs { get; set; } = new List<ProcessingJob>();
}
