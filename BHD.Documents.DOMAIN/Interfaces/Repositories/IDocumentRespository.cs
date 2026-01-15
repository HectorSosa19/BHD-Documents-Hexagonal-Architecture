using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;


public interface IDocumentRepository
{
    Task<DocumentAsset> AddAsync(DocumentAsset document, CancellationToken cancellationToken = default);
    Task<DocumentAsset?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateAsync(DocumentAsset document, CancellationToken cancellationToken = default);
    Task<PagedResult<DocumentAsset>> SearchAsync(DocumentSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<int> CountAsync(DocumentSearchCriteria criteria, CancellationToken cancellationToken = default);
}

