using SqlServerMcp.Core.Security;

namespace SqlServerMcp.Core.Tests.Security;

public class TableAllowListTests
{
    private readonly TableAllowList _allowList = new(["SalesLT.Product", "SalesLT.SalesOrderHeader"]);

    [Theory]
    [InlineData("SalesLT", "Product")]
    [InlineData("saleslt", "PRODUCT")]
    public void IsAllowed_ListedTable_ReturnsTrue(string schema, string name)
    {
        Assert.True(_allowList.IsAllowed(schema, name));
    }

    [Theory]
    [InlineData("SalesLT", "Customer")]
    [InlineData("dbo", "Product")]
    [InlineData("SalesLT", "Product; DROP TABLE x")]
    public void IsAllowed_UnlistedOrInvalidTable_ReturnsFalse(string schema, string name)
    {
        Assert.False(_allowList.IsAllowed(schema, name));
    }

    [Fact]
    public void Constructor_RemovesDuplicatesIgnoringCase()
    {
        var allowList = new TableAllowList(["SalesLT.Product", "saleslt.product"]);

        Assert.Single(allowList.Tables);
    }

    [Fact]
    public void Constructor_InvalidEntry_Throws()
    {
        // A typo or injection attempt in configuration must stop the server, not be skipped silently.
        Assert.Throws<ArgumentException>(() => new TableAllowList(["SalesLT.Product", "bad name"]));
    }
}
