// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Kvr.SqlBuilder.Convention;

namespace kvr.SqlBuilder;

/// <summary>
/// Provides utility methods for SQL query building operations, including table and column name resolution,
/// SQL encoding, and type mapping functionality. Supports customizable naming conventions through INameConvention.
/// </summary>
public static class Utils
{
    /// <summary>
    /// A set of supported database mapping types that can be directly mapped to SQL columns.
    /// Includes common data types like integers, decimals, strings, dates, and GUIDs.
    /// </summary>
    private static readonly HashSet<Type> MappingTypes = new()
    {
        // Integer Types
        typeof(byte), typeof(byte?),
        typeof(short), typeof(short?),
        typeof(int), typeof(int?),
        typeof(long), typeof(long?),

        // Decimal and Numeric Types
        typeof(decimal), typeof(decimal?),

        // Floating Point Types
        typeof(double), typeof(double?),
        typeof(float), typeof(float?),

        // String Types
        typeof(string),
        typeof(char), typeof(char?),

        // Date and Time Types
        typeof(DateTime), typeof(DateTime?),
        typeof(DateTimeOffset), typeof(DateTimeOffset?),
        typeof(TimeSpan), typeof(TimeSpan?),

        // Binary Types
        typeof(byte[]),

        // Boolean Type
        typeof(bool), typeof(bool?),

        // GUID Type
        typeof(Guid), typeof(Guid?)
    };

    /// <summary>
    /// Extracts the column name from a lambda expression, considering ColumnAttribute decorations.
    /// </summary>
    /// <param name="expression">Lambda expression pointing to a property</param>
    /// <returns>The resolved column name</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is not a property expression</exception>
    private static string GetColumnName(LambdaExpression expression)
    {
        var propertyInfo = GetMemberExpression(expression).Member as PropertyInfo;
        if (propertyInfo == null)
            throw new ArgumentException("Expression must be a property");

        var columnAttr = propertyInfo.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() as ColumnAttribute;
        return columnAttr?.Name ?? propertyInfo.Name;
    }

    /// <summary>
    /// Gets the column name from a PropertyInfo object, considering ColumnAttribute decorations.
    /// </summary>
    /// <param name="propertyInfo">PropertyInfo object to get the column name from</param>
    /// <returns>The resolved column name</returns>
    private static string GetColumnName(PropertyInfo propertyInfo)
    {
        var columnAttr = propertyInfo.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() as ColumnAttribute;
        return columnAttr?.Name ?? propertyInfo.Name;
    }
    
    /// <summary>
    /// Generates a SQL SELECT clause for all columns of a given type, with options for exclusions and ordering.
    /// Uses the provided naming convention for column name formatting.
    /// </summary>
    /// <param name="type">The type to generate columns for</param>
    /// <param name="nameConvention">The naming convention to use for formatting</param>
    /// <param name="excludeColumns">Optional array of columns to exclude</param>
    /// <param name="firstColumn">Optional column to place first in the selection</param>
    /// <param name="prefix">Optional prefix for column names (e.g., table alias)</param>
    /// <returns>A comma-separated list of encoded column names</returns>
    public static string GenerateSelectAllColumns(Type type, INameConvention nameConvention, LambdaExpression[]? excludeColumns = null, LambdaExpression? firstColumn = null, string? prefix = null)
    {
        var excludeColumnNames = excludeColumns?.Select(e => e.GetMemberExpression().Member.Name).ToArray() ?? Array.Empty<string>();
        var firstColumnName = firstColumn?.GetMemberExpression().Member.Name;
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var orderedProperties = properties
            .Where(p => MappingTypes.Contains(p.PropertyType) && !excludeColumnNames.Contains(p.Name));            
        if (firstColumnName != null) 
        {
            orderedProperties = orderedProperties.OrderBy(p => p.Name == firstColumnName ? 0 : 1);
        }
        var columns = orderedProperties.Select(p => EncodeColumn(p, nameConvention, prefix, nameConvention.EscapeIdentifierName(p.Name), true));
        return string.Join(", ", columns);
    }

    /// <summary>
    /// Generates a SQL SELECT clause for specified columns using the provided naming convention.
    /// </summary>
    /// <param name="nameConvention">The naming convention to use for formatting</param>
    /// <param name="columns">Array of lambda expressions selecting the desired columns</param>
    /// <param name="prefix">Optional prefix for column names (e.g., table alias)</param>
    /// <returns>A comma-separated list of encoded column names</returns>
    public static string GenerateSelectColumns(INameConvention nameConvention, LambdaExpression[] columns, string? prefix = null)
    {
        return string.Join(", ", columns.Select(c => EncodeColumn(c, nameConvention, prefix, null, true)));
    }

    /// <summary>
    /// Extracts the MemberExpression from a lambda expression, handling conversion expressions.
    /// </summary>
    /// <param name="expression">The lambda expression to process</param>
    /// <returns>The extracted MemberExpression</returns>
    /// <exception cref="ArgumentException">Thrown when the expression cannot be converted to a MemberExpression</exception>
    private static MemberExpression GetMemberExpression(this LambdaExpression expression)
    {
        var memberExpression = expression.Body as MemberExpression;
        if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand as MemberExpression;
        }

        if (memberExpression == null)
            throw new ArgumentException("Expression must be a member expression");
        
        return memberExpression;
    }

    /// <summary>
    /// Encodes a table name with an optional alias.
    /// </summary>
    /// <param name="tableName">The table name to encode</param>
    /// <param name="alias">Optional alias for the table</param>
    /// <returns>The encoded table name with optional alias</returns>
    public static string EncodeTable(string tableName, string? alias = null)
    {
        return alias == null ? tableName : $"{tableName} {alias}";
    }

    /// <summary>
    /// Encodes a table name for a given type using the provided naming convention.
    /// Considers TableAttribute decorations and allows for name overrides.
    /// </summary>
    /// <param name="type">The type to get the table name for</param>
    /// <param name="nameConvention">The naming convention to use for formatting</param>
    /// <param name="overrideName">Optional override name that takes precedence if provided</param>
    /// <param name="alias">Optional alias for the table</param>
    /// <returns>The encoded table name with optional alias</returns>
    public static string EncodeTable(Type type, INameConvention nameConvention, string? overrideName = null, string? alias = null)
    {
        if (overrideName != null)
            return alias == null ? overrideName : $"{overrideName} {alias}";

        var tableAttr = type.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
        var tableName = nameConvention.EscapeIdentifierName(tableAttr?.Name) ?? nameConvention.ToTableName(type.Name);
        return alias == null ? tableName : $"{tableName} {alias}";
    }

    /// <summary>
    /// Encodes a column name from a lambda expression using the provided naming convention.
    /// </summary>
    /// <param name="propertyExpression">The property expression identifying the column</param>
    /// <param name="nameConvention">The naming convention to use for formatting</param>
    /// <param name="prefix">Optional prefix for the column name</param>
    /// <param name="alias">Optional alias for the column</param>
    /// <param name="isAs">Whether to include AS keyword for aliasing</param>
    /// <returns>The encoded column name</returns>
    public static string EncodeColumn(LambdaExpression propertyExpression, INameConvention nameConvention, string? prefix, string? alias, bool isAs)
    {
        var propertyInfo = GetMemberExpression(propertyExpression).Member as PropertyInfo;
        if (propertyInfo == null)
            throw new ArgumentException("Expression must be a property");

        return EncodeColumn(propertyInfo, nameConvention, prefix, alias ?? nameConvention.EscapeIdentifierName(propertyInfo.Name), isAs);
    }

    /// <summary>
    /// Encodes a column name from a PropertyInfo using the provided naming convention.
    /// </summary>
    /// <param name="propertyInfo">The property info identifying the column</param>
    /// <param name="nameConvention">The naming convention to use for formatting</param>
    /// <param name="prefix">Optional prefix for the column name</param>
    /// <param name="alias">Optional alias for the column</param>
    /// <param name="isAs">Whether to include AS keyword for aliasing</param>
    /// <returns>The encoded column name</returns>
    private static string EncodeColumn(PropertyInfo propertyInfo, INameConvention nameConvention, string? prefix, string? alias, bool isAs)
    {
        var columnAttr = propertyInfo.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() as ColumnAttribute;
        var columnName = nameConvention.EscapeIdentifierName(columnAttr?.Name) ?? nameConvention.ToColumnName(propertyInfo.Name);
        return EncodeColumn(columnName, prefix, alias, isAs);
    }

    /// <summary>
    /// Encodes a column name with optional prefix and alias.
    /// </summary>
    /// <param name="columnName">The column name to encode</param>
    /// <param name="prefix">Optional prefix for the column name</param>
    /// <param name="alias">Optional alias for the column</param>
    /// <param name="isAs">Whether to include AS keyword for aliasing</param>
    /// <returns>The encoded column name</returns>
    public static string EncodeColumn(string columnName, string? prefix, string? alias, bool isAs)
    {
        var encoded = prefix == null ? columnName : $"{prefix}.{columnName}";
        return isAs ? $"{encoded} AS {alias ?? columnName}" : encoded;
    }
}