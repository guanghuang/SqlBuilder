// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System;

namespace Kvr.SqlBuilder.Convention;

/// <summary>
/// Provides a base implementation for SQL naming conventions with common functionality
/// for handling pluralization and SQL Server-specific syntax.
/// </summary>
public abstract class BaseNameConvention: INameConvention
{
    /// <summary>
    /// Determines whether table names should be pluralized.
    /// </summary>
    protected bool IsPlural;

    /// <summary>
    /// Determines whether SQL Server-specific syntax should be used for identifiers.
    /// </summary>
    protected bool IsSqlServer;

    /// <summary>
    /// Formats a name according to SQL Server or standard SQL syntax.
    /// </summary>
    /// <param name="name">The name to format</param>
    /// <returns>The formatted name with appropriate SQL syntax</returns>
    protected string GetSqlName(string name) => IsSqlServer ? $"[{name}]" : name;

    /// <summary>
    /// Template method for converting a type name to a table name.
    /// Must be implemented by derived classes to define specific naming conventions.
    /// </summary>
    /// <param name="typeName">The type name to convert</param>
    /// <returns>The converted table name before pluralization and SQL formatting</returns>
    protected abstract string ToTableNameImpl(string typeName);

    /// <summary>
    /// Converts a type name to its corresponding SQL table name.
    /// Handles pluralization and SQL formatting after the base conversion.
    /// </summary>
    /// <param name="typeName">The type name to convert</param>
    /// <returns>The fully formatted SQL table name</returns>
    public virtual string ToTableName(string typeName) {
        var tableName = IsPlural ? $"{ToTableNameImpl(typeName)}s" : ToTableNameImpl(typeName);
        return GetSqlName(tableName);
    }

    /// <summary>
    /// Template method for converting a property name to a column name.
    /// Must be implemented by derived classes to define specific naming conventions.
    /// </summary>
    /// <param name="propertyName">The property name to convert</param>
    /// <returns>The converted column name before SQL formatting</returns>
    protected abstract string ToColumnNameImpl(string propertyName);

    /// <summary>
    /// Converts a property name to its corresponding SQL column name.
    /// Handles SQL formatting after the base conversion.
    /// </summary>
    /// <param name="propertyName">The property name to convert</param>
    /// <returns>The fully formatted SQL column name</returns>
    public virtual string ToColumnName(string propertyName) {
        return GetSqlName(ToColumnNameImpl(propertyName));
    }

    /// <summary>
    /// Escapes an identifier name according to the current SQL syntax settings.
    /// </summary>
    /// <param name="identifierName">The identifier name to escape</param>
    /// <returns>The escaped identifier name, or null if the input was null</returns>
    public string? EscapeIdentifierName(string? identifierName) => identifierName == null ? null : GetSqlName(identifierName);
}
