
namespace DocumentSystemApi.Dtos
{
    //Represents text extracted from one document
    public class ExtractionResult
    {
        //All extracted text combined
        public string FullText { get; set; } = "";

        //The individual sections or pages
        public List<ExtractedSection> Sections { get; set; } = new();
    }

    //Represents text from one page or document section
    public class ExtractedSection
    {
        //Page number when available
        public int? PageNumber { get; set; }

        //Extracted text for this section
        public string Text { get; set; } = "";
    }
}
