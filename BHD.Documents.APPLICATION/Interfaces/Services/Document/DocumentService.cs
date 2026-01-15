
using Aplication.DTOs.Requests;
using Aplication.DTOs.Response;
using Aplication.DTOs.Response.Pagination;
using Aplication.Services.Documents;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using Microsoft.Extensions.Logging;

namespace Aplication.Interfaces.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IDocumentUploadQueue _uploadQueue;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository repository,
        IDocumentUploadQueue uploadQueue,
        ILogger<DocumentService> logger)
    {
        _repository = repository;
        _uploadQueue = uploadQueue;
        _logger = logger;
    }

    public async Task<DocumentUploadResponse> UploadDocumentAsync(DocumentUploadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            byte[] fileBytes = Convert.FromBase64String(request.EncodedFile!);

            var document = new DocumentAsset
            {
                Filename = request.Filename,
                ContentType = request.ContentType,
                DocumentType = request.DocumentType,
                Channel = request.Channel,
                CustomerId = request.CustomerId,
                CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString(),
                Size = fileBytes.Length,
                EncodedFile = fileBytes,
                DocumentStatus = DocumentStatus.Received,
                UploadDate = DateTime.UtcNow,
                
            };

            await _repository.AddAsync(document, cancellationToken);

            await _uploadQueue.EnqueueAsync(document.Id, cancellationToken);

            _logger.LogInformation(
                "Document upload accepted. Id: {DocumentId}, Filename: {Filename}, CorrelationId: {CorrelationId}",
                document.Id, document.Filename, document.CorrelationId);

            return new DocumentUploadResponse { Id = document.Id };
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid base64 encoded file");
            throw new ArgumentException("Invalid base64 encoded file", nameof(request.EncodedFile), ex);
        }
    }

    public async Task<PagedResponse<DocumentAssetResponse>> SearchDocumentsAsync(DocumentSearchCriteria criteria, CancellationToken cancellationToken = default)
    {

        var pagedResult = await _repository.SearchAsync(criteria, cancellationToken);

        var responseItems = pagedResult.Items.Select(MapToResponse).ToList();

        _logger.LogInformation(
            "Found {TotalCount} documents. Returning page {PageNumber} with {ItemCount} items",
            pagedResult.TotalCount,
            pagedResult.PageNumber,
            responseItems.Count);

        return new PagedResponse<DocumentAssetResponse>(
            responseItems,
            pagedResult.PageNumber,
            pagedResult.PageSize,
            pagedResult.TotalCount);
    }
    private static DocumentAssetResponse MapToResponse(DocumentAsset document)
    {
        return new DocumentAssetResponse
        {
            Id = document.Id.ToString(),
            Filename = document.Filename,
            ContentType = document.ContentType,
            DocumentType = document.DocumentType,
            Channel = document.Channel,
            CustomerId = document.CustomerId,
            Status = document.DocumentStatus,
            Url = document.Url,
            Size = document.Size,
            UploadDate = document.UploadDate,
            CorrelationId = document.CorrelationId
        };
    }
}