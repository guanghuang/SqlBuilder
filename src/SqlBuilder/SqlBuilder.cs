using System.Linq.Expressions;
using System.Text;
using kvr.SqlBuilder;

namespace Kvr.SqlBuilder;

/// <summary>
/// A fluent SQL query builder that provides type-safe methods for constructing SQL queries.
/// Supports table name pluralization, SQL Server-specific syntax, and custom table mappings.
/// </summary>
public class SqlBuilder
{
    /// <summary>Global setting for pluralizing table names across all SqlBuilder instances</summary>
    private static bool _globalPluralTableNames;
    /// <summary>Global setting for SQL Server syntax across all SqlBuilder instances</summary>
    private static bool _globalIsSqlServer;
    /// <summary>Global mapping of types to custom table names</summary>
    private static readonly Dictionary<Type, string> GlobalExtraTableMapping = new();
    /// <summary>Prefix used for generating table aliases</summary>
    private static readonly string TableAliasPrefix = "kvr";
    /// <summary>Counter for generating unique table aliases</summary>
    private int tableAliasIndex;
    /// <summary>Instance-specific setting for table name pluralization</summary>
    private bool pluralTableNames;
    /// <summary>Tracks if a SELECT clause has been started</summary>
    private bool hasSelect;
    /// <summary>Instance-specific setting for SQL Server syntax</summary>
    private bool isSqlServer;
    /// <summary>Instance-specific mapping of types to custom table names</summary>
    private readonly Dictionary<Type, string> extraTableMapping;
    /// <summary>Maps types to their table prefixes/aliases</summary>
    private readonly Dictionary<Type, string> tablePrefixes = new();
    /// <summary>StringBuilder instance for constructing the SQL query</summary>
    private StringBuilder sqlBuilder = new();

    /// <summary>
    /// Initializes a new instance of SqlBuilder with default settings from global configuration.
    /// </summary>
    public SqlBuilder()
    {
        this.pluralTableNames = _globalPluralTableNames;
        this.isSqlServer = _globalIsSqlServer;
        this.extraTableMapping = new Dictionary<Type, string>(GlobalExtraTableMapping);
    }

    /// <summary>
    /// Sets the global setting for table name pluralization.
    /// </summary>
    /// <param name="pluralTableNames">Whether to pluralize table names by default</param>
    public static void UseGlobalPluralTableNames(bool pluralTableNames = true)
    {
        _globalPluralTableNames = pluralTableNames;
    }

    /// <summary>
    /// Sets instance-specific table name pluralization.
    /// </summary>
    /// <param name="pluralTableNames">Whether to pluralize table names</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder UsePluralTableNames(bool pluralTableNames = true)
    {
        this.pluralTableNames = pluralTableNames;
        return this;
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
    /// Sets the global setting for SQL Server syntax.
    /// </summary>
    /// <param name="isSqlServer">Whether to use SQL Server syntax by default</param>
    public static void UseGlobalSqlServer(bool isSqlServer = true)
    {
        _globalIsSqlServer = isSqlServer;
    }

    /// <summary>
    /// Sets instance-specific SQL Server syntax setting.
    /// </summary>
    /// <param name="isSqlServer">Whether to use SQL Server syntax</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder UseSqlServer(bool isSqlServer = true)
    {
        this.isSqlServer = isSqlServer;
        return this;
    }

    /// <summary>
    /// Appends a SELECT clause or column to the query.
    /// </summary>
    /// <param name="sql">The SQL fragment to append</param>
    private void AppendSelect(string sql)
    {
        if (hasSelect)
        {
            sqlBuilder.Append(", ");
        }
        else
        {
            hasSelect = true;
            sqlBuilder.Append("SELECT ");
        }

        sqlBuilder.Append(sql).AppendLine();
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
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="excludeColumns">Columns to exclude from the selection</param>
    /// <param name="firstColumn">Column to place first in the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>[]? excludeColumns, Expression<Func<T, object>>? firstColumn, string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        AppendSelect(Utils.GenerateSelectAllColumns(typeof(T), isSqlServer, excludeColumns, firstColumn, prefix));
        return this;
    }

    /// <summary>
    /// Adds a SELECT * clause for a type, with options to exclude specific columns.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="excludeColumns">Columns to exclude from the selection</param>
    /// <param name="firstColumn">Column to place first in the selection</param>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>[]? excludeColumns, Expression<Func<T, object>>? firstColumn, out string prefix)
    {
        prefix = GetTablePrefix(typeof(T), true);
        AppendSelect(Utils.GenerateSelectAllColumns(typeof(T), isSqlServer, excludeColumns, firstColumn, prefix));
        return this;
    }


    /// <summary>
    /// Adds a SELECT * clause for a type, excluding specific columns.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="excludeColumns">Columns to exclude from the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>[]? excludeColumns, string? prefix = null)
    {
        return SelectAll<T>(excludeColumns, null, prefix);
    }
    
    /// <summary>
    /// Adds a SELECT * clause for a type, with options to specify a first column.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="firstColumn">Column to place first in the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>? firstColumn, out string prefix)
    {
        return SelectAll<T>(null, firstColumn, out prefix);
    }

    /// <summary>
    /// Adds a SELECT * clause for a type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(out string prefix)
    {
        return SelectAll<T>(null, null, out prefix);
    }

    /// <summary>
    /// Adds a SELECT * clause for a type, with options to specify a first column.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="firstColumn">Column to place first in the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder SelectAll<T>(Expression<Func<T, object>>? firstColumn, string? prefix = null)
    {
        return SelectAll<T>(null, firstColumn, prefix);
    }

    /// <summary>
    /// Adds a SELECT * clause for a type.
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
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="includeColumns">Columns to include in the selection</param>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(Expression<Func<T, object>>[] includeColumns, out string prefix)
    {
        prefix = GetTablePrefix(typeof(T), true);
        AppendSelect(Utils.GenerateSelectColumns(isSqlServer, includeColumns, prefix));
        return this;
    }

    /// <summary>
    /// Adds a SELECT clause for specific columns of a type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="includeColumns">Columns to include in the selection</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(Expression<Func<T, object>>[] includeColumns, string? prefix = null)
    {
        prefix ??= GetTablePrefix(typeof(T), false);
        AppendSelect(Utils.GenerateSelectColumns(isSqlServer, includeColumns, prefix));
        return this;
    }

    /// <summary>
    /// Adds a SELECT clause for no columns of a type.
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
    /// Adds a SELECT clause for a single column of a type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="column">The column to select</param>
    /// <param name="alias">Optional column alias</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
        public SqlBuilder Select<T>(Expression<Func<T, object>> column, string? alias = null, string? prefix = null)
    {
        return Select<T>(Utils.GetColumnName(column), alias, prefix);
    }

    /// <summary>
    /// Adds a SELECT clause for a single column of a type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="column">The column to select</param>
    /// <param name="alias">Optional column alias</param>
    /// <param name="prefix">Output parameter for the generated table prefix</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Select<T>(Expression<Func<T, object>> column, string? alias, out string prefix)
    {
        return Select<T>(Utils.GetColumnName(column), alias, out prefix);
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
        AppendSelect(Utils.EncodeColumn(column, isSqlServer, prefix, alias, true));
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
        AppendSelect(Utils.EncodeColumn(column, isSqlServer, prefix, alias, true));
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
        sqlBuilder.Append(" FROM ");
        sqlBuilder.Append(Utils.EncodeTable(
            Utils.GetTableName(typeof(T), pluralTableNames, extraTableMapping.ContainsKey(typeof(T)) ? extraTableMapping[typeof(T)] : null),
            isSqlServer, prefix ?? GetTablePrefix(typeof(T), false))).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a WHERE clause to the query.   
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TResult">The column type</typeparam>
    /// <param name="expression">The column expression</param>
    /// <param name="value">The value to compare against</param>
    /// <param name="op">The comparison operator (defaults to "=")</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Where<T, TResult>(Expression<Func<T, TResult>> expression, string value, string op = "=", string? prefix = null)
    {
        sqlBuilder.Append(" WHERE ");
        sqlBuilder.Append(Utils.EncodeColumn(Utils.GetColumnName(expression), isSqlServer, prefix, null, false));
        sqlBuilder.Append(" ").Append(op).Append(" ").Append(value).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds a WHERE clause to the query.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Where(string rawSql)
    {
        sqlBuilder.Append(" WHERE ").Append(rawSql).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an AND condition to the WHERE clause.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TResult">The column type</typeparam>
    /// <param name="expression">The column expression</param>
    /// <param name="value">The value to compare against</param>
    /// <param name="op">The comparison operator (defaults to "=")</param>
    /// <param name="prefix">Optional table prefix/alias</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder And<T, TResult>(Expression<Func<T, TResult>> expression, string value, string op = "=", string? prefix = null)
    {
        sqlBuilder.Append(" AND ");
        sqlBuilder.Append(Utils.EncodeColumn(Utils.GetColumnName(expression), isSqlServer, prefix, null, false));
        sqlBuilder.Append(" ").Append(op).Append(" ").Append(value).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an AND condition to the WHERE clause.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder And(string rawSql)
    {
        sqlBuilder.Append(" AND ").Append(rawSql).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an OR condition to the WHERE clause.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="expression"></param>
    /// <param name="value"></param>
    /// <param name="op"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public SqlBuilder Or<T, TResult>(Expression<Func<T, TResult>> expression, string value, string op = "=", string? prefix = null)
    {
        sqlBuilder.Append(" OR ");
        sqlBuilder.Append(Utils.EncodeColumn(Utils.GetColumnName(expression), isSqlServer, prefix, null, false));
        sqlBuilder.Append(" ").Append(op).Append(" ").Append(value).AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an OR condition to the WHERE clause.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Or(string rawSql)
    {
        sqlBuilder.Append(" OR ").Append(rawSql).AppendLine();
        return this;
    }


    /// <summary>
    /// Adds an ORDER BY clause to the query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="expression"></param>
    /// <param name="ascOrder"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public SqlBuilder OrderBy<T, TResult>(Expression<Func<T, TResult>> expression, bool ascOrder = true, string? prefix = null)
    {
        sqlBuilder.Append(" ORDER BY ");
        sqlBuilder.Append(Utils.EncodeColumn(Utils.GetColumnName(expression), isSqlServer, prefix, null, false));
        sqlBuilder.Append(" ").Append(ascOrder ? "ASC" : "DESC").AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an ORDER BY clause to the query.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder OrderBy(string rawSql, bool ascOrder = true)
    {
        sqlBuilder.Append(" ORDER BY ").Append(rawSql).Append(" ").Append(ascOrder ? "ASC" : "DESC").AppendLine();
        return this;
    }

    /// <summary>
    /// Helper method to build JOIN clauses.
    /// </summary>
    /// <typeparam name="TLeft">The left table's entity type</typeparam>
    /// <typeparam name="TRight">The right table's entity type</typeparam>
    /// <typeparam name="TValue">The join key's type</typeparam>
    /// <param name="left">Expression for the left table's join key</param>
    /// <param name="right">Expression for the right table's join key</param>
    /// <param name="leftPrefix">Optional left table prefix/alias</param>
    /// <param name="rightPrefix">Optional right table prefix/alias</param>
    /// <param name="op">The join operator (defaults to "=")</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    private SqlBuilder BuildJoinClause<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> left, Expression<Func<TRight, TValue>> right, string? leftPrefix,
        string? rightPrefix,
        string op)
    {
        sqlBuilder.Append(Utils.EncodeTable(
            Utils.GetTableName(typeof(TRight), pluralTableNames, extraTableMapping.ContainsKey(typeof(TRight)) ? extraTableMapping[typeof(TRight)] : null),
            isSqlServer, rightPrefix ?? GetTablePrefix(typeof(TRight), false)));
        sqlBuilder.Append(" ON ");
        sqlBuilder.Append(Utils.EncodeColumn(Utils.GetColumnName(left), isSqlServer, leftPrefix ?? GetTablePrefix(typeof(TLeft), false), null, false));
        sqlBuilder.Append(" ").Append(op).Append(" ")
            .Append(Utils.EncodeColumn(Utils.GetColumnName(right), isSqlServer, rightPrefix ?? GetTablePrefix(typeof(TRight), false), null, false))
            .AppendLine();
        return this;
    }

    /// <summary>
    /// Adds an INNER JOIN clause to the query.
    /// </summary>
    /// <typeparam name="TLeft">The left table's entity type</typeparam>
    /// <typeparam name="TRight">The right table's entity type</typeparam>
    /// <typeparam name="TValue">The join key's type</typeparam>
    /// <param name="left">Expression for the left table's join key</param>
    /// <param name="right">Expression for the right table's join key</param>
    /// <param name="leftPrefix">Optional left table prefix/alias</param>
    /// <param name="rightPrefix">Optional right table prefix/alias</param>
    /// <param name="op">The join operator (defaults to "=")</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder Join<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> left, Expression<Func<TRight, TValue>> right, string? leftPrefix = null,
        string? rightPrefix = null,
        string op = "=")
    {
        sqlBuilder.Append(" JOIN ");
        return BuildJoinClause(left, right, leftPrefix, rightPrefix, op);
    }

    /// <summary>
    /// Adds a LEFT JOIN clause to the query.
    /// </summary>
    public SqlBuilder LeftJoin<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> left, Expression<Func<TRight, TValue>> right, string? leftPrefix = null,
        string? rightPrefix = null, string op = "=")
    {
        sqlBuilder.Append(" LEFT JOIN ");
        return BuildJoinClause(left, right, leftPrefix, rightPrefix, op);
    }

    /// <summary>
    /// Adds a RIGHT JOIN clause to the query.
    /// </summary>
    public SqlBuilder RightJoin<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> left, Expression<Func<TRight, TValue>> right, string? leftPrefix = null,
        string? rightPrefix = null,
        string op = "=")
    {
        sqlBuilder.Append(" RIGHT JOIN ");
        return BuildJoinClause(left, right, leftPrefix, rightPrefix, op);
    }

    /// <summary>
    /// Adds a FULL JOIN clause to the query.
    /// </summary>
    public SqlBuilder FullJoin<TLeft, TRight, TValue>(Expression<Func<TLeft, TValue>> left, Expression<Func<TRight, TValue>> right, string? leftPrefix = null,
        string? rightPrefix = null,
        string op = "=")
    {
        sqlBuilder.Append(" FULL JOIN ");
        return BuildJoinClause(left, right, leftPrefix, rightPrefix, op);
    }

    /// <summary>
    /// Appends raw SQL to the query.
    /// </summary>
    /// <param name="rawSql">The raw SQL string</param>
    /// <returns>The current SqlBuilder instance for method chaining</returns>
    public SqlBuilder AppendRawSql(string rawSql)
    {
        sqlBuilder.Append(rawSql);
        return this;
    }

    /// <summary>
    /// Builds and returns the complete SQL query string.
    /// </summary>
    /// <returns>The constructed SQL query</returns>
    public string Build()
    {
        return sqlBuilder.ToString();
    }
}