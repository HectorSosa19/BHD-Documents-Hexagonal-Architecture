using Aplication.DTOs.Requests;
using Aplication.Interfaces.Services;
using Aplication.Services.Documents;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BHD.Documents.TEST.UnitTest.Application.Services;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _mockRepository;
    private readonly Mock<IDocumentUploadQueue> _mockUploadQueue;
    private readonly Mock<ILogger<DocumentService>> _mockLogger;
    private readonly DocumentService _documentService;

    public DocumentServiceTests()
    {
        _mockRepository = new Mock<IDocumentRepository>();
        _mockUploadQueue = new Mock<IDocumentUploadQueue>();
        _mockLogger = new Mock<ILogger<DocumentService>>();
        _documentService = new DocumentService(
            _mockRepository.Object,
            _mockUploadQueue.Object,
            _mockLogger.Object);
    }

    #region UploadDocumentAsync Tests

    [Fact]
    public async Task UploadDocumentAsync_WithValidRequest_ShouldReturnDocumentUploadResponse()
    {
        var base64Content = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var request = new DocumentUploadRequest
        {
            Filename = "test_document.pdf",
            EncodedFile = base64Content,
            ContentType = "application/pdf",
            DocumentType = DocumentType.Contract,
            Channel = Channel.Digital,
            CustomerId = "CUST-001",
            CorrelationId = "CORR-001"
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentAsset doc, CancellationToken _) => doc);

        _mockUploadQueue
            .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var result = await _documentService.UploadDocumentAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUploadQueue.Verify(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldSetStatusToReceived()
    {
        var base64Content = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var request = new DocumentUploadRequest
        {
            Filename = "contract.pdf",
            EncodedFile = base64Content,
            ContentType = "application/pdf",
            DocumentType = DocumentType.Contract,
            Channel = Channel.Branch
        };

        DocumentAsset? capturedDocument = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentAsset, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync((DocumentAsset doc, CancellationToken _) => doc);

        _mockUploadQueue
            .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await _documentService.UploadDocumentAsync(request, CancellationToken.None);

        capturedDocument.Should().NotBeNull();
        capturedDocument!.DocumentStatus.Should().Be(DocumentStatus.Received);
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldSetUploadDateToUtcNow()
    {
        var base64Content = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var request = new DocumentUploadRequest
        {
            Filename = "kyc_doc.pdf",
            EncodedFile = base64Content,
            ContentType = "application/pdf",
            DocumentType = DocumentType.Kyc,
            Channel = Channel.Digital
        };

        DocumentAsset? capturedDocument = null;
        var beforeTest = DateTime.UtcNow;

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentAsset, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync((DocumentAsset doc, CancellationToken _) => doc);

        _mockUploadQueue
            .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await _documentService.UploadDocumentAsync(request, CancellationToken.None);

        var afterTest = DateTime.UtcNow;
        capturedDocument.Should().NotBeNull();
        capturedDocument!.UploadDate.Should().BeOnOrAfter(beforeTest);
        capturedDocument.UploadDate.Should().BeOnOrBefore(afterTest);
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldCalculateFileSizeCorrectly()
    {
        var fileContent = new byte[1024]; 
        var base64Content = Convert.ToBase64String(fileContent);
        var request = new DocumentUploadRequest
        {
            Filename = "test.pdf",
            EncodedFile = base64Content,
            ContentType = "application/pdf",
            DocumentType = DocumentType.Form,
            Channel = Channel.BackOffice
        };

        DocumentAsset? capturedDocument = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentAsset, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync((DocumentAsset doc, CancellationToken _) => doc);

        _mockUploadQueue
            .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await _documentService.UploadDocumentAsync(request, CancellationToken.None);

        capturedDocument.Should().NotBeNull();
        capturedDocument!.Size.Should().Be(1024);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithInvalidBase64_ShouldThrowArgumentException()
    {
        var request = new DocumentUploadRequest
        {
            Filename = "test.pdf",
            EncodedFile = "not-valid-base64!!!",
            ContentType = "application/pdf",
            DocumentType = DocumentType.Other,
            Channel = Channel.Other
        };

        var act = () => _documentService.UploadDocumentAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid base64*");
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldGenerateCorrelationIdIfNotProvided()
    {
        var base64Content = Convert.ToBase64String(new byte[] { 0x00 });
        var request = new DocumentUploadRequest
        {
            Filename = "test.pdf",
            EncodedFile = base64Content,
            ContentType = "application/pdf",
            DocumentType = DocumentType.Contract,
            Channel = Channel.Digital,
            CorrelationId = null
        };

        DocumentAsset? capturedDocument = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentAsset, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync((DocumentAsset doc, CancellationToken _) => doc);

        _mockUploadQueue
            .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await _documentService.UploadDocumentAsync(request, CancellationToken.None);

        capturedDocument.Should().NotBeNull();
        capturedDocument!.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [NUnit.Framework.Theory]
    [InlineData(DocumentType.Kyc)]
    [InlineData(DocumentType.Contract)]
    [InlineData(DocumentType.Form)]
    [InlineData(DocumentType.SupportingDocument)]
    [InlineData(DocumentType.Other)]
    public async Task UploadDocumentAsync_ShouldPreserveDocumentType(DocumentType documentType)
    {
        var base64Content = Convert.ToBase64String(new byte[] { 0x00 });
        var request = new DocumentUploadRequest
        {
            Filename = "test.pdf",
            EncodedFile = base64Content,
            ContentType = "application/pdf",
            DocumentType = documentType,
            Channel = Channel.Digital
        };

        DocumentAsset? capturedDocument = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentAsset, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync((DocumentAsset doc, CancellationToken _) => doc);

        _mockUploadQueue
            .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await _documentService.UploadDocumentAsync(request, CancellationToken.None);

        capturedDocument.Should().NotBeNull();
        capturedDocument!.DocumentType.Should().Be(documentType);
    }

    [InlineData(Channel.Branch)]
    [InlineData(Channel.Digital)]
    [InlineData(Channel.BackOffice)]
    [InlineData(Channel.Other)]
    public async Task UploadDocumentAsync_ShouldPreserveChannel(Channel channel)
    {
        var base64Content = Convert.ToBase64String(new byte[] { 0x00 });
        var request = new DocumentUploadRequest
        {
            Filename = "test.pdf",
            EncodedFile = base64Content,
            ContentType = "application/pdf",
            DocumentType = DocumentType.Contract,
            Channel = channel
        };

        DocumentAsset? capturedDocument = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentAsset, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync((DocumentAsset doc, CancellationToken _) => doc);

        _mockUploadQueue
            .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await _documentService.UploadDocumentAsync(request, CancellationToken.None);

        capturedDocument.Should().NotBeNull();
        capturedDocument!.Channel.Should().Be(channel);
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldEnqueueDocumentForAsyncProcessing()
    {
        var base64Content = Convert.ToBase64String(new byte[] { 0x00 });
        var request = new DocumentUploadRequest
        {
            Filename = "test.pdf",
            EncodedFile = base64Content,
            ContentType = "application/pdf",
            DocumentType = DocumentType.Contract,
            Channel = Channel.Digital
        };

        string? enqueuedDocumentId = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<DocumentAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentAsset doc, CancellationToken _) => doc);

        _mockUploadQueue
            .Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => enqueuedDocumentId = id)
            .Returns(ValueTask.CompletedTask);

        var result = await _documentService.UploadDocumentAsync(request, CancellationToken.None);

        enqueuedDocumentId.Should().NotBeNullOrEmpty();
        enqueuedDocumentId.Should().Be(result.Id);
    }

    #endregion

    #region SearchDocumentsAsync Tests

    [Fact]
    public async Task SearchDocumentsAsync_ShouldReturnPagedResponse()
    {
        var criteria = new DocumentSearchCriteria
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "uploadDate",
            SortDirection = "Asc"
        };

        var documents = GenerateTestDocuments(5);
        var pagedResult = new PagedResult<DocumentAsset>(documents, 5, 1, 10);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<DocumentSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _documentService.SearchDocumentsAsync(criteria, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithNoResults_ShouldReturnEmptyPage()
    {
        var criteria = new DocumentSearchCriteria
        {
            PageNumber = 1,
            PageSize = 10,
            Filename = "nonexistent"
        };

        var pagedResult = new PagedResult<DocumentAsset>(
            Enumerable.Empty<DocumentAsset>(), 0, 1, 10);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<DocumentSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _documentService.SearchDocumentsAsync(criteria, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldCalculateTotalPagesCorrectly()
    {
        var criteria = new DocumentSearchCriteria
        {
            PageNumber = 1,
            PageSize = 10
        };

        var documents = GenerateTestDocuments(10);
        var pagedResult = new PagedResult<DocumentAsset>(documents, 25, 1, 10);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<DocumentSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _documentService.SearchDocumentsAsync(criteria, CancellationToken.None);

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task SearchDocumentsAsync_FirstPage_ShouldNotHavePreviousPage()
    {
        var criteria = new DocumentSearchCriteria { PageNumber = 1, PageSize = 10 };
        var pagedResult = new PagedResult<DocumentAsset>(
            GenerateTestDocuments(10), 50, 1, 10);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<DocumentSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _documentService.SearchDocumentsAsync(criteria, CancellationToken.None);

        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task SearchDocumentsAsync_LastPage_ShouldNotHaveNextPage()
    {
        var criteria = new DocumentSearchCriteria { PageNumber = 5, PageSize = 10 };
        var pagedResult = new PagedResult<DocumentAsset>(
            GenerateTestDocuments(5), 45, 5, 10);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<DocumentSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _documentService.SearchDocumentsAsync(criteria, CancellationToken.None);

        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task SearchDocumentsAsync_MiddlePage_ShouldHaveBothNavigation()
    {
        var criteria = new DocumentSearchCriteria { PageNumber = 3, PageSize = 10 };
        var pagedResult = new PagedResult<DocumentAsset>(
            GenerateTestDocuments(10), 50, 3, 10);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<DocumentSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _documentService.SearchDocumentsAsync(criteria, CancellationToken.None);

        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldMapDocumentPropertiesCorrectly()
    {
        var criteria = new DocumentSearchCriteria { PageNumber = 1, PageSize = 10 };
        var document = new DocumentAsset
        {
            Id = Guid.NewGuid().ToString(),
            Filename = "test.pdf",
            ContentType = "application/pdf",
            DocumentType = DocumentType.Contract,
            Channel = Channel.Digital,
            CustomerId = "CUST-001",
            DocumentStatus = DocumentStatus.Sent,
            Size = 1024,
            UploadDate = DateTime.UtcNow,
            CorrelationId = "CORR-001"
        };

        var pagedResult = new PagedResult<DocumentAsset>(
            new[] { document }, 1, 1, 10);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<DocumentSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _documentService.SearchDocumentsAsync(criteria, CancellationToken.None);

        var item = result.Items.First();
        item.Id.Should().Be(document.Id);
        item.Filename.Should().Be(document.Filename);
        item.ContentType.Should().Be(document.ContentType);
        item.DocumentType.Should().Be(document.DocumentType);
        item.Channel.Should().Be(document.Channel);
        item.CustomerId.Should().Be(document.CustomerId);
        item.Status.Should().Be(document.DocumentStatus);
        item.Size.Should().Be(document.Size);
        item.CorrelationId.Should().Be(document.CorrelationId);
    }

    [Fact]
    public async Task SearchDocumentsAsync_ShouldCallRepositoryWithCorrectCriteria()
    {
        var criteria = new DocumentSearchCriteria
        {
            PageNumber = 2,
            PageSize = 20,
            DocumentType = DocumentType.Kyc,
            Channel = Channel.Branch,
            Status = DocumentStatus.Received,
            CustomerId = "CUST-TEST",
            Filename = "test",
            SortBy = "filename",
            SortDirection = "Desc"
        };

        DocumentSearchCriteria? capturedCriteria = null;
        var pagedResult = new PagedResult<DocumentAsset>(
            Enumerable.Empty<DocumentAsset>(), 0, 2, 20);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<DocumentSearchCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentSearchCriteria, CancellationToken>((c, _) => capturedCriteria = c)
            .ReturnsAsync(pagedResult);

        await _documentService.SearchDocumentsAsync(criteria, CancellationToken.None);

        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.PageNumber.Should().Be(2);
        capturedCriteria.PageSize.Should().Be(20);
        capturedCriteria.DocumentType.Should().Be(DocumentType.Kyc);
        capturedCriteria.Channel.Should().Be(Channel.Branch);
        capturedCriteria.Status.Should().Be(DocumentStatus.Received);
        capturedCriteria.CustomerId.Should().Be("CUST-TEST");
        capturedCriteria.Filename.Should().Be("test");
    }

    #endregion

    #region Helper Methods

    private static List<DocumentAsset> GenerateTestDocuments(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new DocumentAsset
            {
                Id = Guid.NewGuid().ToString(),
                Filename = $"document_{i}.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.Contract,
                Channel = Channel.Digital,
                CustomerId = $"CUST-{i:D5}",
                DocumentStatus = DocumentStatus.Received,
                Size = 1024 * i,
                UploadDate = DateTime.UtcNow.AddDays(-i),
                CorrelationId = $"CORR-{i:D5}"
            })
            .ToList();
    }

    #endregion
}