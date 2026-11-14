
namespace DocumentSystemApi.Dtos
{
    //Represents a generated summary returned to React
    public class SummaryResponseDto
    {
        //Unique identifier of the saved summary
        public Guid Id { get; set; }

        //Analysis that was used to generate this summary
        public Guid AnalysisId { get; set; }

        //General, Technical or Executive
        public string SummaryType { get; set; } = "";

        //Version number for this summary type
        public int Version { get; set; }

        //Title displayed on the summary page
        public string Title { get; set; } = "";

        //Actual AI-generated summary text
        public string Content { get; set; } = "";

        //Pending, Processing, Completed or Failed
        public string Status { get; set; } = "";

        //Date when summary generation started
        public DateTimeOffset CreatedAt { get; set; }

        //Date when generation completed
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
