
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using DocumentSystemApi.Data;
using DocumentSystemApi.Models;
using System.IO.Compression;
using System.Text;
using DocumentSystemApi.Dtos;

namespace DocumentSystemApi.Services;

public class DocumentService
{
    private readonly DocumentDbContext _context;
    private readonly string _storagePath;

    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
    {
        ".pdf", ".docx", ".txt"
    };

    public DocumentService(DocumentDbContext context)
    {
        _context = context;

        //Set the location where uploaded documents will be stored
        _storagePath = @"..\Uploads";

        Directory.CreateDirectory(_storagePath);
    }

    //Upload Service
    public async Task<Document> UploadAsync(
        IFormFile file,
        string title,
        Guid userId)
    {
        //No file
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required.");

        //File too large
        if (file.Length > MaxFileSize)
            throw new ArgumentException(
                "Maximum file size is 10 MB.");

        // The original filename is untrusted input.
        string originalName = Path.GetFileName(
            file.FileName.Replace('\\', '/'));

        if (string.IsNullOrWhiteSpace(originalName) ||
            originalName.Length > 255)
            throw new ArgumentException("Invalid filename.");

        string extension = Path.GetExtension(originalName)
            .ToLowerInvariant();

        //Not Allowed File Type
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Unsupported file type.");

        title = title?.Trim() ?? "";
        //Empty tile or too long
        if (string.IsNullOrWhiteSpace(title) ||
            title.Length > 200)
            throw new ArgumentException(
                "Title must contain 1 to 200 characters.");

        // Random storage name. To avoid duplication.
        string storedName =
            $"{Guid.NewGuid():N}{extension}";

        string path = Path.Combine(_storagePath, storedName);

        // Save to private, non-public storage.
        await using (var stream = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write))
        {
            await file.CopyToAsync(stream);
        }

        try
        {
            //Validate file content
            ValidateFileContent(path, extension);

            // Find possible duplicates for THIS user only.
            var candidates = await _context.Documents
                .Where(r =>
                    r.UserId == userId &&
                    !r.IsArchived &&
                    r.FileSizeBytes == file.Length)
                .Select(r => r.FilePath)
                .ToListAsync();

            //Gether all the bytes inside the file
            byte[] uploadedBytes =
                await File.ReadAllBytesAsync(path);

            foreach (string existingPath in candidates)
            {
                if (!File.Exists(existingPath))
                    continue;

                byte[] existingBytes =
                    await File.ReadAllBytesAsync(existingPath);

                if (uploadedBytes.AsSpan()
                    .SequenceEqual(existingBytes))
                {
                    throw new InvalidOperationException(
                        "This document has already been uploaded.");
                }
            }

            var document = new Document
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                OriginalFileName = originalName,
                StoredFileName = storedName,
                FilePath = path,
                ContentType = extension switch
                {
                    ".pdf" => "application/pdf",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    _ => "text/plain"
                },
                FileSizeBytes = file.Length,
                Status = "Uploaded",
                DocumentType = "Unknown",
                UploadedAt = DateTimeOffset.UtcNow,
                IsArchived = false
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return document;
        }
        catch
        {
            // Clean up if validation or database save fails.
            if (File.Exists(path))
                File.Delete(path);

            throw;
        }
    }

    private static void ValidateFileContent(
        string path, string extension)
    {
        if (extension == ".pdf")
        {
            using var stream = File.OpenRead(path);
            byte[] header = new byte[5];

            if (stream.Read(header) != 5 ||
                Encoding.ASCII.GetString(header) != "%PDF-")
                throw new ArgumentException("Invalid PDF file.");
        }
        else if (extension == ".docx")
        {
            try
            {
                using var archive = ZipFile.OpenRead(path);

                if (archive.GetEntry("word/document.xml") == null)
                    throw new ArgumentException(
                        "Invalid Word document.");
            }
            catch (InvalidDataException)
            {
                throw new ArgumentException(
                    "Invalid Word document.");
            }
        }
        else if (extension == ".txt")
        {
            try
            {
                var utf8 = new UTF8Encoding(false, true);
                string content = utf8.GetString(
                    File.ReadAllBytes(path));

                if (content.Contains('\0'))
                    throw new ArgumentException(
                        "Invalid text file.");
            }
            catch (DecoderFallbackException)
            {
                throw new ArgumentException(
                    "Text file must use UTF-8.");
            }
        }
    }


    //Get all active documents belonging to a specific user
    public async Task<List<DocumentResponseDto>> GetUserDocumentsAsync(
        Guid userId)
    {
        //Find documents owned by the user that are not archived
        var documents = await _context.Documents
            .Where(r =>
                r.UserId == userId &&
                !r.IsArchived)

            //Display the newest documents first
            .OrderByDescending(r => r.UploadedAt)

            //Select only the fields we want to return
            .Select(r => new DocumentResponseDto
            {
                Id = r.Id,
                Title = r.Title,
                OriginalFileName = r.OriginalFileName,
                ContentType = r.ContentType,
                FileSizeBytes = r.FileSizeBytes,
                Status = r.Status,
                DocumentType = r.DocumentType,
                UploadedAt = r.UploadedAt
            }).ToListAsync();

        return documents;
    }


    //Get information about one document owned by the user
    public async Task<DocumentResponseDto?> GetDocumentByIdAsync(
        Guid documentId,
        Guid userId)
    {
        //Search using both document ID and user ID
        var document = await _context.Documents
            .Where(r =>
                r.Id == documentId &&
                r.UserId == userId &&
                !r.IsArchived)

            //Convert the Document into a Document Response Dto
            .Select(r => new DocumentResponseDto
            {
                Id = r.Id,
                Title = r.Title,
                OriginalFileName = r.OriginalFileName,
                ContentType = r.ContentType,
                FileSizeBytes = r.FileSizeBytes,
                Status = r.Status,
                DocumentType = r.DocumentType,
                UploadedAt = r.UploadedAt
            }).FirstOrDefaultAsync(); //Return the document or null if it doesn't exist

        return document;
    }


    //Archive a document without physically deleting it
    public async Task<bool> ArchiveDocumentAsync(
        Guid documentId,
        Guid userId)
    {
        //Find the document and verify its owner
        var document = await _context.Documents
            .FirstOrDefaultAsync(r =>
                r.Id == documentId &&
                r.UserId == userId &&
                !r.IsArchived);

        //Return false if the document wasn't found
        if (document == null)
            return false;

        //Mark the document as archived
        document.IsArchived = true;

        //Update the modification date
        document.UpdatedAt = DateTimeOffset.UtcNow;

        //Save changes
        await _context.SaveChangesAsync();

        return true;
    }


}
