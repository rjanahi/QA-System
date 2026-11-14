
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentSystemApi.Dtos;
using UglyToad.PdfPig; //for pdf extraction
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor; 

namespace DocumentSystemApi.Services
{
    public class DocumentExtractionService
    {
        //Main method for extracting text from a document
        public ExtractionResult ExtractText(
            string filePath)
        {
            //Check whether the document exists
            if (!File.Exists(filePath))
                throw new FileNotFoundException(
                    "Document file was not found.");

            //Identify the document format
            string extension = Path.GetExtension(filePath)
                .ToLowerInvariant();

            //Call the correct extraction method
            return extension switch
            {
                ".txt" => ExtractTxt(filePath),
                ".pdf" => ExtractPdf(filePath),
                ".docx" => ExtractDocx(filePath),

                _ => throw new NotSupportedException(
                    "Unsupported document format.")
            };
        }


        //Extract text from a TXT document
        private ExtractionResult ExtractTxt(
            string filePath)
        {
            //Read the file using strict UTF-8 decoding
            var encoding = new UTF8Encoding(false, true);

            //Turn from byte to readable string
            string text = encoding.GetString(
                File.ReadAllBytes(filePath));

            //TXT files do not have real page numbers
            return new ExtractionResult
            {
                FullText = text,

                Sections = new List<ExtractedSection>
                {
                    new ExtractedSection
                    {
                        PageNumber = null,
                        Text = text
                    }
                }
            };
        }


        //Extract text from a PDF document
        private ExtractionResult ExtractPdf(
            string filePath)
        {
            var result = new ExtractionResult();

            var fullText = new StringBuilder();

            //Open the PDF file
            using var document = PdfDocument.Open(filePath);

            //Read each page separately
            foreach (var page in document.GetPages())
            {
                //Extract text using PDF content positioning
                string pageText =
                    ContentOrderTextExtractor.GetText(page);

                //Save the text with its original page number
                result.Sections.Add(new ExtractedSection
                {
                    PageNumber = page.Number,
                    Text = pageText
                });

                //Combine all pages into one text result
                fullText.AppendLine(pageText);
            }

            result.FullText = fullText.ToString();

            return result;
        }


        //Extract text from a Microsoft Word document
        private ExtractionResult ExtractDocx(
            string filePath)
        {
            //Open the Word document in read-only mode
            using var document =
                WordprocessingDocument.Open(filePath, false);

            //Locate the main document body
            var body = document.MainDocumentPart?
                .Document?
                .Body;

            //Reject Word documents without a readable body
            if (body == null)
                throw new InvalidDataException(
                    "The Word document has no readable body.");

            var textBuilder = new StringBuilder();

            //Read paragraphs from the document body
            foreach (var paragraph in
                body.Descendants<Paragraph>())
            {
                //Get the text inside the paragraph
                string paragraphText = paragraph.InnerText;

                //Skip empty paragraphs
                if (string.IsNullOrWhiteSpace(paragraphText))
                    continue;

                //Preserve paragraph separation
                textBuilder.AppendLine(paragraphText);
            }

            string text = textBuilder.ToString();

            //Word page numbers are not reliably available
            return new ExtractionResult
            {
                FullText = text,

                Sections = new List<ExtractedSection>
                {
                    new ExtractedSection
                    {
                        PageNumber = null,
                        Text = text
                    }
                }
            };
        }
    }
}
