// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using Kvr.SqlBuilder.Convention;
using Xunit;

namespace SqlBuilder.Tests.Convention;

public class SnakeCaseNameConventionTests
{
    [Theory]
    [InlineData("CustomerOrder", "customer_order")]
    [InlineData("firstName", "first_name")]
    [InlineData("ABC", "a_b_c")]
    [InlineData("PdfFile", "pdf_file")]
    public void ToTableName_WithoutPluralization_ReturnsSnakeCase(string input, string expected)
    {
        // Arrange
        var convention = SnakeCaseNameConvention.Create();

        // Act
        var result = convention.ToTableName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("CustomerOrder", "customer_orders")]
    [InlineData("firstName", "first_names")]
    [InlineData("ABC", "a_b_cs")]
    [InlineData("PdfFile", "pdf_files")]
    public void ToTableName_WithPluralization_ReturnsPluralizedSnakeCase(string input, string expected)
    {
        // Arrange
        var convention = SnakeCaseNameConvention.Create()
            .UsePluralTableNames();

        // Act
        var result = convention.ToTableName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("FirstName", "first_name")]
    [InlineData("Id", "id")]
    [InlineData("XmlData", "xml_data")]
    [InlineData("isActive", "is_active")]
    public void ToColumnName_ReturnsSnakeCase(string input, string expected)
    {
        // Arrange
        var convention = SnakeCaseNameConvention.Create();

        // Act
        var result = convention.ToColumnName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("abc")]
    public void ToColumnName_WithSimpleInput_ReturnsSameCase(string input)
    {
        // Arrange
        var convention = SnakeCaseNameConvention.Create();

        // Act
        var result = convention.ToColumnName(input);

        // Assert
        Assert.Equal(input.ToLower(), result);
    }

    [Fact]
    public void ToColumnName_WithNull_ReturnsNull()
    {
        // Arrange
        var convention = SnakeCaseNameConvention.Create();

        // Act
        var result = convention.EscapeIdentifierName(null);

        // Assert
        Assert.Null(result);
    }
} 