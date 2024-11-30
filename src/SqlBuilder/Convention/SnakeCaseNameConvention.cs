// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System;

namespace Kvr.SqlBuilder.Convention;

/// <summary>
/// Provides a snake_case naming convention for SQL identifiers, converting PascalCase or camelCase
/// names to lowercase with underscores. Supports optional pluralization and SQL Server-specific syntax.
/// mainly for PostgreSQL
/// For example: "CustomerOrder" becomes "customer_order".
/// </summary>
public class SnakeCaseNameConvention: BaseNameConvention
{
    /// <summary>
    /// Private constructor to enforce factory method pattern.
    /// </summary>
    private SnakeCaseNameConvention() { }

    /// <summary>
    /// Creates a new instance of SnakeCaseNameConvention with default settings.
    /// </summary>
    /// <returns>A new instance of SnakeCaseNameConvention</returns>
    public static SnakeCaseNameConvention Create() => new();

    /// <summary>
    /// Configures whether table names should be pluralized.
    /// </summary>
    /// <param name="isPlural">Whether to pluralize table names</param>
    /// <returns>The current instance for method chaining</returns>
    public SnakeCaseNameConvention UsePluralTableNames(bool isPlural = true) {
        IsPlural = isPlural;
        return this;
    }

    /// <summary>
    /// Converts a type name to snake_case format.
    /// </summary>
    /// <param name="typeName">The type name to convert</param>
    /// <returns>The snake_case version of the type name</returns>
    protected override string ToTableNameImpl(string typeName) => ToSnakeCase(typeName);

    /// <summary>
    /// Converts a property name to snake_case format.
    /// </summary>
    /// <param name="propertyName">The property name to convert</param>
    /// <returns>The snake_case version of the property name</returns>
    protected override string ToColumnNameImpl(string propertyName) => ToSnakeCase(propertyName);

    /// <summary>
    /// Converts a PascalCase or camelCase string to snake_case.
    /// For example: "CustomerOrder" becomes "customer_order".
    /// </summary>
    /// <param name="input">The string to convert</param>
    /// <returns>The snake_case version of the input string</returns>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = new System.Text.StringBuilder(input.Length + 5);
        result.Append(char.ToLower(input[0]));

        for (var i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLower(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
