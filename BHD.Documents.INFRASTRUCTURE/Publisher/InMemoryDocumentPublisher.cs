using Aplication.Services.Documents.Publisher;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Publisher;

public class InMemoryDocumentPublisher : IDocumentPublisher
{
    private readonly ILogger<InMemoryDocumentPublisher> _logger;

    public InMemoryDocumentPublisher(ILogger<InMemoryDocumentPublisher> logger)
    {
        _logger = logger;
    }

    public async Task<string> PublishAsync(DocumentAsset document, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publicando documento: {DocumentId}, Filename: {Filename}, Size: {Size} bytes",
            document.Id, document.Filename, document.Size);

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        var url = $"https://storage.bhd.com.do/documents/{document.Id}/{document.Filename}";

        _logger.LogInformation(
            "Documento publicado exitosamente: {DocumentId}, URL: {Url}",
            document.Id, url);

        return url;
    }
}