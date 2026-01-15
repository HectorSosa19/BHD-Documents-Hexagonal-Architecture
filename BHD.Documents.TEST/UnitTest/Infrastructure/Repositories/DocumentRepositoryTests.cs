using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using FluentAssertions;
using Infraestructure.DbContext;
using Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BHD.Documents.TEST.UnitTest.Infrastructure.Repositories;

public class DocumentRepositoryTests : IDisposable
{
     private readonly DataContext _context;
    private readonly DocumentRepository _repository;

    public DocumentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataContext(options);
        _repository = new DocumentRepository(_context);
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ShouldPersistDocument()
    {
        var document = CreateTestDocument();

        var result = await _repository.AddAsync(document);

        result.Should().NotBeNull();
        result.Id.Should().Be(document.Id);
        
        var savedDocument = await _context.Documents.FindAsync(document.Id);
        savedDocument.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveAllProperties()
    {
        var document = new DocumentAsset
        {
            Id = Guid.NewGuid().ToString(),
            Filename = "test_file.pdf",
            ContentType = "application/pdf",
            DocumentType = DocumentType.Kyc,
            Channel = Channel.Branch,
            CustomerId = "CUST-12345",
            DocumentStatus = DocumentStatus.Received,
            Size = 2048,
            UploadDate = DateTime.UtcNow,
            CorrelationId = "CORR-67890",
            Url = "https://storage.example.com/doc123"
        };

        await _repository.AddAsync(document);

        var saved = await _context.Documents.FindAsync(document.Id);
        saved!.Filename.Should().Be(document.Filename);
        saved.ContentType.Should().Be(document.ContentType);
        saved.DocumentType.Should().Be(document.DocumentType);
        saved.Channel.Should().Be(document.Channel);
        saved.CustomerId.Should().Be(document.CustomerId);
        saved.DocumentStatus.Should().Be(document.DocumentStatus);
        saved.Size.Should().Be(document.Size);
        saved.CorrelationId.Should().Be(document.CorrelationId);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnDocument()
    {
        var document = CreateTestDocument();
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(document.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(document.Id);
        result.Filename.Should().Be(document.Filename);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var nonExistingId = Guid.NewGuid().ToString();

        var result = await _repository.GetByIdAsync(nonExistingId);

        result.Should().BeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ShouldModifyDocument()
    {
        var document = CreateTestDocument();
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();
        _context.Entry(document).State = EntityState.Detached;

        document.DocumentStatus = DocumentStatus.Sent;
        document.Url = "https://storage.example.com/updated";

        await _repository.UpdateAsync(document);

        var updated = await _context.Documents.FindAsync(document.Id);
        updated!.DocumentStatus.Should().Be(DocumentStatus.Sent);
        updated.Url.Should().Be("https://storage.example.com/updated");
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNoFilters_ShouldReturnAllDocuments()
    {
        await SeedTestDocuments(10);
        var criteria = new DocumentSearchCriteria
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        result.TotalCount.Should().Be(10);
        result.Items.Should().HaveCount(10);
    }

    [Fact]
    public async Task SearchAsync_WithPagination_ShouldReturnCorrectPage()
    {
        await SeedTestDocuments(25);
        var criteria = new DocumentSearchCriteria
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        result.TotalCount.Should().Be(25);
        result.Items.Should().HaveCount(10);
        result.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_FilterByDocumentType_ShouldReturnMatchingDocuments()
    {
        await SeedTestDocuments(10);
        await _context.Documents.AddAsync(new DocumentAsset
        {
            Id = Guid.NewGuid().ToString(),
            Filename = "kyc_special.pdf",
            ContentType = "application/pdf",
            DocumentType = DocumentType.Kyc,
            Channel = Channel.Digital,
            DocumentStatus = DocumentStatus.Received,
            UploadDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var criteria = new DocumentSearchCriteria
        {
            DocumentType = DocumentType.Kyc,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        result.Items.Should().AllSatisfy(d => d.DocumentType.Should().Be(DocumentType.Kyc));
    }

    [Fact]
    public async Task SearchAsync_FilterByChannel_ShouldReturnMatchingDocuments()
    {
        await _context.Documents.AddRangeAsync(
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "1.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Branch, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "2.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "3.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Branch, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var criteria = new DocumentSearchCriteria
        {
            Channel = Channel.Branch,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(d => d.Channel.Should().Be(Channel.Branch));
    }

    [Fact]
    public async Task SearchAsync_FilterByStatus_ShouldReturnMatchingDocuments()
    {
        await _context.Documents.AddRangeAsync(
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "1.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "2.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Sent, UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "3.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Failed, UploadDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var criteria = new DocumentSearchCriteria
        {
            Status = DocumentStatus.Sent,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        result.TotalCount.Should().Be(1);
        result.Items.First().DocumentStatus.Should().Be(DocumentStatus.Sent);
    }

    [Fact]
    public async Task SearchAsync_FilterByCustomerId_ShouldReturnMatchingDocuments()
    {
        await _context.Documents.AddRangeAsync(
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "1.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, CustomerId = "CUST-001", UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "2.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, CustomerId = "CUST-002", UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "3.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, CustomerId = "CUST-001", UploadDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var criteria = new DocumentSearchCriteria
        {
            CustomerId = "CUST-001",
            PageNumber = 1,
            PageSize = 20,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(d => d.CustomerId.Should().Be("CUST-001"));
    }

    [Fact]
    public async Task SearchAsync_FilterByFilename_ShouldReturnPartialMatches()
    {
        await _context.Documents.AddRangeAsync(
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "contract_2024.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "kyc_document.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Kyc, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "contract_2023.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var criteria = new DocumentSearchCriteria
        {
            Filename = "contract",
            PageNumber = 1,
            PageSize = 20,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(d => d.Filename.Should().Contain("contract"));
    }

    [Fact]
    public async Task SearchAsync_FilterByDateRange_ShouldReturnDocumentsInRange()
    {
  
        var now = DateTime.UtcNow;
        await _context.Documents.AddRangeAsync(
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "old.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = now.AddDays(-10) },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "recent.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = now.AddDays(-2) },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "today.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = now }
        );
        await _context.SaveChangesAsync();

        var criteria = new DocumentSearchCriteria
        {
            UploadDateStart = now.AddDays(-5),
            UploadDateEnd = now.AddDays(1),
            PageNumber = 1,
            PageSize = 20,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        result.TotalCount.Should().Be(2);
    }

    [Xunit.Theory]
    [InlineData("uploadDate", "Asc")]
    [InlineData("uploadDate", "Desc")]
    [InlineData("filename", "Asc")]
    [InlineData("filename", "Desc")]
    public async Task SearchAsync_WithSorting_ShouldOrderCorrectly(string sortBy, string sortDirection)
    {
        await SeedTestDocuments(5);
        var criteria = new DocumentSearchCriteria
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _repository.SearchAsync(criteria);

        result.Items.Should().NotBeEmpty();
        
        if (sortBy == "filename")
        {
            var filenames = result.Items.Select(d => d.Filename).ToList();
            if (sortDirection == "Asc")
                filenames.Should().BeInAscendingOrder();
            else
                filenames.Should().BeInDescendingOrder();
        }
    }

    [Fact]
    public async Task SearchAsync_WithMultipleFilters_ShouldApplyAllFilters()
    {
        await _context.Documents.AddRangeAsync(
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "contract.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Sent, CustomerId = "CUST-001", UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "contract.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Branch, DocumentStatus = DocumentStatus.Sent, CustomerId = "CUST-001", UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "kyc.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Kyc, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Sent, CustomerId = "CUST-001", UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "contract.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, CustomerId = "CUST-001", UploadDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var criteria = new DocumentSearchCriteria
        {
            DocumentType = DocumentType.Contract,
            Channel = Channel.Digital,
            Status = DocumentStatus.Sent,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var result = await _repository.SearchAsync(criteria);

        result.TotalCount.Should().Be(1);
        var doc = result.Items.First();
        doc.DocumentType.Should().Be(DocumentType.Contract);
        doc.Channel.Should().Be(Channel.Digital);
        doc.DocumentStatus.Should().Be(DocumentStatus.Sent);
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_WithNoFilters_ShouldReturnTotalCount()
    {
        await SeedTestDocuments(15);
        var criteria = new DocumentSearchCriteria();

        var count = await _repository.CountAsync(criteria);

        count.Should().Be(15);
    }

    [Fact]
    public async Task CountAsync_WithFilters_ShouldReturnFilteredCount()
    {
        await _context.Documents.AddRangeAsync(
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "1.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "2.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Kyc, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow },
            new DocumentAsset { Id = Guid.NewGuid().ToString(), Filename = "3.pdf", ContentType = "application/pdf", DocumentType = DocumentType.Contract, Channel = Channel.Digital, DocumentStatus = DocumentStatus.Received, UploadDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var criteria = new DocumentSearchCriteria
        {
            DocumentType = DocumentType.Contract
        };

        var count = await _repository.CountAsync(criteria);

        count.Should().Be(2);
    }

    #endregion

    #region Helper Methods

    private static DocumentAsset CreateTestDocument()
    {
        return new DocumentAsset
        {
            Id = Guid.NewGuid().ToString(),
            Filename = "test_document.pdf",
            ContentType = "application/pdf",
            DocumentType = DocumentType.Contract,
            Channel = Channel.Digital,
            CustomerId = "CUST-001",
            DocumentStatus = DocumentStatus.Received,
            Size = 1024,
            UploadDate = DateTime.UtcNow,
            CorrelationId = "CORR-001"
        };
    }

    private async Task SeedTestDocuments(int count)
    {
        var documents = Enumerable.Range(1, count)
            .Select(i => new DocumentAsset
            {
                Id = Guid.NewGuid().ToString(),
                Filename = $"document_{i:D3}.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.Contract,
                Channel = Channel.Digital,
                CustomerId = $"CUST-{i:D5}",
                DocumentStatus = DocumentStatus.Received,
                Size = 1024 * i,
                UploadDate = DateTime.UtcNow.AddHours(-i),
                CorrelationId = $"CORR-{i:D5}"
            });

        await _context.Documents.AddRangeAsync(documents);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #endregion
}