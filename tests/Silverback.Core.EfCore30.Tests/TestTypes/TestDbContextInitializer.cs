// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Silverback.Messaging.Publishing;

// ReSharper disable once CheckNamespace
namespace Silverback.Tests.Core.EFCore30.TestTypes
{
    public sealed class TestDbContextInitializer : IDisposable
    {
        private SqliteConnection? _connection;

        public TestDbContext GetTestDbContext()
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
                _connection.Open();
            }

            var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection.ConnectionString)
                .Options;

            var dbContext = new TestDbContext(dbOptions, Substitute.For<IPublisher>());

            dbContext.Database.EnsureCreated();

            return dbContext;
        }

        public void Dispose()
        {
            _connection?.SafeClose();
            _connection?.Dispose();
        }
    }
}
