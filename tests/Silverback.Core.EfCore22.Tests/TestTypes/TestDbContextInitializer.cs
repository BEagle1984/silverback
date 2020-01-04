﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Core.EFCore22.TestTypes
{
    public class TestDbContextInitializer : IDisposable
    {
        private SqliteConnection _connection;

        public TestDbContext GetTestDbContext()
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();
            }

            var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;

            var dbContext = new TestDbContext(dbOptions, Substitute.For<IPublisher>());

            dbContext.Database.EnsureCreated();

            return dbContext;
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}