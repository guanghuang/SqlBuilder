// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System;

namespace Kvr.SqlBuilder.Convention;

/// <summary>
/// Defines the contract for SQL naming conventions used in the SqlBuilder.
/// Implementations of this interface determine how type names and property names
/// are converted to table and column names in SQL queries.
/// </summary>
public interface INameConvention
{
    /// <summary>
    /// Converts a type name to its corresponding SQL table name according to the convention.
    /// </summary>
    /// <param name="typeName">The name of the type to convert</param>
    /// <returns>The SQL table name following the convention</returns>
    string ToTableName(string typeName);

    /// <summary>
    /// Converts a property name to its corresponding SQL column name according to the convention.
    /// </summary>
    /// <param name="propertyName">The name of the property to convert</param>
    /// <returns>The SQL column name following the convention</returns>
    string ToColumnName(string propertyName);

    /// <summary>
    /// Escapes an identifier name according to the SQL dialect rules (e.g., adding square brackets for SQL Server).
    /// </summary>
    /// <param name="identifierName">The identifier name to escape, can be null</param>
    /// <returns>The escaped identifier name, or null if the input was null</returns>
    string? EscapeIdentifierName(string? identifierName);
}
