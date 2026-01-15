namespace Aplication.Services.Documents;

public interface IDocumentUploadQueue
{
    ValueTask EnqueueAsync(string documentId, CancellationToken cancellationToken = default);
    ValueTask<string> DequeueAsync(CancellationToken cancellationToken = default);
}