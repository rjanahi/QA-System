
using Microsoft.EntityFrameworkCore;
using DocumentSystemApi.Data;
using DocumentSystemApi.Dtos;
using DocumentSystemApi.Models;
using System.Data;
using System.Text.Json;

namespace DocumentSystemApi.Services
{
    public class AnalysisPersistenceService
    {
        private readonly DocumentDbContext _context;

        //Constructor: receive the database context
        public AnalysisPersistenceService(
            DocumentDbContext context)
        {
            _context = context;
        }


        //Save a completed general analysis and all its findings
        public async Task<DocumentAnalysis> SaveGeneralAnalysisAsync(
            Guid documentId,
            Guid userId,
            GeneralAnalysisResultDto result,
            string modelName)
        {
            //Ensure the AI result exists
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            //Start a database transaction
            //Serializable helps protect version numbering
            //when multiple requests run at the same time
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            //Find the document and confirm ownership
            var Document = await _context.Documents
                .FirstOrDefaultAsync(r =>
                    r.Id == documentId &&
                    r.UserId == userId &&
                    !r.IsArchived);

            //Reject missing or inaccessible documents
            if (Document == null)
                throw new KeyNotFoundException(
                    "Document not found.");

            //Only save analyses for processed documents
            if (Document.Status != "Ready")
                throw new InvalidOperationException(
                    "Document must be processed first.");

            //Get the latest analysis version
            int latestVersion = await _context.DocumentAnalyses
                .Where(a =>
                    a.DocumentId == documentId &&
                    a.AnalysisType == "General")
                .Select(a => (int?)a.Version)
                .MaxAsync() ?? 0;

            //Increase the version number by one
            int newVersion = latestVersion + 1;

            //Get the current UTC date and time
            var now = DateTimeOffset.UtcNow;

            //Combine the overview and overall assessment
            //because our existing table has no Overview column
            string assessment =
                $"Overview:\n{result.Overview}\n\n" +
                $"Overall Assessment:\n{result.OverallAssessment}";

            //Create the parent analysis database record
            var analysis = new DocumentAnalysis
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                AnalysisType = "General",
                Version = newVersion,
                Status = "Completed",
                OverallAssessment = assessment,
                //Store the lists as JSON strings
                KeyRisks = JsonSerializer.Serialize(
                    result.KeyRisks ?? new()),
                Recommendations = JsonSerializer.Serialize(
                    result.Recommendations ?? new()),
                ModelName = modelName,
                ErrorMessage = null,
                CreatedAt = now,
                CompletedAt = now
            };

            //Add the parent record to EF Core
            _context.DocumentAnalyses.Add(analysis);

            //Convert each AI finding into a database record
            foreach (var finding in
                result.Findings ?? new List<GeneralFindingDto>())
            {
                //Create one finding linked to its analysis
                var DocumentFinding = new DocumentFinding
                {
                    Id = Guid.NewGuid(),
                    //Foreign key connecting the finding
                    //to its parent analysis
                    AnalysisId = analysis.Id,
                    Title = finding.Title,
                    Description = finding.Description,
                    Category = finding.Category,
                    Severity = finding.Severity,
                    Impact = finding.Impact,
                    Recommendation = finding.Recommendation,
                    SourceLocation = finding.SourceLocation,
                    AffectedArea = null,
                    CreatedAt = now
                };

                //Add this finding to EF Core
                _context.DocumentFindings.Add(DocumentFinding);
            }

            //Save the analysis and its findings together
            await _context.SaveChangesAsync();

            //Commit the transaction only if saving succeeds
            await transaction.CommitAsync();

            //Return the saved analysis
            return analysis;
        }


        //Save a completed QA analysis and all its findings
        public async Task<DocumentAnalysis> SaveQAAnalysisAsync(
            Guid documentId,
            Guid userId,
            QAAnalysisResultDto result,
            string modelName)
        {
            //Check that the AI returned an analysis
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            //Start a transaction to save all changes together
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            //Find the document and verify its owner
            var Document = await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == documentId &&
                    r.UserId == userId &&
                    !r.IsArchived);

            //Reject documents that do not exist or belong to others
            if (Document == null)
                throw new KeyNotFoundException(
                    "Document not found.");

            //Only save analyses for processed documents
            if (Document.Status != "Ready")
                throw new InvalidOperationException(
                    "Document must be processed first.");

            //Only allow QA analysis for QA-classified documents
            if (Document.DocumentType != "QA")
                throw new InvalidOperationException(
                    "Document must be classified as QA.");

            //Find the latest QA analysis version
            int latestVersion = await _context.DocumentAnalyses
                .Where(a =>
                    a.DocumentId == documentId &&
                    a.AnalysisType == "QA")
                .Select(a => (int?)a.Version)
                .MaxAsync() ?? 0;

            //Increase the version number
            int newVersion = latestVersion + 1;

            //Store the current UTC date and time
            var now = DateTimeOffset.UtcNow;

            //Create the main QA analysis record
            var analysis = new DocumentAnalysis
            {
                //Generate a unique ID for this analysis
                Id = Guid.NewGuid(),

                //Connect the analysis to its document
                DocumentId = documentId,

                //Identify this as a QA analysis
                AnalysisType = "QA",

                //Store the new version number
                Version = newVersion,

                //Mark the analysis as completed
                Status = "Completed",

                //Store test execution statistics
                TotalTests = result.TotalTests,
                PassedTests = result.PassedTests,
                FailedTests = result.FailedTests,
                BlockedTests = result.BlockedTests,
                SkippedTests = result.SkippedTests,

                //Store the overall quality assessment
                OverallAssessment = result.OverallAssessment,

                //Convert lists into JSON strings for SQL Server
                KeyRisks = JsonSerializer.Serialize(
                    result.KeyRisks ?? new()),

                Recommendations = JsonSerializer.Serialize(
                    result.Recommendations ?? new()),

                //Store the model used for analysis
                ModelName = modelName,

                //No error occurred
                ErrorMessage = null,

                //Store creation and completion timestamps
                CreatedAt = now,
                CompletedAt = now
            };

            //Add the analysis record to Entity Framework
            _context.DocumentAnalyses.Add(analysis);

            //Loop through the findings returned by the AI
            foreach (var finding in
                result.Findings ?? new List<QAFindingDto>())
            {
                //Create a database record for each finding
                var DocumentFinding = new DocumentFinding
                {
                    //Generate a unique finding ID
                    Id = Guid.NewGuid(),

                    //Connect this finding to the parent analysis
                    AnalysisId = analysis.Id,

                    //Store the issue details
                    Title = finding.Title,
                    Description = finding.Description,

                    //Store the issue classification and severity
                    Category = finding.Category,
                    Severity = finding.Severity,

                    //Store which software feature is affected
                    AffectedArea = finding.AffectedArea,

                    //Store the impact and recommended action
                    Impact = finding.Impact,
                    Recommendation = finding.Recommendation,

                    //Store where the issue was found
                    SourceLocation = finding.SourceLocation,

                    //Record when the finding was created
                    CreatedAt = now
                };

                //Add the finding to Entity Framework
                _context.DocumentFindings.Add(DocumentFinding);
            }

            //Save the analysis and findings to SQL Server
            await _context.SaveChangesAsync();

            //Commit only after all records are saved successfully
            await transaction.CommitAsync();

            //Return the saved analysis
            return analysis;
        }

    }
}
