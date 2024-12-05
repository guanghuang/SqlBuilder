// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using kvr.SqlBuilder;
using Kvr.SqlBuilder.Convention;
using Xunit;

namespace SqlBuilder.Tests;

public class UtilsTests
{
    private readonly INameConvention _defaultConvention = DefaultNameConvention.Create();

    [Fact]
    public void GenerateSelectAllColumns_WithSimpleType_ReturnsAllColumns()
    {
        // Act
        var result = Utils.GenerateSelectAllColumns(typeof(TestClass), _defaultConvention);

        // Assert
        Assert.Equal("Name AS Name, Age AS Age, NullableAge AS NullableAge", result);
    }

    [Fact]
    public void GenerateSelectAllColumns_WithPrefix_ReturnsPrefixedColumns()
    {
        // Act
        var result = Utils.GenerateSelectAllColumns(typeof(TestClass), _defaultConvention, prefix: "t1");

        // Assert
        Assert.Equal("t1.Name AS Name, t1.Age AS Age, t1.NullableAge AS NullableAge", result);
    }

    [Fact]
    public void GenerateSelectAllColumns_WithExcludeColumns_ReturnsFilteredColumns()
    {
        // Arrange
        Expression<Func<TestClass, object>>[] excludeColumns = { x => x.Age };

        // Act
        var result = Utils.GenerateSelectAllColumns(typeof(TestClass), _defaultConvention, excludeColumns);

        // Assert
        Assert.Equal("Name AS Name, NullableAge AS NullableAge", result);
    }

    [Fact]
    public void GenerateSelectColumns_WithSpecificColumns_ReturnsSelectedColumns()
    {
        // Arrange
        var columns = new Expression<Func<TestClass, object>>[]
        {
            x => x.Name,
            x => x.Age
        };

        // Act
        var result = Utils.GenerateSelectColumns(_defaultConvention, columns);

        // Assert
        Assert.Equal("Name AS Name, Age AS Age", result);
    }

    [Fact]
    public void EncodeTable_WithSimpleTable_ReturnsTableName()
    {
        // Act
        var result = Utils.EncodeTable(typeof(TestClass), _defaultConvention);

        // Assert
        Assert.Equal("TestClass", result);
    }
    
    [Fact]
    public void EncodeTable_WithTableAttribute_ReturnsAttributeName()
    {
        // Act
        var result = Utils.EncodeTable(typeof(AttributeTestClass), _defaultConvention);

        // Assert
        Assert.Equal("CustomTable", result);
    }

    [Fact]
    public void EncodeTable_WithAlias_ReturnsTableNameWithAlias()
    {
        // Act
        var result = Utils.EncodeTable(typeof(TestClass), _defaultConvention, alias: "t1");

        // Assert
        Assert.Equal("TestClass t1", result);
    }

    [Fact]
    public void EncodeColumn_WithSimpleColumn_ReturnsColumnName()
    {
        // Arrange
        Expression<Func<TestClass, object>> expression = x => x.Name;

        // Act
        var result = Utils.EncodeColumn(expression, _defaultConvention, null, null, true);

        // Assert
        Assert.Equal("Name AS Name", result);
    }

    [Fact]
    public void EncodeColumn_WithPrefix_ReturnsPrefixedColumn()
    {
        // Arrange
        Expression<Func<TestClass, object>> expression = x => x.Name;

        // Act
        var result = Utils.EncodeColumn(expression, _defaultConvention, "t1", null, true);

        // Assert
        Assert.Equal("t1.Name AS Name", result);
    }

    [Fact]
    public void EncodeColumn_WithColumnAttribute_ReturnsAttributeName()
    {
        // Arrange
        Expression<Func<AttributeTestClass, object>> expression = x => x.CustomName;

        // Act
        var result = Utils.EncodeColumn(expression, _defaultConvention, null, null, true);

        // Assert
        Assert.Equal("RenamedColumn AS CustomName", result);
    }

    private class TestClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public int? NullableAge { get; set; }
    }

    [Table("CustomTable")]
    private class AttributeTestClass
    {
        [Column("RenamedColumn")]
        public string CustomName { get; set; }
    }
} 