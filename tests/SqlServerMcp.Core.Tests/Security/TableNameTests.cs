using SqlServerMcp.Core.Security;

namespace SqlServerMcp.Core.Tests.Security;

public class TableNameTests
{
    [Theory]
    [InlineData("SalesLT.Product", "SalesLT", "Product")]
    [InlineData("  SalesLT.Product  ", "SalesLT", "Product")]
    [InlineData("Product", "dbo", "Product")]
    [InlineData("_schema.Table_1", "_schema", "Table_1")]
    public void TryParse_ValidName_ReturnsSchemaAndName(string input, string schema, string name)
    {
        var ok = TableName.TryParse(input, out var table);

        Assert.True(ok);
        Assert.Equal(schema, table.Schema);
        Assert.Equal(name, table.Name);
    }

    // The agent controls this input, so these are the cases that matter most.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SalesLT.Product; DROP TABLE x")]
    [InlineData("SalesLT.[Product]")]
    [InlineData("SalesLT.Product]; --")]
    [InlineData("SalesLT.Product--")]
    [InlineData("SalesLT.Product/*x*/")]
    [InlineData("Product'")]
    [InlineData("server.db.schema.table")]
    [InlineData("db.schema.table")]
    [InlineData("1Table")]
    [InlineData(".Product")]
    [InlineData("SalesLT.")]
    [InlineData("Sales LT.Product")]
    public void TryParse_InvalidOrMaliciousName_ReturnsFalse(string? input)
    {
        Assert.False(TableName.TryParse(input, out _));
    }

    [Fact]
    public void TryParse_IdentifierLongerThan128Chars_ReturnsFalse()
    {
        var input = "dbo." + new string('a', 129);

        Assert.False(TableName.TryParse(input, out _));
    }

    [Fact]
    public void TryParse_Identifier128Chars_ReturnsTrue()
    {
        var tableName = new string('a', 128);
        var input = "dbo." + tableName;

        var ok = TableName.TryParse(input, out var table);

        Assert.True(ok);
        Assert.Equal("dbo", table.Schema);
        Assert.Equal(tableName, table.Name);
    }

    [Fact]
    public void Parse_InvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => TableName.Parse("x; DROP TABLE y"));
    }

    [Fact]
    public void ToQuotedIdentifier_ReturnsBracketedTwoPartName()
    {
        var table = TableName.Parse("SalesLT.Product");

        Assert.Equal("[SalesLT].[Product]", table.ToQuotedIdentifier());
    }

    [Fact]
    public void ToQuotedIdentifier_ReservedWord_IsBracketed()
    {
        var input = "dbo.Order";
        var expected = "[dbo].[Order]";

        var ok = TableName.TryParse(input, out var table);

        Assert.True(ok);
        Assert.Equal(expected, table.ToQuotedIdentifier());
    }
}
