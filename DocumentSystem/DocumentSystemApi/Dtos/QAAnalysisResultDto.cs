
namespace DocumentSystemApi.Dtos
{
    //Represents the complete result of a QA document analysis
    public class QAAnalysisResultDto
    {
        //Overall evaluation of the software testing results
        public string OverallAssessment { get; set; } = "";

        //Total number of test cases Documented
        //Null means the document does not provide this information
        public int? TotalTests { get; set; }

        //Number of successful test cases
        public int? PassedTests { get; set; }

        //Number of failed test cases
        public int? FailedTests { get; set; }

        //Number of blocked test cases
        public int? BlockedTests { get; set; }

        //Number of skipped test cases
        public int? SkippedTests { get; set; }

        //Important quality risks found in the Document
        public List<string> KeyRisks { get; set; } = new();

        //Recommended actions to improve software quality
        public List<string> Recommendations { get; set; } = new();

        //Individual defects or QA findings
        public List<QAFindingDto> Findings { get; set; } = new();
    }


    //Represents one issue or observation found in a QA Document
    public class QAFindingDto
    {
        //Short description of the issue
        public string Title { get; set; } = "";

        //Detailed explanation of the issue
        public string Description { get; set; } = "";

        //Type of finding, such as Defect or Risk
        public string Category { get; set; } = "";

        //Severity: Low, Medium, High or Critical
        public string Severity { get; set; } = "";

        //System feature affected by the issue
        public string AffectedArea { get; set; } = "";

        //Potential impact on users or system functionality
        public string Impact { get; set; } = "";

        //Suggested action to resolve or investigate the issue
        public string Recommendation { get; set; } = "";

        //Page number or chunk reference supporting the finding
        public string SourceLocation { get; set; } = "";
    }
}
