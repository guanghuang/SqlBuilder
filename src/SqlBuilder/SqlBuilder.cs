// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text;
using kvr.SqlBuilder;
using Kvr.SqlBuilder.Convention;

namespace Kvr.SqlBuilder;

/// <summary>
/// A fluent SQL query builder that provides type-safe methods for constructing SQL queries.
/// Supports customizable naming conventions, table aliases, and complex query construction.
/// </summary>
public class SqlBuilder
{
    /// <summary>
    /// The current naming convention used for formatting table and column names.
    /// Defaults to DefaultNameConvention.
    /// </summary>
    private static INameConvention GlobalNameConvention = DefaultNameConvention.Create();

    /// <summary>Global mapping of types to custom table names</summary>
    private static readonly Dictionary<Type, string> GlobalExtraTableMapping = new();
    
    /// <summary>Prefix used for generating table aliases</summary>
    private static readonly string TableAliasPrefix = "kvr";
    private INameConvention _nameConvention;
    
    /// <summary>Counter for generating unique table aliases</summary>
    private int tableAliasIndex;
    
    /// <summary>Tracks if a SELECT clause has been started</summary>
    private bool hasSelect;
    
    /// <summary>Instance-specific mapping of types to custom table names</summary>
    private readonly Dictionary<Type, string> extraTableMapping;
    
    /// <summary>Maps types to their table prefixes/aliases</summary>
    private readonly Dictionary<Type, string> tablePrefixes = new();
    
    /// <summary>StringBuilder instance for constructing the SELECT part of the SQL query</summary>
    private readonly StringBuilder selectBuilder = new();

    /// <summary>StringBuilder instance for constructing the FROM and subsequent parts of the SQL query</summary>
    private readonly StringBuilder fromBuilder = new();

    /// <summary>
    /// Initializes a new instance of SqlBuilder with default settings from global configuration.
    /// </summary>
    public SqlBuilder()
    {
        extraTableMapping = new Dictionary<Type, string>(GlobalExtraTableMapping);
        _nameConvention = GlobalNameConvention;
    }

    /// <summary>
    /// Creates a new instance of SqlBuilder with default settings from global configuration.
    /// </summary>
    /// <returns></returns>
    public static SqlBuilder Create()
    {
        return new SqlBuilder();
    }

    /// <summary>
    /// Sets the global naming convention for all SqlBuilder instances.
    /// </summary>
    /// <param name="nameConvention">The naming convention to use</param>
    public static void UseGlobalNameConvention(INameConvention nameConvention) {
        GlobalNameConvention = nameConvention;
    }

    /// <summary>
    /// Maps a type to a custom table name globally.
    /// </summary>
    /// <typeparam name="T">The entity type to map</typeparam>
    /// <param name="name">The custom table name</param>
    public static void MapGlobalTable<T>(string name)
    {
        GlobalExtraTableMapping[typeof(T)] = name;
    }

    /// <summary>
    /// Maps a type to a custom table name for this instance.
    /// </summary>
    /// <typeparam name="T">The entity type to map</typeparam>
    /// <param name="name">The custom table name</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder MapTable<T>(string name)
    {
        extraTableMapping[typeof(T)] = name;
        return this;
    }

    /// <summary>
    /// Appends a SELECT clause or column to the query.
    /// </summary>
    /// <param name="sql">The SQL fragment to append</param>
    /// <param name="fromBegin">Whether to insert at the beginning of the SELECT clause. If true, will insert with comma if needed</param>
    private void AppendSelect(string sql, bool fromBegin = false)
    {
        if (hasSelect)
        {
            if (fromBegin)
            {
                selectBuilder.Insert(0, ", ");
            }
            else
            {
                selectBuilder.Append(", ");
            }
        }
        else
        {
            hasSelect = true;
        }
        if (fromBegin)
        {
            selectBuilder.Insert(0, sql);
        }
        else
        {
            selectBuilder.Append(sql).AppendLine();
        }
    }

    /// <summary>
    /// Gets or creates a table prefix/alias for a given type.
    /// </summary>
    /// <param name="type">The entity type</param>
    /// <param name="alwaysCreateNew">Whether to always create a new prefix</param>
    /// <returns>The table prefix/alias</returns>
    private string GetTablePrefix(Type type, bool alwaysCreateNew)
    {
        if (tablePrefixes.TryGetValue(type, out var prefix))
        {
            if (!alwaysCreateNew)
                return prefix;
            prefix = TableAliasPrefix + tableAliasIndex;
            tableAliasIndex++;
            return prefix;
        }

        var newPrefix = TableAliasPrefix + tableAliasIndex;
        tablePrefixes[type] = newPrefix;
        tableAliasIndex++;
        return newPrefix;
    }
    
    /// <summary>
    /// Adds a SELECT * clause for a type, with options to exclude specific columns and specify a first column.
    /// Uses the current naming convention for column formatting.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="excludePropertyExpressions">Properties to exclude from the selection</param>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <param name="fromBegin">Whether to insert at the beginning of the SELECT clause. If true, will insert with comma if needed</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>[]? excludePropertyExpressions, Expression<Func<T, object>>? firstPropertyExpression, string? prefix = null, bool fromBegin = false)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        AppendSelect(Utils.GenerateSelectAllColumns(typeof(T), _nameConvention, excludePropertyExpressions, firstPropertyExpression, prefix), fromBegin);
        return this;
    }

    /// <summary>
    /// Adds a SELECT * clause for two types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>  
    public SqlBuilder SelectAll<T, T1>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1);
    }

    /// <summary>
    /// Adds a SELECT * clause for three types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <typeparam name="T2">The third entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression2">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T, T1, T2>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null, Expression<Func<T2, object>>? firstPropertyExpression2 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1).SelectAll<T2>(firstPropertyExpression2);
    }

    /// <summary>
    /// Adds a SELECT * clause for four types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <typeparam name="T2">The third entity type</typeparam>
    /// <typeparam name="T3">The fourth entity type</typeparam> 
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression2">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression3">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T, T1, T2, T3>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null, Expression<Func<T2, object>>? firstPropertyExpression2 = null, Expression<Func<T3, object>>? firstPropertyExpression3 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1).SelectAll<T2>(firstPropertyExpression2).SelectAll<T3>(firstPropertyExpression3);
    }

    /// <summary>
    /// Adds a SELECT * clause for five types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <typeparam name="T2">The third entity type</typeparam>
    /// <typeparam name="T3">The fourth entity type</typeparam>
    /// <typeparam name="T4">The fifth entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression2">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression3">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression4">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T, T1, T2, T3, T4>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null, Expression<Func<T2, object>>? firstPropertyExpression2 = null, Expression<Func<T3, object>>? firstPropertyExpression3 = null, Expression<Func<T4, object>>? firstPropertyExpression4 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1).SelectAll<T2>(firstPropertyExpression2).SelectAll<T3>(firstPropertyExpression3).SelectAll<T4>(firstPropertyExpression4);
    }

    /// <summary>
    /// Adds a SELECT * clause for six types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <typeparam name="T2">The third entity type</typeparam>
    /// <typeparam name="T3">The fourth entity type</typeparam>
    /// <typeparam name="T4">The fifth entity type</typeparam>
    /// <typeparam name="T5">The sixth entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression2">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression3">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression4">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression5">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T, T1, T2, T3, T4, T5>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null, Expression<Func<T2, object>>? firstPropertyExpression2 = null, Expression<Func<T3, object>>? firstPropertyExpression3 = null, Expression<Func<T4, object>>? firstPropertyExpression4 = null, Expression<Func<T5, object>>? firstPropertyExpression5 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1).SelectAll<T2>(firstPropertyExpression2).SelectAll<T3>(firstPropertyExpression3).SelectAll<T4>(firstPropertyExpression4).SelectAll<T5>(firstPropertyExpression5);
    }

    /// <summary>
    /// Adds a SELECT * clause for six types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <typeparam name="T2">The third entity type</typeparam>
    /// <typeparam name="T3">The fourth entity type</typeparam>
    /// <typeparam name="T4">The fifth entity type</typeparam>
    /// <typeparam name="T5">The sixth entity type</typeparam>
    /// <typeparam name="T6">The seventh entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression2">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression3">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression4">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression5">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression6">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T, T1, T2, T3, T4, T5, T6>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null, Expression<Func<T2, object>>? firstPropertyExpression2 = null, Expression<Func<T3, object>>? firstPropertyExpression3 = null, Expression<Func<T4, object>>? firstPropertyExpression4 = null, Expression<Func<T5, object>>? firstPropertyExpression5 = null, Expression<Func<T6, object>>? firstPropertyExpression6 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1).SelectAll<T2>(firstPropertyExpression2).SelectAll<T3>(firstPropertyExpression3).SelectAll<T4>(firstPropertyExpression4).SelectAll<T5>(firstPropertyExpression5).SelectAll<T6>(firstPropertyExpression6);
    }

    /// <summary>
    /// Adds a SELECT * clause for seven types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <typeparam name="T2">The third entity type</typeparam>
    /// <typeparam name="T3">The fourth entity type</typeparam>
    /// <typeparam name="T4">The fifth entity type</typeparam>
    /// <typeparam name="T5">The sixth entity type</typeparam>
    /// <typeparam name="T6">The seventh entity type</typeparam>
    /// <typeparam name="T7">The eighth entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression2">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression3">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression4">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression5">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression6">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression7">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T, T1, T2, T3, T4, T5, T6, T7>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null, Expression<Func<T2, object>>? firstPropertyExpression2 = null, Expression<Func<T3, object>>? firstPropertyExpression3 = null, Expression<Func<T4, object>>? firstPropertyExpression4 = null, Expression<Func<T5, object>>? firstPropertyExpression5 = null, Expression<Func<T6, object>>? firstPropertyExpression6 = null, Expression<Func<T7, object>>? firstPropertyExpression7 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1).SelectAll<T2>(firstPropertyExpression2).SelectAll<T3>(firstPropertyExpression3).SelectAll<T4>(firstPropertyExpression4).SelectAll<T5>(firstPropertyExpression5).SelectAll<T6>(firstPropertyExpression6).SelectAll<T7>(firstPropertyExpression7);
    }

    /// <summary>
    /// Adds a SELECT * clause for eight types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <typeparam name="T2">The third entity type</typeparam>
    /// <typeparam name="T3">The fourth entity type</typeparam>
    /// <typeparam name="T4">The fifth entity type</typeparam>
    /// <typeparam name="T5">The sixth entity type</typeparam>
    /// <typeparam name="T6">The seventh entity type</typeparam>
    /// <typeparam name="T7">The eighth entity type</typeparam>
    /// <typeparam name="T8">The ninth entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression2">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression3">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression4">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression5">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression6">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression7">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression8">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T, T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null, Expression<Func<T2, object>>? firstPropertyExpression2 = null, Expression<Func<T3, object>>? firstPropertyExpression3 = null, Expression<Func<T4, object>>? firstPropertyExpression4 = null, Expression<Func<T5, object>>? firstPropertyExpression5 = null, Expression<Func<T6, object>>? firstPropertyExpression6 = null, Expression<Func<T7, object>>? firstPropertyExpression7 = null, Expression<Func<T8, object>>? firstPropertyExpression8 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1).SelectAll<T2>(firstPropertyExpression2).SelectAll<T3>(firstPropertyExpression3).SelectAll<T4>(firstPropertyExpression4).SelectAll<T5>(firstPropertyExpression5).SelectAll<T6>(firstPropertyExpression6).SelectAll<T7>(firstPropertyExpression7).SelectAll<T8>(firstPropertyExpression8);
    }

    /// <summary>
    /// Adds a SELECT * clause for nine types.
    /// </summary>
    /// <typeparam name="T">The first entity type</typeparam>
    /// <typeparam name="T1">The second entity type</typeparam>
    /// <typeparam name="T2">The third entity type</typeparam>
    /// <typeparam name="T3">The fourth entity type</typeparam>
    /// <typeparam name="T4">The fifth entity type</typeparam>
    /// <typeparam name="T5">The sixth entity type</typeparam>
    /// <typeparam name="T6">The seventh entity type</typeparam>
    /// <typeparam name="T7">The eighth entity type</typeparam>
    /// <typeparam name="T8">The ninth entity type</typeparam>
    /// <typeparam name="T9">The tenth entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression1">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression2">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression3">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression4">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression5">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression6">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression7">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression8">Property to place first in the selection</param>
    /// <param name="firstPropertyExpression9">Property to place first in the selection</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Expression<Func<T, object>>? firstPropertyExpression = null, Expression<Func<T1, object>>? firstPropertyExpression1 = null, Expression<Func<T2, object>>? firstPropertyExpression2 = null, Expression<Func<T3, object>>? firstPropertyExpression3 = null, Expression<Func<T4, object>>? firstPropertyExpression4 = null, Expression<Func<T5, object>>? firstPropertyExpression5 = null, Expression<Func<T6, object>>? firstPropertyExpression6 = null, Expression<Func<T7, object>>? firstPropertyExpression7 = null, Expression<Func<T8, object>>? firstPropertyExpression8 = null, Expression<Func<T9, object>>? firstPropertyExpression9 = null) 
    {
        return SelectAll<T>(firstPropertyExpression).SelectAll<T1>(firstPropertyExpression1).SelectAll<T2>(firstPropertyExpression2).SelectAll<T3>(firstPropertyExpression3).SelectAll<T4>(firstPropertyExpression4).SelectAll<T5>(firstPropertyExpression5).SelectAll<T6>(firstPropertyExpression6).SelectAll<T7>(firstPropertyExpression7).SelectAll<T8>(firstPropertyExpression8).SelectAll<T9>(firstPropertyExpression9);
    }

    /// <summary>
    /// Adds a SELECT * clause for a type, with options to exclude specific columns and specify a first column.
    /// Uses the current naming convention for column formatting and generates a new table prefix.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="excludePropertyExpressions">Properties to exclude from the selection</param>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>[]? excludePropertyExpressions, Expression<Func<T, object>>? firstPropertyExpression, out string prefix)
    {
        prefix = GetTablePrefix(typeof(T), true);
        AppendSelect(Utils.GenerateSelectAllColumns(typeof(T), _nameConvention, excludePropertyExpressions, firstPropertyExpression, prefix));
        return this;
    }

    /// <summary>
    /// Adds a SELECT * clause for a type, excluding specific columns.
    /// Uses the current naming convention for column formatting.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="excludePropertyExpressions">Properties to exclude from the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>[]? excludePropertyExpressions, string? prefix = null)
    {
        return SelectAll(excludePropertyExpressions, null, prefix);
    }
    
    /// <summary>
    /// Adds a SELECT * clause for a type with a specified first column.
    /// Uses the current naming convention for column formatting and generates a new table prefix.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>? firstPropertyExpression, out string prefix)
    {
        return SelectAll(null, firstPropertyExpression, out prefix);
    }

    /// <summary>
    /// Generates a SELECT * clause for a type, with a WHERE clause to filter by a specific property value.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <param name="propertyExpression">Property to place first in the selection</param>
    /// <param name="value">The value to compare against</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectFromWhere<T, TKey>(Expression<Func<T, TKey>> propertyExpression, string value)
    {
        return SelectAll<T>().From<T>().Where(propertyExpression, value);
    }

    /// <summary>
    /// Adds a SELECT * clause for a type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectFrom<T>()
    {
        return SelectAll<T>().From<T>();
    }

    /// <summary>
    /// Adds a SELECT * clause for a type.
    /// Uses the current naming convention for column formatting and generates a new table prefix.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(out string prefix)
    {
        return SelectAll<T>(null, null, out prefix);
    }

    /// <summary>
    /// Adds a SELECT * clause for a type with a specified first column.
    /// Uses the current naming convention for column formatting.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="firstPropertyExpression">Property to place first in the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>? firstPropertyExpression, string? prefix = null)
    {
        return SelectAll(null, firstPropertyExpression, prefix);
    }

    /// <summary>
    /// Adds a SELECT * clause for a type.
    /// Uses the current naming convention for column formatting.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(string? prefix = null)
    {
        return SelectAll<T>(null, null, prefix);
    }

    /// <summary>
    /// Adds a SELECT clause for specific columns of a type.
    /// Uses the current naming convention for column formatting and generates a new table prefix.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="includePropertyExpressions">Properties to include in the selection</param>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(Expression<Func<T, object>>[] includePropertyExpressions, out string prefix)
    {
        prefix = GetTablePrefix(typeof(T), true);
        AppendSelect(Utils.GenerateSelectColumns(_nameConvention, includePropertyExpressions, prefix));
        return this;
    }

    /// <summary>
    /// Adds a SELECT clause for specific columns of a type.
    /// Uses the current naming convention for column formatting.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="includePropertyExpressions">Properties to include in the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(Expression<Func<T, object>>[] includePropertyExpressions, string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        AppendSelect(Utils.GenerateSelectColumns(_nameConvention, includePropertyExpressions, prefix));
        return this;
    }

    /// <summary>
    /// Creates a table alias without selecting any columns.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectNone<T>(out string prefix)
    {
        prefix = GetTablePrefix(typeof(T), true);
        return this;
    }
    

    /// <summary>
    /// Adds a SELECT clause for a single column with optional aliasing.
    /// Uses the current naming convention for column formatting.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="propertyExpression">The property to select</param>
    /// <param name="alias">Optional alias for the column</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(Expression<Func<T, object>> propertyExpression, string? alias = null, string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        AppendSelect(Utils.EncodeColumn(propertyExpression, _nameConvention, prefix, alias, true));
        return this;
    }

    /// <summary>
    /// Adds a SELECT clause for a single column of a type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="propertyExpression">The column to select</param>
    /// <param name="alias">Optional column alias</param>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(Expression<Func<T, object>> propertyExpression, string? alias, out string prefix)
    {
        prefix = GetTablePrefix(typeof(T), true);
        AppendSelect(Utils.EncodeColumn(propertyExpression, _nameConvention, prefix, alias, true));
        return this;
    }
    
    /// <summary>
    /// Adds a SELECT clause for a single column of a type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="column">The column to select</param>
    /// <param name="alias">Optional column alias</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(string column, string? alias, string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        // here pass null for INameConvention
        AppendSelect(Utils.EncodeColumn(column, prefix, alias, true));
        return this;
    }

    /// <summary>
    /// Adds a SELECT clause for a single column of a type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="column">The column to select</param>
    /// <param name="alias">Optional column alias</param>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(string column, string? alias, out string prefix)
    {
        prefix = GetTablePrefix(typeof(T), true);
        // here pass null for INameConvention
        AppendSelect(Utils.EncodeColumn(column, prefix, alias, true));
        return this;
    }

    /// <summary>
    /// Adds a FROM clause to the query.
    /// </summary>
    /// <typeparam name="T">The entity type representing the table</typeparam>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder From<T>(string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        fromBuilder.Append(" FROM ");
        fromBuilder.Append(Utils.EncodeTable(typeof(T), _nameConvention, extraTableMapping.ContainsKey(typeof(T)) ? extraTableMapping[typeof(T)] : null, prefix ?? GetTablePrefix(typeof(T), false)))
            .AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a WHERE clause to the query.   
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TResult">The column type</typeparam>
    /// <param name="propertyExpression">The column expression</param>
    /// <param name="value">The value to compare against</param>
    /// <param name="op">The comparison operator (defaults to "=")</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Where<T, TResult>(Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        fromBuilder.Append(" WHERE ");
        fromBuilder.Append(Utils.EncodeColumn(propertyExpression, _nameConvention, prefix, null, false));
        fromBuilder.Append(" ").Append(op).Append(" ").Append(value).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a WHERE clause to the query.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Where(string rawSql)
    {
        fromBuilder.Append(" WHERE ").Append(rawSql).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an AND condition to the WHERE clause.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TResult">The column type</typeparam>
    /// <param name="propertyExpression">The column expression</param>
    /// <param name="value">The value to compare against</param>
    /// <param name="op">The comparison operator (defaults to "=")</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder And<T, TResult>(Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        fromBuilder.Append(" AND ");
        fromBuilder.Append(Utils.EncodeColumn(propertyExpression, _nameConvention, prefix, null, false));
        fromBuilder.Append(" ").Append(op).Append(" ").Append(value).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an AND condition to the WHERE clause.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder And(string rawSql)
    {
        fromBuilder.Append(" AND ").Append(rawSql).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an OR condition to the WHERE clause.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult">The column type</typeparam>
    /// <param name="propertyExpression">The column expression</param>
    /// <param name="value">The value to compare against</param>
    /// <param name="op">The comparison operator (defaults to "=")</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Or<T, TResult>(Expression<Func<T, TResult>> propertyExpression, string value, string op = "=", string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        fromBuilder.Append(" OR ");
        fromBuilder.Append(Utils.EncodeColumn(propertyExpression, _nameConvention, prefix, null, false));
        fromBuilder.Append(" ").Append(op).Append(" ").Append(value).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an OR condition to the WHERE clause.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Or(string rawSql)
    {
        fromBuilder.Append(" OR ").Append(rawSql).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an ORDER BY clause to the query.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TResult">The column type</typeparam>
    /// <param name="propertyExpression">The column expression</param>
    /// <param name="ascOrder">Whether to sort in ascending order (defaults to true)</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder OrderBy<T, TResult>(Expression<Func<T, TResult>> propertyExpression, bool ascOrder = true, string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        fromBuilder.Append(" ORDER BY ");
        fromBuilder.Append(Utils.EncodeColumn(propertyExpression, _nameConvention, prefix, null, false));
        fromBuilder.Append(" ").Append(ascOrder ? "ASC" : "DESC").AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an ORDER BY clause to the query.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <param name="ascOrder">Whether to sort in ascending order (defaults to true)</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder OrderBy(string rawSql, bool ascOrder = true)
    {
        fromBuilder.Append(" ORDER BY ").Append(rawSql).Append(" ").Append(ascOrder ? "ASC" : "DESC").AppendLine();
        return this;
    }

    /// <summary>
    /// Helper method to build JOIN clauses.
    /// </summary>
    /// <typeparam name="TLeft">The left table's entity type</typeparam>
    /// <typeparam name="TRight">The right table's entity type</typeparam>
    /// <typeparam name="TValue">The join key's type</typeparam>
    /// <param name="leftPropertyExpression">Expression for the left table's join key</param>
    /// <param name="rightPropertyExpression">Expression for the right table's join key</param>
    /// <param name="leftPrefix">Optional left table prefix/alias</param>
    /// <param name="rightPrefix">Optional right table prefix/alias</param>
    /// <param name="op">The join operator (defaults to "=")</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    private SqlBuilder BuildJoinClause<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> leftPropertyExpression, Expression<Func<TRight, TValue>> rightPropertyExpression, string? leftPrefix,
        string? rightPrefix,
        string op)
    {
        leftPrefix ??= GetTablePrefix(typeof(TLeft), false);
        rightPrefix ??= GetTablePrefix(typeof(TRight), false);
        fromBuilder.Append(Utils.EncodeTable(
            typeof(TRight), _nameConvention, extraTableMapping.ContainsKey(typeof(TRight)) ? extraTableMapping[typeof(TRight)] : null, rightPrefix ?? GetTablePrefix(typeof(TRight), false)));
        fromBuilder.Append(" ON ");
        fromBuilder.Append(Utils.EncodeColumn(leftPropertyExpression, _nameConvention, leftPrefix ?? GetTablePrefix(typeof(TLeft), false), null, false));
        fromBuilder.Append(" ").Append(op).Append(" ")
            .Append(Utils.EncodeColumn(rightPropertyExpression, _nameConvention, rightPrefix ?? GetTablePrefix(typeof(TRight), false), null, false))
            .AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an INNER JOIN clause to the query.
    /// </summary>
    /// <typeparam name="TLeft">The left table's entity type</typeparam>
    /// <typeparam name="TRight">The right table's entity type</typeparam>
    /// <typeparam name="TValue">The join key's type</typeparam>
    /// <param name="leftPropertyExpression">Expression for the left table's join key</param>
    /// <param name="rightPropertyExpression">Expression for the right table's join key</param>
    /// <param name="leftPrefix">Optional left table prefix/alias</param>
    /// <param name="rightPrefix">Optional right table prefix/alias</param>
    /// <param name="op">The join operator (defaults to "=")</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Join<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> leftPropertyExpression, Expression<Func<TRight, TValue>> rightPropertyExpression, string? leftPrefix = null,
        string? rightPrefix = null,
        string op = "=")
    {
        fromBuilder.Append(" JOIN ");
        return BuildJoinClause(leftPropertyExpression, rightPropertyExpression, leftPrefix, rightPrefix, op);
    }

    /// <summary>
    /// Adds a LEFT JOIN clause to the query.
    /// </summary>
    /// <typeparam name="TLeft">The left table's entity type</typeparam>
    /// <typeparam name="TRight">The right table's entity type</typeparam>
    /// <typeparam name="TValue">The join key's type</typeparam>
    /// <param name="leftPropertyExpression">Expression for the left table's join key</param>
    /// <param name="rightPropertyExpression">Expression for the right table's join key</param>
    /// <param name="leftPrefix">Optional left table prefix/alias</param>
    /// <param name="rightPrefix">Optional right table prefix/alias</param>
    /// <param name="op">The join operator (defaults to "=")</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder LeftJoin<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> leftPropertyExpression, Expression<Func<TRight, TValue>> rightPropertyExpression, string? leftPrefix = null,
        string? rightPrefix = null, string op = "=")
    {
        fromBuilder.Append(" LEFT JOIN ");
        return BuildJoinClause(leftPropertyExpression, rightPropertyExpression, leftPrefix, rightPrefix, op);
    }

    /// <summary>
    /// Adds a RIGHT JOIN clause to the query.
    /// </summary>
    /// <typeparam name="TLeft">The left table's entity type</typeparam>
    /// <typeparam name="TRight">The right table's entity type</typeparam>
    /// <typeparam name="TValue">The join key's type</typeparam>
    /// <param name="leftPropertyExpression">Expression for the left table's join key</param>
    /// <param name="rightPropertyExpression">Expression for the right table's join key</param>
    /// <param name="leftPrefix">Optional left table prefix/alias</param>
    /// <param name="rightPrefix">Optional right table prefix/alias</param>
    /// <param name="op">The join operator (defaults to "=")</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder RightJoin<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> leftPropertyExpression, Expression<Func<TRight, TValue>> rightPropertyExpression, string? leftPrefix = null,
        string? rightPrefix = null,
        string op = "=")
    {
        fromBuilder.Append(" RIGHT JOIN ");
        return BuildJoinClause(leftPropertyExpression, rightPropertyExpression, leftPrefix, rightPrefix, op);
    }

    /// <summary>
    /// Adds a FULL JOIN clause to the query.
    /// </summary>
    /// <typeparam name="TLeft">The left table's entity type</typeparam>
    /// <typeparam name="TRight">The right table's entity type</typeparam>
    /// <typeparam name="TValue">The join key's type</typeparam>
    /// <param name="leftPropertyExpression">Expression for the left table's join key</param>
    /// <param name="rightPropertyExpression">Expression for the right table's join key</param>
    /// <param name="leftPrefix">Optional left table prefix/alias</param>
    /// <param name="rightPrefix">Optional right table prefix/alias</param>
    /// <param name="op">The join operator (defaults to "=")</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder FullJoin<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> leftPropertyExpression, Expression<Func<TRight, TValue>> rightPropertyExpression, string? leftPrefix = null,
        string? rightPrefix = null,
        string op = "=")
    {
        fromBuilder.Append(" FULL JOIN ");
        return BuildJoinClause(leftPropertyExpression, rightPropertyExpression, leftPrefix, rightPrefix, op);
    }

    /// <summary>
    /// Appends raw SQL to the query.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <param name="afterFrom">Whether to append the raw SQL after the FROM clause (defaults to true)</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder RawSql(string rawSql, bool afterFrom = true)
    {
        if (afterFrom)
        {
            fromBuilder.Append(rawSql);
        }
        else
        {
            selectBuilder.Append(rawSql);
        }
        return this;
    }

    /// <summary>
    /// Builds and returns the complete SQL query string.
    /// </summary>
    /// <returns>The constructed SQL query</returns>
    public string Build()
    {
        return "SELECT " + selectBuilder + fromBuilder;
    }
}