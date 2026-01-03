using Kaya.ApiExplorer.Helpers;

namespace Kaya.ApiExplorer.Tests;

/// <summary>
/// Test class for XML documentation helper
/// </summary>
public class XmlDocumentationHelperTests
{
    /// <summary>
    /// Test method for getting type summary
    /// </summary>
    [Fact]
    public void GetTypeSummary_ReturnsDocumentation_WhenXmlExists()
    {
        // Arrange
        var type = typeof(XmlDocumentationHelperTests);

        // Act
        var summary = XmlDocumentationHelper.GetTypeSummary(type);

        // Assert
        Assert.NotNull(summary);
        Assert.Contains("Test class for XML documentation helper", summary);
    }

    /// <summary>
    /// Sample method for testing method documentation
    /// </summary>
    /// <param name="value">A test parameter</param>
    /// <returns>Returns the value multiplied by 2</returns>
    public int SampleMethod(int value)
    {
        return value * 2;
    }

    [Fact]
    public void GetMethodSummary_ReturnsDocumentation_WhenXmlExists()
    {
        // Arrange
        var method = typeof(XmlDocumentationHelperTests).GetMethod(nameof(SampleMethod));

        // Act
        var summary = XmlDocumentationHelper.GetMethodSummary(method!);

        // Assert
        Assert.NotNull(summary);
        Assert.Contains("Sample method for testing method documentation", summary);
    }

    [Fact]
    public void GetParameterDescription_ReturnsDocumentation_WhenXmlExists()
    {
        // Arrange
        var method = typeof(XmlDocumentationHelperTests).GetMethod(nameof(SampleMethod));

        // Act
        var description = XmlDocumentationHelper.GetParameterDescription(method!, "value");

        // Assert
        Assert.NotNull(description);
        Assert.Contains("test parameter", description);
    }

    [Fact]
    public void GetReturnsDescription_ReturnsDocumentation_WhenXmlExists()
    {
        // Arrange
        var method = typeof(XmlDocumentationHelperTests).GetMethod(nameof(SampleMethod));

        // Act
        var returnsDesc = XmlDocumentationHelper.GetReturnsDescription(method!);

        // Assert
        Assert.NotNull(returnsDesc);
        Assert.Contains("multiplied by 2", returnsDesc);
    }

    [Fact]
    public void GetTypeSummary_ReturnsNull_WhenNoDocumentation()
    {
        // Arrange
        var type = typeof(object); // System types won't have our XML docs

        // Act
        var summary = XmlDocumentationHelper.GetTypeSummary(type);

        // Assert
        Assert.Null(summary);
    }
}
