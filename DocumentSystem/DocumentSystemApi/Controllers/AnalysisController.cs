
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocumentSystemApi.Data;
using DocumentSystemApi.Services;
using System.Security.Claims;
using System.Text.Json;

namespace DocumentSystemApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly GeneralAnalysisService _analysisService;
        private readonly AnalysisPersistenceService _persistenceService;
        private readonly DocumentDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly QAAnalysisService _qaAnalysisService;

        //Constructor: receive required services
        public AnalysisController(
      GeneralAnalysisService analysisService,
      QAAnalysisService qaAnalysisService,
      AnalysisPersistenceService persistenceService,
      DocumentDbContext context,
      IConfiguration configuration)
        {
            //Service for analyzing general documents
            _analysisService = analysisService;

            //Service for analyzing QA Documents
            _qaAnalysisService = qaAnalysisService;

            //Service for saving generated results
            _persistenceService = persistenceService;

            //Database connection
            _context = context;

            //Application settings, including AI model name
            _configuration = configuration;
        }


        //Get the ID of the authenticated user
        private Guid? GetUserId()
        {
            //Read user ID from authentication claims
            string? userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            //Convert the claim to a Guid
            if (Guid.TryParse(userIdValue, out Guid userId))
                return userId;

            //Return null if there is no valid ID
            return null;
        }


        //POST /api/Analysis/{documentId}/general
        //Generate and save a general document analysis
        [HttpPost("{documentId:guid}/general")]
        public async Task<IActionResult> GenerateGeneralAnalysis(
            Guid documentId)
        {
            //Identify the logged-in user
            Guid? userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            try
            {
                //Generate the AI analysis
                var result = await _analysisService.AnalyzeAsync(
                    documentId,
                    userId.Value);

                //Get the configured AI model name
                string modelName =
                    _configuration["OpenAI:Model"]
                    ?? "Not specified";

                //Save the analysis and findings in SQL Server
                var savedAnalysis =
                    await _persistenceService.SaveGeneralAnalysisAsync(
                        documentId,
                        userId.Value,
                        result,
                        modelName);

                //Return information about the saved analysis
                return Ok(new
                {
                    message = "Analysis completed successfully.",
                    analysisId = savedAnalysis.Id,
                    documentId = savedAnalysis.DocumentId,
                    analysisType = savedAnalysis.AnalysisType,
                    version = savedAnalysis.Version,
                    status = savedAnalysis.Status,
                    findingsCount = result.Findings?.Count ?? 0
                });
            }
            catch (KeyNotFoundException)
            {
                //Document not found or belongs to another user
                return NotFound(new
                {
                    message = "Document not found."
                });
            }
            catch (InvalidOperationException)
            {
                //Document not ready or AI result invalid
                return Conflict(new
                {
                    message = "Analysis could not be completed.",
                    detail = "Check the document processing status."
                });
            }
            catch (HttpRequestException)
            {
                //External AI request failed
                return StatusCode(502, new
                {
                    message = "AI analysis service is unavailable."
                });
            }
            catch (JsonException)
            {
                //AI returned an unexpected JSON response
                return StatusCode(502, new
                {
                    message = "AI returned an invalid analysis response."
                });
            }
        }


        //GET /api/Analysis/{documentId}
        //Get the analysis history for a document
        [HttpGet("{documentId:guid}")]
        public async Task<IActionResult> GetAnalysisHistory(
            Guid documentId)
        {
            //Identify the logged-in user
            Guid? userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            //Check whether the document belongs to the user
            bool documentExists = await _context.Documents
                .AnyAsync(r =>
                    r.Id == documentId &&
                    r.UserId == userId.Value &&
                    !r.IsArchived);

            //Do not expose documents owned by other users
            if (!documentExists)
                return NotFound(new
                {
                    message = "Document not found."
                });

            //Retrieve saved analysis records for this document
            var analyses = await _context.DocumentAnalyses
                .Where(a => a.DocumentId == documentId)

                //Newest analyses first
                .OrderByDescending(a => a.CreatedAt)

                //Return only fields needed for history
                .Select(a => new
                {
                    analysisId = a.Id,
                    analysisType = a.AnalysisType,
                    version = a.Version,
                    status = a.Status,
                    modelName = a.ModelName,
                    createdAt = a.CreatedAt,
                    completedAt = a.CompletedAt
                })
                .ToListAsync();

            return Ok(analyses);
        }


        //GET /api/Analysis/result/{analysisId}
        //Get one analysis together with its findings
        [HttpGet("result/{analysisId:guid}")]
        public async Task<IActionResult> GetAnalysisResult(
            Guid analysisId)
        {
            //Identify the logged-in user
            Guid? userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            //Find the analysis and verify ownership
            //through the Documents table
            var analysis = await _context.DocumentAnalyses
                .Where(a =>
                    a.Id == analysisId &&
                    _context.Documents.Any(r =>
                        r.Id == a.DocumentId &&
                        r.UserId == userId.Value &&
                        !r.IsArchived))
                .FirstOrDefaultAsync();

            if (analysis == null)
                return NotFound(new
                {
                    message = "Analysis not found."
                });

            //Retrieve findings belonging to this analysis
            var findings = await _context.DocumentFindings
                .Where(f => f.AnalysisId == analysisId)
                .OrderBy(f => f.CreatedAt)
                .Select(f => new
                {
                    f.Id,
                    f.Title,
                    f.Description,
                    f.Category,
                    f.Severity,
                    f.AffectedArea,
                    f.Impact,
                    f.Recommendation,
                    f.SourceLocation
                })
                .ToListAsync();

            //Return analysis information and its findings
            return Ok(new
            {
                analysisId = analysis.Id,
                documentId = analysis.DocumentId,
                analysisType = analysis.AnalysisType,
                version = analysis.Version,
                status = analysis.Status,
                overallAssessment = analysis.OverallAssessment,

                //Return test statistics for QA analyses
                //These values will be null for general analyses
                totalTests = analysis.TotalTests,
                passedTests = analysis.PassedTests,
                failedTests = analysis.FailedTests,
                blockedTests = analysis.BlockedTests,
                skippedTests = analysis.SkippedTests,

                //Convert stored JSON lists back into arrays
                //Assessment and recommendations
                keyRisks = DeserializeList(analysis.KeyRisks),
                recommendations = DeserializeList(analysis.Recommendations),

                //Additional analysis information
                modelName = analysis.ModelName,
                createdAt = analysis.CreatedAt,
                completedAt = analysis.CompletedAt,
                findings
            });
        }


        //Convert a JSON string into a C# string list
        private static List<string> DeserializeList(
            string? json)
        {
            //Return an empty list if the column is empty
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                //Convert stored JSON into a list
                return JsonSerializer.Deserialize<List<string>>(json)
                    ?? new List<string>();
            }
            catch (JsonException)
            {
                //Handle malformed JSON without failing the request
                return new List<string>();
            }
        }


        //POST /api/Analysis/{documentId}/qa
        //Generate and save a specialized QA Document analysis
        [HttpPost("{documentId:guid}/qa")]
        public async Task<IActionResult> GenerateQAAnalysis(
            Guid documentId)
        {
            //Get the authenticated user's ID
            Guid? userId = GetUserId();

            //Reject requests without a valid user ID
            if (userId == null)
                return Unauthorized();

            try
            {
                //Generate the QA analysis using OpenAI
                var result = await _qaAnalysisService.AnalyzeAsync(
                    documentId,
                    userId.Value);

                //Get the model name from configuration
                string modelName =
                    _configuration["OpenAI:Model"]
                    ?? "Not specified";

                //Save the analysis and its findings to SQL Server
                var savedAnalysis =
                    await _persistenceService.SaveQAAnalysisAsync(
                        documentId,
                        userId.Value,
                        result,
                        modelName);

                //Return information about the completed analysis
                return Ok(new
                {
                    message = "QA analysis completed successfully.",

                    //Unique identifier of the saved analysis
                    analysisId = savedAnalysis.Id,

                    //Document associated with this analysis
                    documentId = savedAnalysis.DocumentId,

                    //Identify the analysis type
                    analysisType = savedAnalysis.AnalysisType,

                    //Version of this analysis
                    version = savedAnalysis.Version,

                    //Current analysis status
                    status = savedAnalysis.Status,

                    //Number of findings generated
                    findingsCount = result.Findings?.Count ?? 0,

                    //Test execution statistics
                    totalTests = result.TotalTests,
                    passedTests = result.PassedTests,
                    failedTests = result.FailedTests,
                    blockedTests = result.BlockedTests,
                    skippedTests = result.SkippedTests
                });
            }
            catch (KeyNotFoundException)
            {
                //Return 404 for documents that don't exist
                //or belong to another user
                return NotFound(new
                {
                    message = "Document not found."
                });
            }
            catch (InvalidOperationException)
            {
                //Document may not be processed or classified as QA
                return Conflict(new
                {
                    message = "QA analysis could not be completed.",
                    detail = "Verify that the document is Ready "
                           + "and classified as QA."
                });
            }
            catch (HttpRequestException)
            {
                //Handle problems communicating with the AI API
                return StatusCode(502, new
                {
                    message = "AI analysis service is unavailable."
                });
            }
            catch (System.Text.Json.JsonException)
            {
                //Handle malformed AI responses
                return StatusCode(502, new
                {
                    message = "AI returned an invalid response."
                });
            }
        }


    }
}
