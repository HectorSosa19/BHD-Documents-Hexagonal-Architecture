using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Aplication.DTOs.Requests;

public class DocumentUploadRequest
{
    [Required]
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("encodedFile")]
    public string EncodedFile { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("documentType")]
    public DocumentType DocumentType { get; set; }

    [Required]
    [JsonPropertyName("channel")]
    public Channel Channel { get; set; }

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}
