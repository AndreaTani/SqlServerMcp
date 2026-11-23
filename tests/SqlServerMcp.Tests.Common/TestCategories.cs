namespace SqlServerMcp.Tests.Common;

/// <summary>Trait names used to tag tests. Use these instead of string literals.</summary>
public static class TestTraits
{
    public const string Category = "Category";
}

/// <summary>Values for the <see cref="TestTraits.Category"/> trait.</summary>
public static class TestCategories
{
    /// <summary>Fast tests of a single class, no I/O. Run on every build.</summary>
    public const string Unit = "Unit";

    /// <summary>Tests that host the application or touch external resources.</summary>
    public const string Integration = "Integration";
}