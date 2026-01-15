using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Aplication.DTOs.Requests;

public class DocumentFileUploadRequest
{
    public IFormFile? File { get; set; }
    [Required] 
    public string Filename { get; set; } = string.Empty;

    [Required] 
    public string? EncodedFile { get; set; }

    [Required] 
    public string ContentType { get; set; } = string.Empty;

    [Required] 
    public DocumentType DocumentType { get; set; }

    [Required] 
    public Channel Channel { get; set; }

    public string? CustomerId { get; set; }
    public string? CorrelationId { get; set; }
}