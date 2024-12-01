# SqlBuilder

Copyright © 2024 Kvr.SqlBuilder. All rights reserved.

A lightweight, fluent SQL query builder for .NET that provides type-safe SQL query construction with support for table aliases, column selection, and complex joins. With Table and Column attributes annotations support. RawSql methods support for appending raw sql script to builder. Could be used with [Dapper](https://github.com/DapperLib/Dapper), [NPoco](https://github.com/schotime/NPoco), etc.

## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Quick Start Guide](#quick-start-guide)
- [Usage](#usage)
    - [Table Names](#table-names)
    - [Column Selection](#column-selection)
    - [Joins](#joins)
    - [Where Clauses](#where-clauses)
    - [Order By](#order-by)
    - [Naming Conventions](#naming-conventions)    
    - [Raw Sql](#rawsql)
- [Best Practices](#best-practices)
- [Limitations](#limitations)
- [Supported Frameworks](#supported-frameworks)
- [Version History](#version-history)
- [License](#license)
- [Contributing](#contributing)
- [Dependencies](#dependencies)
- [Support](#support)
- [Build Status](#build-status)

## Features
- Fluent API for building SQL queries
- Type-safe property and table selection
- Support for table aliases and prefixes
- Automatic handling of SQL Server and standard SQL syntax
- Support for complex JOIN operations
- Column selection with aliasing
- WHERE clause construction with multiple conditions
- ORDER BY clause support
- Minimal boilerplate code
- TableAttribute and ColumnAttribute annotations support

## Installation

You can install the package via NuGet Package Manager:

```bash
dotnet add package Kvr.SqlBuilder
```

## Quick Start Guide

Here's a quick example of how to use SqlBuilder to construct SQL queries:

```csharp
using Kvr.SqlBuilder;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
}

// Basic query
var query = new SqlBuilder()
    .SelectAll<Customer>()
    .From<Customer>()
    .Build();
// Result: SELECT [Id], [Name], [Email] FROM [Customers]

// Join query with conditions
var joinQuery = new SqlBuilder()
    .SelectAll<Customer>(out var customerPrefix)
    .SelectAll<Order>(out var orderPrefix)
    .From<Customer>()
    .Join<Customer, Order, int>(
        customer => customer.Id,
        order => order.CustomerId)
    .Where<Customer>(c => c.Id, 1)
    .OrderBy<Order>(o => o.Amount, false)
    .Build();
// Result: SELECT kvr0.[Id], kvr0.[Name], kvr0.[Email], kvr1.[OrderId], kvr1.[CustomerId], kvr1.[Amount] 
//        FROM [Customers] kvr0 
//        JOIN [Orders] kvr1 ON kvr0.[Id] = kvr1.[CustomerId] 
//        WHERE kvr0.[Id] = 1 
//        ORDER BY kvr1.[Amount] DESC
```

## Usage

### Table Names

SqlBuilder supports both singular and plural table names, with customization options:

```csharp
// Global setting for plural table names
SqlBuilder.UseGlobalPluralTableNames();

// Instance-specific setting
var builder = new SqlBuilder()
    .UsePluralTableNames()
    .SelectAll<Customer>()
    .From<Customer>();

// Custom table name mapping
SqlBuilder.MapGlobalTable<Customer>("tbl_customers");
// or instance-specific
builder.MapTable<Customer>("tbl_customers");
```

### Column Selection

Multiple ways to select columns:

```csharp
// Select all columns
builder.SelectAll<Customer>();

// Return c.Id as first column, it is important for Dapper's splitOn parameter
builder.SelectAll<Customer>(c => c.Id);

// Select specific columns
builder.Select<Customer>(c => new[] { c.Id, c.Name });

// Select with alias
builder.Select<Customer>(c => c.Name, "CustomerName");

// Exclude specific columns
builder.SelectAll<Customer>(excludeColumns: new[] { c => c.Email });

// Exclude specific column Email. Return c.Id as first column, it is important for Dapper's splitOn parameter
builder.SelectAll<Customer>(excludeColumns: new[] { c => c.Email }, c => c.Id);

```

### Joins

Support for various JOIN types:

```csharp
// INNER JOIN
builder.Join<Customer, Order, int>(
    customer => customer.Id,
    order => order.CustomerId);

// LEFT JOIN
builder.LeftJoin<Customer, Order, int>(
    customer => customer.Id,
    order => order.CustomerId);

// RIGHT JOIN
builder.RightJoin<Customer, Order, int>(
    customer => customer.Id,
    order => order.CustomerId);

// FULL JOIN
builder.FullJoin<Customer, Order, int>(
    customer => customer.Id,
    order => order.CustomerId);
```

### Where Clauses

Building WHERE conditions:

```csharp
builder
    .Where<Customer>(c => c.Id, "1")
    .And<Customer>(c => c.Name, "'John'")
    .Or<Customer>(c => c.Email, "'john@example.com'");
```

### NameConvention

SqlBuilder supports custom naming conventions which implements INameConvention interface:

```csharp
SqlBuilder.UseGlobalNameConvention(new CustomNameConvention());
```
There are two built-in naming conventions: DefaultNameConvention and SnakeCaseNameConvention. Both of them have `UseSqlServer` property to specify whether to use SQL Server specific syntax to escape identifiers with square brackets and `UsePlural` property to specify whether to use plural table names.

#### Table Name determination priority:
1. Custom table name mapping (Highest priority)
  * using MapGlobalTable or MapTable method to map table names which could not inherit from CustomNameConvention
  ```csharp
  // Global mapping
  SqlBuilder.MapGlobalTable<Customer>("tbl_customers");
  // or instance-specific mapping
  this.sqlBuilder.MapTable<Customer>("tbl_customers");
  ```
2. `TableAttribute` on the entity class to specify the table name
  ```csharp
  [Table("tbl_customers")]
  public class Customer
  ```
3. `ToTableName` method in CustomNameConvention to convert the table name
  ```csharp
  protected override string ToTableName(string typeName)
  ```

#### Column Name determination priority:
1. Custom column name sepecified in `Select` method (Highest priority)
    ```csharp   
    this.sqlBuilder.Select<Customer>("customer_id", "Name");
    ```
2. `ColumnAttribute` on the property to specify the column name
    ```csharp
    [Column("customer_id")]
    public int Id { get; set; }
    ```
3. `ToColumnName` method in CustomNameConvention to convert the column name
    ```csharp
    protected override string ToColumnName(string propertyName)
    ```

### RawSql

SqlBuilder supports RawSql methods to build WHERE, AND, OR, ORDER BY clauses, also `RawSql` methods to append raw sql script to builder:

```csharp
builder.SelectAll<Customer>().RawSql(", count(*) as TotalCount") // append total count of customers to the result
    .Where<Customer>(c => c.Email, "@customerEmail") // using parameter
    .Or<Customer>("@customerEmail is null") // using raw sql string for parameter condition
    .OrderBy<Customer>("Id desc")
    .RawSql(" having count(*) > 0") // using raw sql string for having clause
    .Build();
// Result: SELECT [Id], [Name], [Email], count(*) as TotalCount FROM [Customers] WHERE [Email] = @customerEmail OR @customerEmail is null ORDER BY Dd desc having count(*) > 0
``` 

## Best Practices
- Use strongly-typed expressions when possible
- Leverage table aliases for complex queries
- Keep SQL Server compatibility in mind
- Use appropriate column selection instead of SELECT *
- Consider query performance when building complex JOINs

## Limitations
1. Limited support for complex subqueries (could use RawSql methods for this)
2. No direct support for GROUP BY and HAVING clauses
3. No NamingConvention support for table and column (e.g. SnakeCase, PascalCase for table and column names)

## Supported Frameworks
- .NET Standard 2.0+
- .NET 5.0+
- .NET 6.0+
- .NET 7.0+

## Version History
- 1.2.1
    - Add prefix for missing Where, Join, Select, From, OrderBy methods.
   
- 1.2.0
    - Add NameConvention support

- 1.1.0
    - Add RawSql methods (Where, And, Or, OrderBy) and AppendRawSql method
    - Change parameter type from LambdaExpression to Expression<Func<T, object>> for strongly-typed expressions

- 1.0.0
    - Initial release
    - Basic SQL query building
    - Support for JOINs, WHERE clauses, and ORDER BY
    - Table name customization
    - SQL Server compatibility

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Dependencies

`System.ComponentModel.Annotations` for Table and Column attributes

## Support

If you encounter any issues or have questions, please file an issue on the GitHub repository.

## Build Status
![Build and Test](https://github.com/guanghuang/SqlBuilder/actions/workflows/build.yml/badge.svg)
![Publish to NuGet](https://github.com/guanghuang/SqlBuilder/actions/workflows/publish.yml/badge.svg)
