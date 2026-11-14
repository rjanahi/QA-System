
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocumentSystemApi.Data;
using DocumentSystemApi.Dtos;
using DocumentSystemApi.Services;
using System.Security.Claims;

namespace DocumentSystemApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SummariesController : ControllerBase
    {
        private readonly SummaryGenerationService _generationService;
        private readonly SummaryPersistenceService _persistenceService;
        private readonly DocumentDbContext _context;
        private readonly ILogger<SummariesController> _logger;

        //Constructor: receive the services needed by this controller
        public SummariesController(
            SummaryGenerationService generationService,
            SummaryPersistenceService persistenceService,
            DocumentDbContext context,
            ILogger<SummariesController> logger)
        {
            _generationService = generationService;
            _persistenceService = persistenceService;
            _context = context;
            _logger = logger;
        }


        //Get the currently authenticated user's ID
        private Guid? GetUserId()
        {
            //Read the user ID stored in the authentication cookie
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            //Convert the value into a GUID
            if (Guid.TryParse(value, out Guid userId))
                return userId;

            //Return null when the user ID is invalid
            return null;
        }


        //POST /api/Summaries/generate
        //Generate and save a new summary
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateSummary(
            [FromBody] GenerateSummaryDto dto)
        {
            //Identify the authenticated user
            Guid? userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            //Reject an empty analysis ID
            if (dto.AnalysisId == Guid.Empty)
            {
                return BadRequest(new
                {
                    message = "A valid analysis ID is required."
                });
            }

            try
            {
                //Generate the summary from the saved analysis
                var generated =
                    await _generationService.GenerateAsync(
                        dto.AnalysisId,
                        userId.Value,
                        dto.SummaryType);

                //Save the summary and assign its version number
                var saved =
                    await _persistenceService.SaveSummaryAsync(
                        dto.AnalysisId,
                        userId.Value,
                        dto.SummaryType,
                        generated);

                //Return the newly saved summary
                return CreatedAtAction(
                    nameof(GetSummary),
                    new { summaryId = saved.Id },
                    saved);
            }
            catch (KeyNotFoundException)
            {
                //The analysis does not exist or belongs to another user
                return NotFound(new
                {
                    message = "Analysis not found."
                });
            }
            catch (ArgumentException ex)
            {
                //Handle invalid request values
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException)
            {
                //The analysis is not completed or its type is invalid
                return Conflict(new
                {
                    message =
                        "The summary could not be generated. " +
                        "Check the analysis status and summary type."
                });
            }
            catch (HttpRequestException ex)
            {
                //Record the external service failure
                _logger.LogError(
                    ex, "AI summary request failed.");

                return StatusCode(502, new
                {
                    message = "AI summary service is unavailable."
                });
            }
            catch (System.Text.Json.JsonException ex)
            {
                //Handle unexpected AI response formats
                _logger.LogError(
                    ex, "AI returned invalid summary data.");

                return StatusCode(502, new
                {
                    message = "AI returned an invalid summary."
                });
            }
        }


        //GET /api/Summaries/analysis/{analysisId}
        //Get all summaries created for an analysis
        [HttpGet("analysis/{analysisId:guid}")]
        public async Task<IActionResult> GetSummaryHistory(
            Guid analysisId)
        {
            //Identify the logged-in user
            Guid? userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            //Check ownership through Analysis -> Document -> User
            bool analysisExists = await _context.DocumentAnalyses
                .AsNoTracking()
                .AnyAsync(a =>
                    a.Id == analysisId &&
                    _context.Documents.Any(r =>
                        r.Id == a.DocumentId &&
                        r.UserId == userId.Value &&
                        !r.IsArchived));

            //Hide analyses belonging to other users
            if (!analysisExists)
            {
                return NotFound(new
                {
                    message = "Analysis not found."
                });
            }

            //Retrieve summaries belonging to this analysis
            var summaries = await _context.GeneratedSummaries
                .AsNoTracking()
                .Where(s => s.AnalysisId == analysisId)

                //Show the newest summaries first
                .OrderByDescending(s => s.CreatedAt)

                //Return the required summary information
                .Select(s => new SummaryResponseDto
                {
                    Id = s.Id,
                    AnalysisId = s.AnalysisId,
                    SummaryType = s.SummaryType,
                    Version = s.Version,
                    Title = s.Title,
                    Content = s.Content,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    CompletedAt = s.CompletedAt
                })
                .ToListAsync();

            return Ok(summaries);
        }


        //GET /api/Summaries/{summaryId}
        //Retrieve one saved summary
        [HttpGet("{summaryId:guid}")]
        public async Task<IActionResult> GetSummary(Guid summaryId)
        {
            //Identify the authenticated user
            Guid? userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            //Find the summary and verify ownership through
            //its parent analysis and document
            var summary = await _context.GeneratedSummaries
                .AsNoTracking()
                .Where(s =>
                    s.Id == summaryId &&
                    _context.DocumentAnalyses.Any(a =>
                        a.Id == s.AnalysisId &&
                        _context.Documents.Any(r =>
                            r.Id == a.DocumentId &&
                            r.UserId == userId.Value &&
                            !r.IsArchived)))

                //Convert the database entity to a safe DTO
                .Select(s => new SummaryResponseDto
                {
                    Id = s.Id,
                    AnalysisId = s.AnalysisId,
                    SummaryType = s.SummaryType,
                    Version = s.Version,
                    Title = s.Title,
                    Content = s.Content,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    CompletedAt = s.CompletedAt
                })
                .FirstOrDefaultAsync();

            //Return 404 if missing or inaccessible
            if (summary == null)
            {
                return NotFound(new
                {
                    message = "Summary not found."
                });
            }

            //Return the saved summary to React
            return Ok(summary);
        }
    }
}
