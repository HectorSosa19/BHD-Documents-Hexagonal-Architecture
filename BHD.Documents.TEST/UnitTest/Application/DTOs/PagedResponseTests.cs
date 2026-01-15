using Aplication.DTOs.Response.Pagination;
using FluentAssertions;
using Xunit;

namespace BHD.Documents.TEST.UnitTest.Application.DTOs;

public class PagedResponseTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldSetPropertiesCorrectly()
    {
        var items = new[] { "item1", "item2", "item3" };

        var response = new PagedResponse<string>(items, 1, 10, 50);

        response.Items.Should().HaveCount(3);
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(10);
        response.TotalCount.Should().Be(50);
    }

    [Fact]
    public void DefaultConstructor_ShouldInitializeWithDefaults()
    {
        var response = new PagedResponse<string>();

        response.Items.Should().BeEmpty();
        response.PageNumber.Should().Be(0);
        response.PageSize.Should().Be(0);
        response.TotalCount.Should().Be(0);
    }

    #endregion

    #region TotalPages Tests

    [Xunit.Theory]
    [InlineData(100, 10, 10)] 
    [InlineData(101, 10, 11)] 
    [InlineData(99, 10, 10)]   
    [InlineData(1, 10, 1)]     
    [InlineData(0, 10, 0)]     
    [InlineData(50, 25, 2)]    
    public void TotalPages_ShouldCalculateCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        var response = new PagedResponse<string>(
            Enumerable.Empty<string>(), 1, pageSize, totalCount);

        response.TotalPages.Should().Be(expectedPages);
    }

    #endregion

    #region HasPreviousPage Tests

    [Xunit.Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    [InlineData(100, true)]
    public void HasPreviousPage_ShouldReturnCorrectValue(int pageNumber, bool expected)
    {
        var response = new PagedResponse<string>(
            Enumerable.Empty<string>(), pageNumber, 10, 100);

        response.HasPreviousPage.Should().Be(expected);
    }

    #endregion

    #region HasNextPage Tests

    [Xunit.Theory]
    [InlineData(1, 10, 100, true)]   
    [InlineData(10, 10, 100, false)] 
    [InlineData(5, 10, 50, false)]   
    [InlineData(1, 10, 5, false)]   
    [InlineData(3, 10, 50, true)]  
    public void HasNextPage_ShouldReturnCorrectValue(int pageNumber, int pageSize, int totalCount, bool expected)
    {
        var response = new PagedResponse<string>(
            Enumerable.Empty<string>(), pageNumber, pageSize, totalCount);

        response.HasNextPage.Should().Be(expected);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void WithSinglePage_ShouldHaveNoPreviousOrNextPage()
    {
        var response = new PagedResponse<string>(
            new[] { "item1", "item2" }, 1, 10, 2);

        response.TotalPages.Should().Be(1);
        response.HasPreviousPage.Should().BeFalse();
        response.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void WithEmptyResult_ShouldReturnZeroTotalPages()
    {
        var response = new PagedResponse<string>(
            Enumerable.Empty<string>(), 1, 10, 0);

        response.TotalPages.Should().Be(0);
        response.HasPreviousPage.Should().BeFalse();
        response.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void WithExactPageSizeMatch_ShouldCalculateCorrectly()
    {
        var response = new PagedResponse<string>(
            Enumerable.Empty<string>(), 3, 10, 30);

        response.TotalPages.Should().Be(3);
        response.HasPreviousPage.Should().BeTrue();
        response.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ItemsCollection_ShouldBeModifiable()
    {
        var items = new List<string> { "item1", "item2" };
        var response = new PagedResponse<string>(items, 1, 10, 2);

        response.Items.Should().BeAssignableTo<IEnumerable<string>>();
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var response = new PagedResponse<string>();

        response.Items = new[] { "test" };
        response.PageNumber = 5;
        response.PageSize = 20;
        response.TotalCount = 100;

        response.Items.Should().HaveCount(1);
        response.PageNumber.Should().Be(5);
        response.PageSize.Should().Be(20);
        response.TotalCount.Should().Be(100);
        response.TotalPages.Should().Be(5);
    }

    #endregion
}