
using System.ComponentModel.DataAnnotations;

namespace DocumentSystemApi.Dtos
{
    //Represents a request to generate a new summary
    public class GenerateSummaryDto
    {
        //ID of the analysis to summarize
        [Required]
        public Guid AnalysisId { get; set; }

        //Type of summary requested by the user
        //Supported values: General, Technical, Executive
        [Required]
        [RegularExpression(
            "^(General|Technical|Executive)$",
            ErrorMessage = "Invalid summary type.")]
        public string SummaryType { get; set; } = "";
    }
}
