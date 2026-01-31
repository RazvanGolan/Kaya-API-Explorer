using System.Reflection;
using Kaya.ApiExplorer.Helpers;
using Kaya.ApiExplorer.Models;

namespace Kaya.ApiExplorer.Tests;

public class ReflectionHelperTests
{
    [Theory]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(int), "integer")]
    [InlineData(typeof(bool), "boolean")]
    [InlineData(typeof(DateTime), "datetime")]
    [InlineData(typeof(Guid), "guid")]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(double), "double")]
    [InlineData(typeof(float), "float")]
    [InlineData(typeof(void), "void")]
    [InlineData(typeof(object), "object")]
    public void GetFriendlyTypeName_ShouldReturnCorrectNames(Type type, string expected)
    {
        // Act
        var result = ReflectionHelper.GetFriendlyTypeName(type);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetFriendlyTypeName_ShouldHandleNullableTypes()
    {
        // Act
        var result = ReflectionHelper.GetFriendlyTypeName(typeof(int?));

        // Assert
        Assert.Equal("integer?", result);
    }

    [Fact]
    public void GetFriendlyTypeName_ShouldHandleArrays()
    {
        // Act
        var result = ReflectionHelper.GetFriendlyTypeName(typeof(List<string>));

        // Assert
        Assert.Equal("string[]", result);
    }

    [Fact]
    public void GetFriendlyTypeName_ShouldHandleDictionary()
    {
        // Act
        var result = ReflectionHelper.GetFriendlyTypeName(typeof(Dictionary<string, int>));

        // Assert
        Assert.Equal("Dictionary<string, integer>", result);
    }

    [Fact]
    public void IsComplexType_ShouldReturnTrueForCustomClasses()
    {
        // Act
        var result = ReflectionHelper.IsComplexType(typeof(TestUser));

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    public void IsComplexType_ShouldReturnFalseForPrimitiveTypes(Type type)
    {
        // Act
        var result = ReflectionHelper.IsComplexType(type);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComplexType_ShouldReturnFalseForDictionary()
    {
        // Act
        var result = ReflectionHelper.IsComplexType(typeof(Dictionary<string, int>));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSystemAssembly_ShouldReturnTrueForSystemAssemblies()
    {
        // Arrange
        var systemAssembly = typeof(string).Assembly;

        // Act
        var result = ReflectionHelper.IsSystemAssembly(systemAssembly);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSystemAssembly_ShouldReturnFalseForUserAssemblies()
    {
        // Arrange
        var userAssembly = Assembly.GetExecutingAssembly();

        // Act
        var result = ReflectionHelper.IsSystemAssembly(userAssembly);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CombineRoutes_ShouldCombineCorrectly()
    {
        // Act
        var result = ReflectionHelper.CombineRoutes("api/users", "profile");

        // Assert
        Assert.Equal("/api/users/profile", result);
    }

    [Fact]
    public void CombineRoutes_ShouldHandleEmptyAdditionalRoute()
    {
        // Act
        var result = ReflectionHelper.CombineRoutes("api/users", "");

        // Assert
        Assert.Equal("/api/users", result);
    }

    [Fact]
    public void CombineRoutes_ShouldHandleAbsoluteAdditionalRoute()
    {
        // Act
        var result = ReflectionHelper.CombineRoutes("api/users", "/absolute/path");

        // Assert
        Assert.Equal("/absolute/path", result);
    }

    [Fact]
    public void GenerateExampleJson_ShouldGenerateForPrimitiveTypes()
    {
        // Arrange
        var schemas = new Dictionary<string, ApiSchema>();
        var processedTypes = new HashSet<Type>();

        // Act
        var stringResult = ReflectionHelper.GenerateExampleJson(typeof(string), schemas, processedTypes);
        var intResult = ReflectionHelper.GenerateExampleJson(typeof(int), schemas, processedTypes);
        var boolResult = ReflectionHelper.GenerateExampleJson(typeof(bool), schemas, processedTypes);

        // Assert
        Assert.Equal("\"string value\"", stringResult);
        Assert.Equal("123", intResult);
        Assert.Equal("true", boolResult);
    }

    [Fact]
    public void GenerateExampleJson_ShouldGenerateForComplexType()
    {
        // Arrange
        var schemas = new Dictionary<string, ApiSchema>();
        var processedTypes = new HashSet<Type>();

        // Act
        var result = ReflectionHelper.GenerateExampleJson(typeof(TestUser), schemas, processedTypes);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("name", result.ToLower());
    }

    [Fact]
    public void GenerateSchemaForType_ShouldGenerateSchemaForComplexType()
    {
        // Act
        var schema = ReflectionHelper.GenerateSchemaForType(typeof(TestUser));

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("object", schema.Type);
        Assert.NotEmpty(schema.Example);
    }
}
