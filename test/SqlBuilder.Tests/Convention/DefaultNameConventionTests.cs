// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using Kvr.SqlBuilder.Convention;
using Xunit;

namespace SqlBuilder.Tests.Convention;

public class DefaultNameConventionTests
{
    [Fact]
    public void ToTableName_WithoutPluralization_ReturnsOriginalName()
    {
        // Arrange
        var convention = DefaultNameConvention.Create();

        // Act
        var result = convention.ToTableName("Customer");

        // Assert
        Assert.Equal("Customer", result);
    }

    [Fact]
    public void ToTableName_WithPluralization_ReturnsPluralizedName()
    {
        // Arrange
        var convention = DefaultNameConvention.Create()
            .UsePluralTableNames();

        // Act
        var result = convention.ToTableName("Customer");

        // Assert
        Assert.Equal("Customers", result);
    }

    [Fact]
    public void ToTableName_WithSqlServer_ReturnsNameInBrackets()
    {
        // Arrange
        var convention = DefaultNameConvention.Create()
            .UseSqlServer();

        // Act
        var result = convention.ToTableName("Customer");

        // Assert
        Assert.Equal("[Customer]", result);
    }

    [Fact]
    public void ToColumnName_WithoutSqlServer_ReturnsOriginalName()
    {
        // Arrange
        var convention = DefaultNameConvention.Create();

        // Act
        var result = convention.ToColumnName("FirstName");

        // Assert
        Assert.Equal("FirstName", result);
    }

    [Fact]
    public void ToColumnName_WithSqlServer_ReturnsNameInBrackets()
    {
        // Arrange
        var convention = DefaultNameConvention.Create()
            .UseSqlServer();

        // Act
        var result = convention.ToColumnName("FirstName");

        // Assert
        Assert.Equal("[FirstName]", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void EscapeIdentifierName_WithNullOrEmptyInput_ReturnsInput(string? input)
    {
        // Arrange
        var convention = DefaultNameConvention.Create();

        // Act
        var result = convention.EscapeIdentifierName(input);

        // Assert
        Assert.Equal(input, result);
    }
} 