

namespace Aplication.Services.Documents.Publisher;
using Domain.Entities;

public interface IDocumentPublisher
{
    Task<string> PublishAsync(DocumentAsset document, CancellationToken cancellationToken = default);
}