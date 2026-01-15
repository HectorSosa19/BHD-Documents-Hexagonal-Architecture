using Domain.Enums;

namespace Aplication.DTOs.Response;

public class DocumentAssetResponse
{
    public string Id { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public Channel Channel { get; set; }
    public string? CustomerId { get; set; }
    public DocumentStatus Status { get; set; }
    public string? Url { get; set; }
    public long Size { get; set; }
    public DateTime UploadDate { get; set; }
    public string? CorrelationId { get; set; }
}