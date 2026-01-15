using Aplication.DTOs.Requests;
using Aplication.DTOs.Response;
using Aplication.DTOs.Response.Pagination;
using Domain.Repositories;

namespace Aplication.Interfaces.Services;

public interface IDocumentService
{
    Task<DocumentUploadResponse> UploadDocumentAsync(DocumentUploadRequest request, CancellationToken cancellationToken = default);
    
    Task<PagedResponse<DocumentAssetResponse>> SearchDocumentsAsync(
        DocumentSearchCriteria criteria, 
        CancellationToken cancellationToken = default);
}