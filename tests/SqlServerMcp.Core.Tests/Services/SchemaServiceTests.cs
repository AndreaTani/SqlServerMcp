using Moq;
using SqlServerMcp.Core.Abstractions;
using SqlServerMcp.Core.Models;
using SqlServerMcp.Core.Security;
using SqlServerMcp.Core.Services;

namespace SqlServerMcp.Core.Tests.Services;

public class SchemaServiceTests
{
    private readonly Mock<ISchemaRepository> _repository = new(MockBehavior.Strict);

    [Fact]
    public async Task ListTablesAsync_FiltersOutTablesNotInAllowList()
    {
        // The database login can see more than it should (e.g. someone granted too much).
        IReadOnlyList<TableInfo> visibleToLogin =
        [
            new TableInfo("SalesLT", "Product", "Products sold"),
            new TableInfo("SalesLT", "Customer", "Contains password hashes"),
            new TableInfo("dbo", "BuildVersion", null),
        ];
        _repository
            .Setup(r => r.GetTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(visibleToLogin);

        var service = new SchemaService(_repository.Object, new TableAllowList(["SalesLT.Product"]));

        var result = await service.ListTablesAsync(CancellationToken.None);

        var table = Assert.Single(result);
        Assert.Equal("Product", table.Name);
    }

    [Fact]
    public async Task ListTablesAsync_SortsBySchemaThenName()
    {
        IReadOnlyList<TableInfo> unsorted =
        [
            new TableInfo("SalesLT", "SalesOrderHeader", null),
            new TableInfo("SalesLT", "Product", null),
        ];
        _repository
            .Setup(r => r.GetTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(unsorted);

        var service = new SchemaService(
            _repository.Object,
            new TableAllowList(["SalesLT.SalesOrderHeader", "SalesLT.Product"]));

        var result = await service.ListTablesAsync(CancellationToken.None);

        Assert.Collection(
            result,
            t => Assert.Equal("Product", t.Name),
            t => Assert.Equal("SalesOrderHeader", t.Name));
    }

    [Fact]
    public async Task ListTablesAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        _repository
            .Setup(r => r.GetTablesAsync(cts.Token))
            .ReturnsAsync(Array.Empty<TableInfo>());

        var service = new SchemaService(_repository.Object, new TableAllowList(["SalesLT.Product"]));

        await service.ListTablesAsync(cts.Token);

        _repository.Verify(r => r.GetTablesAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task ListTablesAsync_IgnoresMalformedNamesFromDatabase()
    {
        // The database returns malformed names; SchemaService must drop them instead of throwing.
        IReadOnlyList<TableInfo> visibleToLogin =
        [
            new TableInfo("SalesLT", "Product", "Products sold"),
            new TableInfo("Sales.LT", "Product", null),
            new TableInfo("SalesLT", "Product; DROP TABLE x", null),
        ];
        _repository
            .Setup(r => r.GetTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(visibleToLogin);

        var service = new SchemaService(_repository.Object, new TableAllowList(["SalesLT.Product"]));

        var result = await service.ListTablesAsync(CancellationToken.None);

        var table = Assert.Single(result);
        Assert.Equal("SalesLT", table.Schema);
        Assert.Equal("Product", table.Name);
    }

    [Fact]
    public async Task ListTablesAsync_RepositoryFails_PropagatesException()
    {
        var failureException = new InvalidOperationException("Database doesn't respond");
        _repository
            .Setup(r => r.GetTablesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(failureException);

        var service = new SchemaService(_repository.Object, new TableAllowList(["SalesLT.Product"]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ListTablesAsync(CancellationToken.None));

        Assert.Same(failureException, exception);
    }
}
