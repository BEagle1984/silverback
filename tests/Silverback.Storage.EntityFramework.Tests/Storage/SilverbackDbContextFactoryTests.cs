// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Storage;

public sealed class SilverbackDbContextFactoryTests : IDisposable
{
    private const string FactoryConnectionString = "Data Source=factory;Mode=Memory;Cache=Shared";

    private readonly SqliteConnection _sqliteConnection;

    private readonly IServiceProvider _serviceProvider;

    public SilverbackDbContextFactoryTests()
    {
        _sqliteConnection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection.Open();

        _serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddDbContextFactory<TestDbContext>(options => options.UseSqlite(FactoryConnectionString)));
    }

    [Fact]
    public void CreateDbContext_ShouldUseDbContextFactory_WhenContextIsNull()
    {
        using TestDbContext dbContext = SilverbackDbContextFactory.CreateDbContext<TestDbContext>(_serviceProvider, null);

        dbContext.CreatedWithConnection.ShouldBeFalse();
        dbContext.Database.GetConnectionString().ShouldBe(FactoryConnectionString);
    }

    [Fact]
    public void CreateDbContext_ShouldUseDbContextFactory_WhenNoTransactionIsEnlisted()
    {
        SilverbackContext context = new(_serviceProvider);

        using TestDbContext dbContext = SilverbackDbContextFactory.CreateDbContext<TestDbContext>(_serviceProvider, context);

        dbContext.CreatedWithConnection.ShouldBeFalse();
        dbContext.Database.GetConnectionString().ShouldBe(FactoryConnectionString);
    }

    [Fact]
    public void CreateDbContext_ShouldUseDbContextFactory_WhenEnlistedTransactionHasNoConnection()
    {
        SilverbackContext context = new(_serviceProvider);
        SqliteTransaction transaction = _sqliteConnection.BeginTransaction();
        using IStorageTransaction storageTransaction = context.EnlistDbTransaction(transaction);
        transaction.Commit();
        transaction.Connection.ShouldBeNull();

        using TestDbContext dbContext = SilverbackDbContextFactory.CreateDbContext<TestDbContext>(_serviceProvider, context);

        dbContext.CreatedWithConnection.ShouldBeFalse();
        dbContext.Database.GetConnectionString().ShouldBe(FactoryConnectionString);
    }

    [Fact]
    public void CreateDbContext_ShouldUseTransactionConnection_WhenTransactionIsEnlisted()
    {
        SilverbackContext context = new(_serviceProvider);
        using IStorageTransaction storageTransaction = context.EnlistDbTransaction(_sqliteConnection.BeginTransaction());

        using TestDbContext dbContext1 = SilverbackDbContextFactory.CreateDbContext<TestDbContext>(_serviceProvider, context);
        using TestDbContext dbContext2 = SilverbackDbContextFactory.CreateDbContext<TestDbContext>(_serviceProvider, context);

        dbContext1.CreatedWithConnection.ShouldBeTrue();
        dbContext1.Database.GetDbConnection().ShouldBeSameAs(_sqliteConnection);
        dbContext2.CreatedWithConnection.ShouldBeTrue();
        dbContext2.Database.GetDbConnection().ShouldBeSameAs(_sqliteConnection);
        dbContext2.ShouldNotBeSameAs(dbContext1);
    }

    public void Dispose() => _sqliteConnection.Dispose();

    private sealed class TestDbContext : DbContext
    {
        [ActivatorUtilitiesConstructor]
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public TestDbContext(SqliteConnection connection)
            : base(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options)
        {
            CreatedWithConnection = true;
        }

        public bool CreatedWithConnection { get; }
    }
}
