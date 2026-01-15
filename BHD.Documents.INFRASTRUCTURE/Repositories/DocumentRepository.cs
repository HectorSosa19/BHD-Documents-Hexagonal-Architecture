using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DbContext.DataContext _context;
    public DocumentRepository(DbContext.DataContext  context)
    {
        _context = context;
    }
    public async Task<DocumentAsset> AddAsync(DocumentAsset document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task<DocumentAsset?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<PagedResult<DocumentAsset>> SearchAsync(DocumentSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(criteria);
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        query = ApplySorting(query, criteria.SortBy, criteria.SortDirection);
        
        var items = await query
            .Skip(criteria.Skip)
            .Take(criteria.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentAsset>(items, totalCount, criteria.PageNumber, criteria.PageSize);
    }

    public async Task<int> CountAsync(DocumentSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(criteria);
        return await query.CountAsync(cancellationToken);
    }

    private IQueryable<DocumentAsset> BuildQuery(DocumentSearchCriteria criteria)
    {
        var query = _context.Documents.AsQueryable();

        if (criteria.UploadDateStart.HasValue)
        {
            query = query.Where(d => d.UploadDate >= criteria.UploadDateStart.Value);
        }

        if (criteria.UploadDateEnd.HasValue)
        {
            query = query.Where(d => d.UploadDate <= criteria.UploadDateEnd.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Filename))
        {
            query = query.Where(d => d.Filename.Contains(criteria.Filename));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ContentType))
        {
            query = query.Where(d => d.ContentType == criteria.ContentType);
        }

        if (criteria.DocumentType.HasValue)
        {
            query = query.Where(d => d.DocumentType == criteria.DocumentType.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(d => d.DocumentStatus == criteria.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CustomerId))
        {
            query = query.Where(d => d.CustomerId == criteria.CustomerId);
        }

        if (criteria.Channel.HasValue)
        {
            query = query.Where(d => d.Channel == criteria.Channel.Value);
        }

        return query;
    }
    private static IQueryable<DocumentAsset> ApplySorting(IQueryable<DocumentAsset> query, string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.Equals("Desc", StringComparison.OrdinalIgnoreCase) ||
                           sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "uploaddate" => isDescending 
                ? query.OrderByDescending(d => d.UploadDate) 
                : query.OrderBy(d => d.UploadDate),
            "filename" => isDescending 
                ? query.OrderByDescending(d => d.Filename) 
                : query.OrderBy(d => d.Filename),
            "documenttype" => isDescending 
                ? query.OrderByDescending(d => d.DocumentType) 
                : query.OrderBy(d => d.DocumentType),
            "status" => isDescending 
                ? query.OrderByDescending(d => d.DocumentStatus) 
                : query.OrderBy(d => d.DocumentStatus),
            _ => isDescending 
                ? query.OrderByDescending(d => d.UploadDate) 
                : query.OrderBy(d => d.UploadDate)
        };
    }
    public async Task UpdateAsync(DocumentAsset document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(cancellationToken);
    }
}