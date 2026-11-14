
namespace DocumentSystemApi.Dtos
{
    //Represents the complete result of a general AI analysis
    public class GeneralAnalysisResultDto
    {
        //Short explanation of what the document is about
        public string Overview { get; set; } = "";

        //Overall evaluation of the document
        public string OverallAssessment { get; set; } = "";

        //Important concerns identified in the document
        public List<string> KeyRisks { get; set; } = new();

        //Suggested actions based on the document
        public List<string> Recommendations { get; set; } = new();

        //Individual findings discovered during analysis
        public List<GeneralFindingDto> Findings { get; set; } = new();
    }


    //Represents one finding identified by the AI
    public class GeneralFindingDto
    {
        //Short name describing the finding
        public string Title { get; set; } = "";

        //Detailed explanation of the finding
        public string Description { get; set; } = "";

        //Type of finding, such as Risk or Observation
        public string Category { get; set; } = "";

        //Importance level: Low, Medium, High or Critical
        public string Severity { get; set; } = "";

        //Possible effect of the finding
        public string Impact { get; set; } = "";

        //Suggested action to address the finding
        public string Recommendation { get; set; } = "";

        //Location of supporting text, when available
        public string SourceLocation { get; set; } = "";
    }
}
