using System.Threading.Channels;

namespace Aplication.Services.Documents;

public class DocumentUploadQueue : IDocumentUploadQueue
{
    private readonly Channel<string> _queue;

    public DocumentUploadQueue()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<string>(options);
    }

    public async ValueTask EnqueueAsync(string documentId, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(documentId, cancellationToken);
    }

    public async ValueTask<string> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}