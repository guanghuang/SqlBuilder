// Copyright © 2024 Kvr.SqlBuilder. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using Kvr.SqlBuilder;
using Kvr.SqlBuilder.Convention;
using SqlBuilder.Tests.Models;
using Xunit;

namespace SqlBuilder.Tests;

public class SqlBuilderTests
{
    private static void AssertSqlEqual(string expected, string actual)
    {
        Assert.Equal(expected.ToLower().Replace("\n", "").Replace("\r", "").Replace("\t", ""), 
            actual.ToLower().Replace("\n", "").Replace("\r", "").Replace("\t", ""));
    }

    [Fact]
    public void SelectAll_WithDefaultConvention_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());        
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>()
            .From<Customer>()
            .Build();

        // Assert
        AssertSqlEqual("SELECT kvr0.Id As Id, kvr0.FirstName As FirstName, kvr0.LastName As LastName, kvr0.Email As Email FROM Customer kvr0", sql);
    }

    [Fact]
    public void SelectAll_WithSnakeCase_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>()
            .From<Customer>()
            .Build();

        // Assert
        AssertSqlEqual("SELECT kvr0.id as Id, kvr0.first_name as FirstName, kvr0.last_name as LastName, kvr0.email as Email FROM Customer kvr0", sql);
    }

    [Fact]
    public void SelectAll_WithTableAttribute_UsesSpecifiedTableName()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Order>()
            .From<Order>()
            .Build();

        // Assert
        AssertSqlEqual("SELECT kvr0.OrderId as OrderId, kvr0.CustomerId as CustomerId, kvr0.Amount as Amount, kvr0.OrderDate as OrderDate FROM OrderDetails kvr0", sql);
    }

    [Fact]
    public void Join_WithDefaultConvention_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(out var customerPrefix)
            .SelectAll<Order>(out var orderPrefix)
            .From<Customer>()
            .Join<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, kvr0.Email as Email, " +
            "kvr1.OrderId as OrderId, kvr1.CustomerId as CustomerId, kvr1.Amount as Amount, kvr1.OrderDate as OrderDate " +
            "FROM Customer kvr0 " +
            "JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId",
            sql);
    }

    [Fact]
    public void Where_WithMultipleConditions_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>()
            .From<Customer>()
            .Where<Customer, int>(c => c.Id, "1")
            .And<Customer, string?>(c => c.Email, "'test@example.com'")
            .Or<Customer, string?>(c => c.LastName, "'Smith'")
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, kvr0.Email as Email " +
            "FROM Customer kvr0 " +
            "WHERE kvr0.Id = 1 " +
            "AND kvr0.Email = 'test@example.com' " +
            "OR kvr0.LastName = 'Smith'",
            sql);
    }

    [Fact]
    public void OrderBy_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>()
            .From<Customer>()
            .OrderBy<Customer, string?>(c => c.LastName)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, kvr0.Email as Email " +
            "FROM Customer kvr0 " +
            "ORDER BY kvr0.LastName ASC",
            sql);
    }

    [Fact]
    public void Select_WithSpecificColumns_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .Select<Customer>(new[] { 
                (Expression<Func<Customer, object>>)(c => c.Id),
                c => c.Email 
            })
            .From<Customer>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.Email as Email " +
            "FROM Customer kvr0",
            sql);
    }

    [Fact]
    public void SelectAll_WithExcludeColumns_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(new[] { 
                (Expression<Func<Customer, object>>)(c => c.Email) 
            })
            .From<Customer>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName " +
            "FROM Customer kvr0",
            sql);
    }

    [Fact]
    public void LeftJoin_WithTwoTables_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(out var customerPrefix)
            .SelectAll<Order>(out var orderPrefix)
            .From<Customer>()
            .LeftJoin<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, kvr0.Email as Email, " +
            "kvr1.OrderId as OrderId, kvr1.CustomerId as CustomerId, kvr1.Amount as Amount, kvr1.OrderDate as OrderDate " +
            "FROM Customer kvr0 " +
            "LEFT JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId",
            sql);
    }

    [Fact]
    public void LeftJoin_WithThreeTables_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(out var customerPrefix)
            .SelectAll<Order>(out var orderPrefix)
            .SelectAll<CustomerAddress>(out var addressPrefix)
            .From<Customer>()
            .LeftJoin<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId)
            .LeftJoin<Customer, CustomerAddress, int>(
                customer => customer.Id,
                address => address.CustomerId)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, kvr0.Email as Email, " +
            "kvr1.OrderId as OrderId, kvr1.CustomerId as CustomerId, kvr1.Amount as Amount, kvr1.OrderDate as OrderDate, " +
            "kvr2.AddressId as AddressId, kvr2.CustomerId as CustomerId, kvr2.Street as Street, kvr2.City as City, kvr2.Country as Country, kvr2.PostalCode as PostalCode " +
            "FROM Customer kvr0 " +
            "LEFT JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId " +
            "LEFT JOIN CustomerAddress kvr2 ON kvr0.Id = kvr2.CustomerId",
            sql);
    }

    [Fact]
    public void LeftJoin_WithThreeTables_WithSnakeCase_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(out var customerPrefix)
            .SelectAll<Order>(out var orderPrefix)
            .SelectAll<CustomerAddress>(out var addressPrefix)
            .From<Customer>()
            .LeftJoin<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId)
            .LeftJoin<Customer, CustomerAddress, int>(
                customer => customer.Id,
                address => address.CustomerId)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.id as Id, kvr0.first_name as FirstName, kvr0.last_name as LastName, kvr0.email as Email, " +
            "kvr1.order_id as OrderId, kvr1.customer_id as CustomerId, kvr1.amount as Amount, kvr1.order_date as OrderDate, " +
            "kvr2.address_id as AddressId, kvr2.customer_id as CustomerId, kvr2.street as Street, kvr2.city as City, kvr2.country as Country, kvr2.postal_code as PostalCode " +
            "FROM customer kvr0 " +
            "LEFT JOIN OrderDetails kvr1 ON kvr0.id = kvr1.customer_id " +
            "LEFT JOIN customer_address kvr2 ON kvr0.id = kvr2.customer_id",
            sql);
    }

    [Fact]
    public void LeftJoin_WithThreeTables_WithSpecificColumns_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .Select<Customer>(new[] { 
                (Expression<Func<Customer, object>>)(c => c.Id),
                c => c.FirstName 
            }, out var customerPrefix)
            .Select<Order>(new[] { 
                (Expression<Func<Order, object>>)(o => o.OrderId),
                o => o.Amount 
            }, out var orderPrefix)
            .Select<CustomerAddress>(new[] { 
                (Expression<Func<CustomerAddress, object>>)(a => a.City),
                a => a.Country 
            }, out var addressPrefix)
            .From<Customer>()
            .LeftJoin<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId)
            .LeftJoin<Customer, CustomerAddress, int>(
                customer => customer.Id,
                address => address.CustomerId)
            .Where<Customer, int>(c => c.Id, "1")
            .OrderBy<Order, decimal>(o => o.Amount, false)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, " +
            "kvr1.OrderId as OrderId, kvr1.Amount as Amount, " +
            "kvr2.City as City, kvr2.Country as Country " +
            "FROM Customer kvr0 " +
            "LEFT JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId " +
            "LEFT JOIN CustomerAddress kvr2 ON kvr0.Id = kvr2.CustomerId " +
            "WHERE kvr0.Id = 1 " +
            "ORDER BY kvr1.Amount DESC",
            sql);
    }

    [Fact]
    public void LeftJoin_WithThreeTables_WithExcludeColumns_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>()
            .SelectAll<Order>()
            .SelectAll<CustomerAddress>()
            .From<Customer>()
            .LeftJoin<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId)
            .LeftJoin<Customer, CustomerAddress, int>(
                customer => customer.Id,
                address => address.CustomerId)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, kvr0.Email as Email, " +
            "kvr1.OrderId as OrderId, kvr1.CustomerId as CustomerId, kvr1.Amount as Amount, kvr1.OrderDate as OrderDate, " +
            "kvr2.AddressId as AddressId, kvr2.CustomerId as CustomerId, kvr2.Street as Street, kvr2.City as City, kvr2.Country as Country, kvr2.PostalCode as PostalCode " +
            "FROM Customer kvr0 " +
            "LEFT JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId " +
            "LEFT JOIN CustomerAddress kvr2 ON kvr0.Id = kvr2.CustomerId",
            sql);
    }

    [Fact]
    public void Join_WithOutPrefix_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(out var customerPrefix)
            .SelectAll<Order>(out var orderPrefix)
            .From<Customer>()
            .Join<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId,
                customerPrefix,
                orderPrefix)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, kvr0.Email as Email, " +
            "kvr1.OrderId as OrderId, kvr1.CustomerId as CustomerId, kvr1.Amount as Amount, kvr1.OrderDate as OrderDate " +
            "FROM Customer kvr0 " +
            "JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId",
            sql);
    }

    [Fact]
    public void Join_WithMultipleOutPrefixes_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(out var customerPrefix)
            .SelectAll<Order>(out var orderPrefix)
            .SelectAll<CustomerAddress>(out var addressPrefix)
            .From<Customer>()
            .Join<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId,
                customerPrefix,
                orderPrefix)
            .Join<Customer, CustomerAddress, int>(
                customer => customer.Id,
                address => address.CustomerId,
                customerPrefix,
                addressPrefix)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, kvr0.Email as Email, " +
            "kvr1.OrderId as OrderId, kvr1.CustomerId as CustomerId, kvr1.Amount as Amount, kvr1.OrderDate as OrderDate, " +
            "kvr2.AddressId as AddressId, kvr2.CustomerId as CustomerId, kvr2.Street as Street, kvr2.City as City, kvr2.Country as Country, kvr2.PostalCode as PostalCode " +
            "FROM Customer kvr0 " +
            "JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId " +
            "JOIN CustomerAddress kvr2 ON kvr0.Id = kvr2.CustomerId",
            sql);
    }
 
    [Fact]
    public void Join_WithOutPrefixAndSnakeCase_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(out var customerPrefix)
            .SelectAll<Order>(out var orderPrefix)
            .From<Customer>()
            .Join<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId,
                customerPrefix,
                orderPrefix)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.id as Id, kvr0.first_name as FirstName, kvr0.last_name as LastName, kvr0.email as Email, " +
            "kvr1.order_id as OrderId, kvr1.customer_id as CustomerId, kvr1.amount as Amount, kvr1.order_date as OrderDate " +
            "FROM customer kvr0 " +
            "JOIN OrderDetails kvr1 ON kvr0.id = kvr1.customer_id",
            sql);
    }

    [Fact]
    public void Join_WithOutPrefixAndSpecificColumns_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .Select<Customer>(new[] { 
                (Expression<Func<Customer, object>>)(c => c.Id),
                c => c.FirstName 
            }, out var customerPrefix)
            .Select<Order>(new[] { 
                (Expression<Func<Order, object>>)(o => o.OrderId),
                o => o.Amount 
            }, out var orderPrefix)
            .From<Customer>()
            .Join<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId,
                customerPrefix,
                orderPrefix)
            .Where<Customer, int>(c => c.Id, "1")
            .OrderBy<Order, decimal>(o => o.Amount, false)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, " +
            "kvr1.OrderId as OrderId, kvr1.Amount as Amount " +
            "FROM Customer kvr0 " +
            "JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId " +
            "WHERE kvr0.Id = 1 " +
            "ORDER BY kvr1.Amount DESC",
            sql);
    }

    [Fact]
    public void Join_WithOutPrefixAndExcludeColumns_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(new[] { 
                (Expression<Func<Customer, object>>)(c => c.Email) 
            }, null, out var customerPrefix)
            .SelectAll<Order>(new[] { 
                (Expression<Func<Order, object>>)(o => o.OrderDate) 
            }, null, out var orderPrefix)
            .From<Customer>()
            .Join<Customer, Order, int>(
                customer => customer.Id,
                order => order.CustomerId,
                customerPrefix,
                orderPrefix)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.FirstName as FirstName, kvr0.LastName as LastName, " +
            "kvr1.OrderId as OrderId, kvr1.CustomerId as CustomerId, kvr1.Amount as Amount " +
            "FROM Customer kvr0 " +
            "JOIN OrderDetails kvr1 ON kvr0.Id = kvr1.CustomerId",
            sql);
    }

    [Fact]
    public void SnakeCase_WithCustomerAddress_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<CustomerAddress>()
            .From<CustomerAddress>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.address_id as AddressId, kvr0.customer_id as CustomerId, kvr0.street as Street, kvr0.city as City, kvr0.country as Country, kvr0.postal_code as PostalCode " +
            "FROM customer_address kvr0",
            sql);
    }

    [Fact]
    public void SnakeCase_WithCustomerAddressAndJoin_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Customer>(out var customerPrefix)
            .SelectAll<CustomerAddress>(out var addressPrefix)
            .From<Customer>()
            .LeftJoin<Customer, CustomerAddress, int>(
                customer => customer.Id,
                address => address.CustomerId,
                customerPrefix,
                addressPrefix)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.id as Id, kvr0.first_name as FirstName, kvr0.last_name as LastName, kvr0.email as Email, " +
            "kvr1.address_id as AddressId, kvr1.customer_id as CustomerId, kvr1.street as Street, kvr1.city as City, kvr1.country as Country, kvr1.postal_code as PostalCode " +
            "FROM customer kvr0 " +
            "LEFT JOIN customer_address kvr1 ON kvr0.id = kvr1.customer_id",
            sql);
    }

    [Fact]
    public void SnakeCase_WithCustomerAddressAndSpecificColumns_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .Select<CustomerAddress>(new[] {
                (Expression<Func<CustomerAddress, object>>)(a => a.City),
                a => a.Country,
                a => a.PostalCode
            })
            .From<CustomerAddress>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.city as City, kvr0.country as Country, kvr0.postal_code as PostalCode " +
            "FROM customer_address kvr0",
            sql);
    }

    [Fact]
    public void SnakeCase_WithCustomerAddressAndWhere_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<CustomerAddress>()
            .From<CustomerAddress>()
            .Where<CustomerAddress, string>(a => a.City, "'New York'")
            .And<CustomerAddress, string>(a => a.Country, "'USA'")
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.address_id as AddressId, kvr0.customer_id as CustomerId, kvr0.street as Street, kvr0.city as City, kvr0.country as Country, kvr0.postal_code as PostalCode " +
            "FROM customer_address kvr0 " +
            "WHERE kvr0.city = 'New York' " +
            "AND kvr0.country = 'USA'",
            sql);
    }

    [Fact]
    public void SnakeCase_WithCustomerAddressAndComplexJoin_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .Select<Customer>(new[] {
                (Expression<Func<Customer, object>>)(c => c.Id),
                c => c.FirstName,
                c => c.LastName
            }, out var customerPrefix)
            .Select<CustomerAddress>(new[] {
                (Expression<Func<CustomerAddress, object>>)(a => a.City),
                a => a.Country
            }, out var addressPrefix)
            .From<Customer>()
            .LeftJoin<Customer, CustomerAddress, int>(
                customer => customer.Id,
                address => address.CustomerId,
                customerPrefix,
                addressPrefix)
            .Where<Customer, int>(c => c.Id, "1", prefix: customerPrefix)
            .OrderBy<CustomerAddress, string>(a => a.City, true, addressPrefix)
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.id as Id, kvr0.first_name as FirstName, kvr0.last_name as LastName, " +
            "kvr1.city as City, kvr1.country as Country " +
            "FROM customer kvr0 " +
            "LEFT JOIN customer_address kvr1 ON kvr0.id = kvr1.customer_id " +
            "WHERE kvr0.id = 1 " +
            "ORDER BY kvr1.city ASC",
            sql);
    }

    [Fact]
    public void SelectFromWhere_WithIntegerKey_GeneratesCorrectSql()
    {
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        // Arrange
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFromWhere<Customer, int>(c => c.Id, "1")
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.id as Id, kvr0.first_name as FirstName, kvr0.last_name as LastName, kvr0.email as Email " +
            "FROM customer kvr0 " +
            "WHERE kvr0.id = 1",
            sql);
    }

    [Fact]
    public void SelectFromWhere_WithStringKey_GeneratesCorrectSql()
    {
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        // Arrange
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFromWhere<Customer, string>(c => c.Email, "'test@example.com'")
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.id as Id, kvr0.first_name as FirstName, kvr0.last_name as LastName, kvr0.email as Email " +
            "FROM customer kvr0 " +
            "WHERE kvr0.email = 'test@example.com'",
            sql);
    }

    [Fact]
    public void SelectFromWhere_WithCustomerAddress_GeneratesCorrectSql()
    {
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        // Arrange
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFromWhere<CustomerAddress, int>(a => a.CustomerId, "1")
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.address_id as AddressId, kvr0.customer_id as CustomerId, kvr0.street as Street, kvr0.city as City, kvr0.country as Country, kvr0.postal_code as PostalCode " +
            "FROM customer_address kvr0 " +
            "WHERE kvr0.customer_id = 1",
            sql);
    }

    [Fact]
    public void SelectFromWhere_WithTableAttribute_GeneratesCorrectSql()
    {
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        // Arrange
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFromWhere<Order, int>(o => o.OrderId, "1")
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.order_id as OrderId, kvr0.customer_id as CustomerId, kvr0.amount as Amount, kvr0.order_date as OrderDate " +
            "FROM OrderDetails kvr0 " +
            "WHERE kvr0.order_id = 1",
            sql);
    }

    [Fact]
    public void SelectFromWhere_WithSnakeCase_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFromWhere<CustomerAddress, string>(a => a.PostalCode, "'12345'")
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.address_id as AddressId, kvr0.customer_id as CustomerId, kvr0.street as Street, kvr0.city as City, kvr0.country as Country, kvr0.postal_code as PostalCode " +
            "FROM customer_address kvr0 " +
            "WHERE kvr0.postal_code = '12345'",
            sql);
    }

    [Fact]
    public void SelectFrom_WithCustomer_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFrom<Customer>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.id as Id, kvr0.first_name as FirstName, kvr0.last_name as LastName, kvr0.email as Email " +
            "FROM customer kvr0",
            sql);
    }

    [Fact]
    public void SelectFrom_WithCustomerAddress_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFrom<CustomerAddress>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.address_id as AddressId, kvr0.customer_id as CustomerId, kvr0.street as Street, kvr0.city as City, kvr0.country as Country, kvr0.postal_code as PostalCode " +
            "FROM customer_address kvr0",
            sql);
    }

    [Fact]
    public void SelectFrom_WithTableAttribute_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFrom<Order>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.order_id as OrderId, kvr0.customer_id as CustomerId, kvr0.amount as Amount, kvr0.order_date as OrderDate " +
            "FROM OrderDetails kvr0",
            sql);
    }

    [Fact]
    public void SelectFrom_WithSnakeCase_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectFrom<CustomerAddress>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.address_id as AddressId, kvr0.customer_id as CustomerId, kvr0.street as Street, kvr0.city as City, kvr0.country as Country, kvr0.postal_code as PostalCode " +
            "FROM customer_address kvr0",
            sql);
    }

    [Fact]
    public void SelectAll_WithNotMappedAttribute_ExcludesNotMappedProperties()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<EntityWithNotMapped>()
            .From<EntityWithNotMapped>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.Name as Name " +
            "FROM EntityWithNotMapped kvr0",
            sql);
    }

    [Fact]
    public void SelectAll_WithNotMappedAndSnakeCase_ExcludesNotMappedProperties()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(SnakeCaseNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<EntityWithNotMapped>()
            .From<EntityWithNotMapped>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.id as Id, kvr0.name as Name " +
            "FROM entity_with_not_mapped kvr0",
            sql);
    }

    [Fact]
    public void SelectAll_WithNotMappedAndTableAttribute_ExcludesNotMappedProperties()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<EntityWithNotMappedAndTable>()
            .From<EntityWithNotMappedAndTable>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.Id as Id, kvr0.Name as Name " +
            "FROM CustomTableName kvr0",
            sql);
    }

    [Fact]
    public void SelectAll_WithNotMappedAndColumnAttribute_ExcludesNotMappedProperties()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<EntityWithNotMappedAndColumn>()
            .From<EntityWithNotMappedAndColumn>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.custom_id as Id, kvr0.custom_name as Name " +
            "FROM EntityWithNotMappedAndColumn kvr0",
            sql);
    }

    [Fact]
    public void SelectAll_WithFromBeginTrue_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Order>()
            .SelectAll<Customer>(null, null, fromBegin: true)
            .From<Customer>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr1.Id as Id, kvr1.FirstName as FirstName, kvr1.LastName as LastName, kvr1.Email as Email, " +
            "kvr0.OrderId as OrderId, kvr0.CustomerId as CustomerId, kvr0.Amount as Amount, kvr0.OrderDate as OrderDate " +
            "FROM Customer kvr1",
            sql);
    }

    [Fact]
    public void SelectAll_WithFromBeginFalse_GeneratesCorrectSql()
    {
        // Arrange
        Kvr.SqlBuilder.SqlBuilder.UseGlobalNameConvention(DefaultNameConvention.Create());
        var builder = new Kvr.SqlBuilder.SqlBuilder();

        // Act
        var sql = builder
            .SelectAll<Order>()
            .SelectAll<Customer>(null, null, fromBegin: false)
            .From<Customer>()
            .Build();

        // Assert
        AssertSqlEqual(
            "SELECT kvr0.OrderId as OrderId, kvr0.CustomerId as CustomerId, kvr0.Amount as Amount, kvr0.OrderDate as OrderDate, " +
            "kvr1.Id as Id, kvr1.FirstName as FirstName, kvr1.LastName as LastName, kvr1.Email as Email " +
            "FROM Customer kvr1",
            sql);
    }

    private class EntityWithNotMapped
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        [NotMapped]
        public string Description { get; set; } = string.Empty;
        [NotMapped]
        public DateTime CreatedDate { get; set; }
    }

    [Table("CustomTableName")]
    private class EntityWithNotMappedAndTable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        [NotMapped]
        public string Description { get; set; } = string.Empty;
    }

    private class EntityWithNotMappedAndColumn
    {
        [Column("custom_id")]
        public int Id { get; set; }
        [Column("custom_name")]
        public string Name { get; set; } = string.Empty;
        [NotMapped]
        public string Description { get; set; } = string.Empty;
    }
} 