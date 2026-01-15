using Domain.Enums;

namespace Domain.Entities;

public class DocumentAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Filename { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public Channel Channel { get; set; }
    public string? CustomerId { get; set; }
    public DocumentStatus DocumentStatus { get; set; } = DocumentStatus.Received;
    public string? Url { get; set; }
    public long Size { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public byte[]? EncodedFile { get; set; }
}