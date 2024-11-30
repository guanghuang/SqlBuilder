// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System;

namespace Kvr.SqlBuilder.Convention;

/// <summary>
/// Provides the default naming convention for SQL identifiers, which preserves the original
/// names of types and properties without any transformation. Supports optional pluralization
/// and SQL Server-specific syntax.
/// </summary>
public class DefaultNameConvention: BaseNameConvention
{
    /// <summary>
    /// Private constructor to enforce factory method pattern.
    /// </summary>
    private DefaultNameConvention() { }

    /// <summary>
    /// Creates a new instance of DefaultNameConvention with default settings.
    /// </summary>
    /// <returns>A new instance of DefaultNameConvention</returns>
    public static DefaultNameConvention Create() => new();

    /// <summary>
    /// Configures whether table names should be pluralized.
    /// </summary>
    /// <param name="isPlural">Whether to pluralize table names</param>
    /// <returns>The current instance for method chaining</returns>
    public DefaultNameConvention UsePluralTableNames(bool isPlural = true) {
        IsPlural = isPlural;
        return this;
    }

    /// <summary>
    /// Configures whether SQL Server-specific syntax should be used.
    /// When enabled, identifiers will be wrapped in square brackets.
    /// </summary>
    /// <param name="isSqlServer">Whether to use SQL Server syntax</param>
    /// <returns>The current instance for method chaining</returns>
    public DefaultNameConvention UseSqlServer(bool isSqlServer = true) {
        IsSqlServer = isSqlServer;
        return this;
    }

    /// <summary>
    /// Returns the type name unchanged as the table name.
    /// </summary>
    /// <param name="typeName">The type name to convert</param>
    /// <returns>The original type name</returns>
    protected override string ToTableNameImpl(string typeName) => typeName;

    /// <summary>
    /// Returns the property name unchanged as the column name.
    /// </summary>
    /// <param name="propertyName">The property name to convert</param>
    /// <returns>The original property name</returns>
    protected override string ToColumnNameImpl(string propertyName) => propertyName;    
}
