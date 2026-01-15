using Aplication.Services.Documents;
using Aplication.Services.Documents.Publisher;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
namespace Infraestructure.BackgroundServices;

public class DocumentUploadProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDocumentUploadQueue _queue;
    private readonly ILogger<DocumentUploadProcessor> _logger;

    public DocumentUploadProcessor(
        IServiceProvider serviceProvider,
        IDocumentUploadQueue queue,
        ILogger<DocumentUploadProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    { 
        _logger.LogInformation("Document Upload Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var documentId = await _queue.DequeueAsync(stoppingToken);
                await ProcessDocumentUploadAsync(documentId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document upload");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Document Upload Processor stopped");
    }

    private async Task ProcessDocumentUploadAsync(string documentId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDocumentPublisher>();

        try
        {
            var document = await repository.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                _logger.LogWarning("Document not found: {DocumentId}", documentId);
                return;
            }

            var url = await publisher.PublishAsync(document, cancellationToken);

            document.DocumentStatus = DocumentStatus.Sent;
            document.Url = url;
            document.EncodedFile = null;

            await repository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Document processed: {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document: {DocumentId}", documentId);

            try
            {
                var document = await repository.GetByIdAsync(documentId, cancellationToken);
                if (document != null)
                {
                    document.DocumentStatus = DocumentStatus.Failed;
                    await repository.UpdateAsync(document, cancellationToken);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update document status: {DocumentId}", documentId);
            }
        }
    }
}

