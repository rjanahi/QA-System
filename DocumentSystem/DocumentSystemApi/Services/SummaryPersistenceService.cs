
using Microsoft.EntityFrameworkCore;
using DocumentSystemApi.Data;
using DocumentSystemApi.Dtos;
using DocumentSystemApi.Models;
using System.Data;

namespace DocumentSystemApi.Services
{
    public class SummaryPersistenceService
    {
        private readonly DocumentDbContext _context;

        //Constructor: receive the database context
        public SummaryPersistenceService(
            DocumentDbContext context)
        {
            _context = context;
        }


        //Save an AI-generated summary with version history
        public async Task<SummaryResponseDto> SaveSummaryAsync(
            Guid analysisId,
            Guid userId,
            string summaryType,
            GeneratedSummaryResultDto result)
        {
            //Check whether the AI returned a result
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            //Reject empty identifiers
            if (analysisId == Guid.Empty)
                throw new ArgumentException(
                    "A valid analysis ID is required.");

            //Only accept the three supported summary types
            if (summaryType is not
                ("General" or "Technical" or "Executive"))
            {
                throw new ArgumentException(
                    "Invalid summary type.");
            }

            //Check that the AI returned meaningful content
            if (string.IsNullOrWhiteSpace(result.Title) ||
                string.IsNullOrWhiteSpace(result.Content))
            {
                throw new ArgumentException(
                    "Generated summary is empty.");
            }

            //Start a transaction to keep saving consistent
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            //Find the analysis and verify document ownership
            var analysis = await _context.DocumentAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == analysisId &&
                    _context.Documents.Any(r =>
                        r.Id == a.DocumentId &&
                        r.UserId == userId &&
                        !r.IsArchived));

            //Reject analyses that do not belong to the user
            if (analysis == null)
                throw new KeyNotFoundException(
                    "Analysis not found.");

            //Only save summaries of completed analyses
            if (analysis.Status != "Completed")
                throw new InvalidOperationException(
                    "Analysis must be completed first.");

            //Technical and Executive summaries require QA analysis
            if (summaryType != "General" &&
                analysis.AnalysisType != "QA")
            {
                throw new InvalidOperationException(
                    "This summary type requires QA analysis.");
            }

            //Find the latest version for this analysis and type
            int latestVersion =
                await _context.GeneratedSummaries
                    .Where(s =>
                        s.AnalysisId == analysisId &&
                        s.SummaryType == summaryType)
                    .Select(s => (int?)s.Version)
                    .MaxAsync() ?? 0;

            //Create the next version number
            int newVersion = latestVersion + 1;

            //Record the current UTC time
            var now = DateTimeOffset.UtcNow;

            //Create the summary database entity
            var summary = new GeneratedSummary
            {
                //Generate a unique database identifier
                Id = Guid.NewGuid(),

                //Connect the summary to its parent analysis
                AnalysisId = analysisId,

                //Store the requested summary type and version
                SummaryType = summaryType,
                Version = newVersion,

                //Store the AI-generated title and content
                Title = result.Title.Trim(),
                Content = result.Content.Trim(),

                //The AI has already finished generating
                Status = "Completed",

                //No downloadable file has been generated yet
                FileName = null,
                FilePath = null,

                //No error occurred
                ErrorMessage = null,

                //Save creation and completion timestamps
                CreatedAt = now,
                CompletedAt = now
            };

            //Tell Entity Framework to insert this record
            _context.GeneratedSummaries.Add(summary);

            //Save the new summary in SQL Server
            await _context.SaveChangesAsync();

            //Commit the transaction after successful saving
            await transaction.CommitAsync();

            //Return only the information needed by React
            return new SummaryResponseDto
            {
                Id = summary.Id,
                AnalysisId = summary.AnalysisId,
                SummaryType = summary.SummaryType,
                Version = summary.Version,
                Title = summary.Title,
                Content = summary.Content,
                Status = summary.Status,
                CreatedAt = summary.CreatedAt,
                CompletedAt = summary.CompletedAt
            };
        }
    }
}
