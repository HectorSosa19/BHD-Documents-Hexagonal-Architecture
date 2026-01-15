using Domain.Enums;

namespace Domain.Repositories;

public class DocumentSearchCriteria
{
    public DateTime? UploadDateStart { get; set; }
    public DateTime? UploadDateEnd { get; set; }
    public string? Filename { get; set; }
    public string? ContentType { get; set; }
    public DocumentType? DocumentType { get; set; }
    public DocumentStatus? Status { get; set; }
    public string? CustomerId { get; set; }
    public Channel? Channel { get; set; }
    public string SortBy { get; set; } = "uploadDate";
    public string SortDirection { get; set; } = "Asc";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Skip => (PageNumber - 1) * PageSize;
}