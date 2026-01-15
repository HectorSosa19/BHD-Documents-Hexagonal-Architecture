using Aplication.DTOs.Requests;
using Aplication.DTOs.Response;
using Aplication.DTOs.Response.Pagination;
using Aplication.Interfaces.Services;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BHD.Controllers;

[ApiController]
[Route("api/bhd/mgmt/1/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost("actions/upload")]
    [Authorize]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] DocumentUploadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _documentService.UploadDocumentAsync(request, cancellationToken);
            return Accepted(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid upload request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
    
    [HttpPost("actions/upload-file")]
    [Authorize]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadDocumentFile(
        [FromForm] DocumentFileUploadRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var file = request.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No se ha proporcionado ningún archivo" });
            }

            const long maxFileSize = 10 * 1024 * 1024; 
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = $"El archivo excede el tamaño máximo permitido de {maxFileSize / 1024 / 1024}MB" });
            }

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { error = $"Tipo de archivo no permitido. Extensiones permitidas: {string.Join(", ", allowedExtensions)}" });
            }

            _logger.LogInformation("Subiendo archivo: {FileName}, Tamaño: {Size} bytes", 
                file.FileName, file.Length);

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream, cancellationToken);
                fileBytes = memoryStream.ToArray();
            }

            var base64File = fileBytes;

            var uploadRequest = new DocumentUploadRequest
            {
                ContentType = file.ContentType,
                DocumentType = request.DocumentType,
                Filename = file.FileName,
                EncodedFile = Convert.ToBase64String(fileBytes),
                Channel = request.Channel,
                CustomerId = request.CustomerId,
                CorrelationId = request.CorrelationId,
            };

            var response = await _documentService.UploadDocumentAsync(uploadRequest, cancellationToken);

            _logger.LogInformation("Archivo subido exitosamente: {DocumentId}", response.Id);

            return Accepted(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid upload request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
    
    
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<DocumentAssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchDocuments(
        [FromQuery] DateTime? uploadDateStart,
        [FromQuery] DateTime? uploadDateEnd,
        [FromQuery] string? filename,
        [FromQuery] string? contentType,
        [FromQuery] DocumentType? documentType,
        [FromQuery] DocumentStatus? status,
        [FromQuery] string? customerId,
        [FromQuery] Channel? channel,
        [FromQuery] SortByField sortBy = SortByField.uploadDate,
        [FromQuery] SortDirection sortDirection = SortDirection.Asc,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validSortFields = new[] { "uploadDate", "filename", "documentType", "status" };
            if (!validSortFields.Contains(sortBy.ToString()))
                return BadRequest(new { error = "Invalid sortBy parameter. Valid values: uploadDate, filename, documentType, status" });

            if (pageNumber < 1)
                return BadRequest(new { error = "PageNumber must be greater than 0" });

            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "PageSize must be between 1 and 100" });

            var criteria = new DocumentSearchCriteria
            {
                UploadDateStart = uploadDateStart,
                UploadDateEnd = uploadDateEnd,
                Filename = filename,
                ContentType = contentType,
                DocumentType = documentType,
                Status = status,
                CustomerId = customerId,
                Channel = channel,
                SortBy = sortBy.ToString(),
                SortDirection = sortDirection.ToString(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var results = await _documentService.SearchDocumentsAsync(criteria, cancellationToken);
            
            Response.Headers.Append("X-Pagination-TotalCount", results.TotalCount.ToString());
            Response.Headers.Append("X-Pagination-PageNumber", results.PageNumber.ToString());
            Response.Headers.Append("X-Pagination-PageSize", results.PageSize.ToString());
            Response.Headers.Append("X-Pagination-TotalPages", results.TotalPages.ToString());
            Response.Headers.Append("X-Pagination-HasPreviousPage", results.HasPreviousPage.ToString());
            Response.Headers.Append("X-Pagination-HasNextPage", results.HasNextPage.ToString());
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}