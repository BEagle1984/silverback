// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Silverback.Database;
using Silverback.Tests.Core.EFCore30.TestTypes;
using Silverback.Tests.Core.EFCore30.TestTypes.Model;
using Xunit;

namespace Silverback.Tests.Core.EFCore30.Database
{
    public sealed class EfCoreDbSetTests : IAsyncDisposable
    {
        private readonly TestDbContextInitializer _dbInitializer;
        private readonly TestDbContext _dbContext;
        private readonly EfCoreDbContext<TestDbContext> _efCoreDbContext;

        public EfCoreDbSetTests()
        {
            _dbInitializer = new TestDbContextInitializer();
            _dbContext = _dbInitializer.GetTestDbContext();
            _efCoreDbContext = new EfCoreDbContext<TestDbContext>(_dbContext);
        }

        [Fact]
        public void Add_SomeEntity_EntityIsAdded()
        {
            _efCoreDbContext.GetDbSet<Person>().Add(new Person());

            _dbContext.Persons.Local.Count.Should().Be(1);
            _dbContext.Entry(_dbContext.Persons.Local.First()).State.Should().Be(EntityState.Added);
        }

        [Fact]
        public void Remove_ExistingEntity_EntityIsRemoved()
        {
            _dbContext.Persons.Add(new Person());
            _dbContext.SaveChanges();

            _efCoreDbContext.GetDbSet<Person>().Remove(_dbContext.Persons.First());

            _dbContext.Entry(_dbContext.Persons.First()).State.Should().Be(EntityState.Deleted);
        }

        [Fact]
        public void RemoveRange_ExistingEntities_EntitiesAreRemoved()
        {
            _dbContext.Persons.Add(new Person());
            _dbContext.Persons.Add(new Person());
            _dbContext.SaveChanges();

            _efCoreDbContext.GetDbSet<Person>().RemoveRange(_dbContext.Persons.ToList());

            _dbContext.Entry(_dbContext.Persons.First()).State.Should().Be(EntityState.Deleted);
            _dbContext.Entry(_dbContext.Persons.Skip(1).First()).State.Should().Be(EntityState.Deleted);
        }

        [Fact]
        public void Find_ExistingKey_EntityIsReturned()
        {
            _dbContext.Persons.Add(new Person { Name = "Sergio" });
            _dbContext.Persons.Add(new Person { Name = "Mandy" });
            _dbContext.SaveChanges();

            var person = _efCoreDbContext.GetDbSet<Person>().Find(2);

            person.Name.Should().Be("Mandy");
        }

        [Fact]
        public async Task FindAsync_ExistingKey_EntityIsReturned()
        {
            _dbContext.Persons.Add(new Person { Name = "Sergio" });
            _dbContext.Persons.Add(new Person { Name = "Mandy" });
            _dbContext.SaveChanges();

            var person = await _efCoreDbContext.GetDbSet<Person>().FindAsync(2);

            person.Name.Should().Be("Mandy");
        }

        [Fact]
        public void AsQueryable_EfCoreDbSetQueryableIsReturned()
        {
            var queryable = _efCoreDbContext.GetDbSet<Person>().AsQueryable();

            queryable.Should().NotBeNull();
            queryable.Should().BeAssignableTo<IQueryable<Person>>();
        }

        [Fact]
        public void GetLocalCache_LocalEntitiesAreReturned()
        {
            _dbContext.Persons.Add(new Person { Name = "Sergio" });
            _dbContext.Persons.Add(new Person { Name = "Mandy" });

            var local = _efCoreDbContext.GetDbSet<Person>().GetLocalCache().ToList();

            local.Should().NotBeNull();
            local.Count.Should().Be(2);
        }

        public async ValueTask DisposeAsync()
        {
            _dbContext?.Dispose();

            if (_dbInitializer != null)
                await _dbInitializer.DisposeAsync();
        }
    }
}