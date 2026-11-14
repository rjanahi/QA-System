using System;
using System.Collections.Generic;
using DocumentSystemApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentSystemApi.Data;

public partial class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatConversation> ChatConversations { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentAnalysis> DocumentAnalyses { get; set; }

    public virtual DbSet<DocumentChunk> DocumentChunks { get; set; }

    public virtual DbSet<DocumentFinding> DocumentFindings { get; set; }

    public virtual DbSet<GeneratedSummary> GeneratedSummaries { get; set; }

    public virtual DbSet<ProcessingJob> ProcessingJobs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasIndex(e => e.DocumentId, "IX_ChatConversations_DocumentId");

            entity.HasIndex(e => e.UserId, "IX_ChatConversations_UserId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_ChatConversations_Id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_ChatConversations_CreatedAt");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Document).WithMany(p => p.ChatConversations)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatConversations_Documents");

            entity.HasOne(d => d.User).WithMany(p => p.ChatConversations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatConversations_Users");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt }, "IX_ChatMessages_ConversationId_CreatedAt");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_ChatMessages_Id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_ChatMessages_CreatedAt");
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.Role).HasMaxLength(20);

            entity.HasOne(d => d.Conversation).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatMessages_ChatConversations");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Documents_UserId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Documents_Id");
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.DocumentType)
                .HasMaxLength(20)
                .HasDefaultValue("Unknown", "DF_Documents_DocumentType");
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Uploaded", "DF_Documents_Status");
            entity.Property(e => e.StoredFileName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Documents_UploadedAt");
        });

        modelBuilder.Entity<DocumentAnalysis>(entity =>
        {
            entity.HasIndex(e => e.DocumentId, "IX_DocumentAnalyses_DocumentId");

            entity.HasIndex(e => new { e.DocumentId, e.AnalysisType, e.Version }, "UQ_DocumentAnalyses_Document_Type_Version").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_DocumentAnalyses_Id");
            entity.Property(e => e.AnalysisType)
                .HasMaxLength(20)
                .HasDefaultValue("General", "DF_DocumentAnalyses_AnalysisType");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_DocumentAnalyses_CreatedAt");
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending", "DF_DocumentAnalyses_Status");
            entity.Property(e => e.Version).HasDefaultValue(1, "DF_DocumentAnalyses_Version");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentAnalyses)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentAnalyses_Documents");
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex }, "UQ_DocumentChunks_Document_Index").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_DocumentChunks_Id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_DocumentChunks_CreatedAt");
            entity.Property(e => e.SectionTitle).HasMaxLength(255);

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentChunks)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentChunks_Documents");
        });

        modelBuilder.Entity<DocumentFinding>(entity =>
        {
            entity.HasIndex(e => e.AnalysisId, "IX_DocumentFindings_AnalysisId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_DocumentFindings_Id");
            entity.Property(e => e.AffectedArea).HasMaxLength(255);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_DocumentFindings_CreatedAt");
            entity.Property(e => e.Severity).HasMaxLength(20);
            entity.Property(e => e.SourceLocation).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(250);

            entity.HasOne(d => d.Analysis).WithMany(p => p.DocumentFindings)
                .HasForeignKey(d => d.AnalysisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentFindings_DocumentAnalyses");
        });

        modelBuilder.Entity<GeneratedSummary>(entity =>
        {
            entity.HasIndex(e => e.AnalysisId, "IX_GeneratedSummaries_AnalysisId");

            entity.HasIndex(e => new { e.AnalysisId, e.SummaryType, e.Version }, "UQ_GeneratedSummaries_Analysis_Type_Version").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_GeneratedSummaries_Id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_GeneratedSummaries_CreatedAt");
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending", "DF_GeneratedSummaries_Status");
            entity.Property(e => e.SummaryType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.Version).HasDefaultValue(1, "DF_GeneratedSummaries_Version");

            entity.HasOne(d => d.Analysis).WithMany(p => p.GeneratedSummaries)
                .HasForeignKey(d => d.AnalysisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GeneratedSummaries_DocumentAnalyses");
        });

        modelBuilder.Entity<ProcessingJob>(entity =>
        {
            entity.HasIndex(e => e.DocumentId, "IX_ProcessingJobs_DocumentId");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_ProcessingJobs_Status_CreatedAt");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_ProcessingJobs_Id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_ProcessingJobs_CreatedAt");
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.JobType).HasMaxLength(30);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending", "DF_ProcessingJobs_Status");

            entity.HasOne(d => d.Document).WithMany(p => p.ProcessingJobs)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcessingJobs_Documents");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Users_Id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Users_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Users_IsActive");
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
