namespace DocumentSystemApi.Dtos
{
    //Data returned to the user about a document
    public class DocumentResponseDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = "";

        public string OriginalFileName { get; set; } = "";

        public string ContentType { get; set; } = "";

        public long FileSizeBytes { get; set; }

        public string Status { get; set; } = "";

        public string DocumentType { get; set; } = "";

        public DateTimeOffset UploadedAt { get; set; }
    }
}
