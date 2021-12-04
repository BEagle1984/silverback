// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Database;
using Silverback.Tests.Core.EFCore30.TestTypes;
using Silverback.Tests.Core.EFCore30.TestTypes.Model;
using Xunit;

namespace Silverback.Tests.Core.EFCore30.Database;

public sealed class EfCoreQueryableTests : IDisposable
{
    private readonly TestDbContextInitializer _dbInitializer;

    private readonly TestDbContext _dbContext;

    private readonly EfCoreDbContext<TestDbContext> _efCoreDbContext;

    public EfCoreQueryableTests()
    {
        _dbInitializer = new TestDbContextInitializer();
        _dbContext = _dbInitializer.GetTestDbContext();
        _efCoreDbContext = new EfCoreDbContext<TestDbContext>(_dbContext);

        SilverbackQueryableExtensions.Implementation = new EfCoreQueryableExtensions();
    }

    [Fact]
    public async Task AnyAsync_EmptySet_FalseReturned()
    {
        bool result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .AnyAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnyAsync_NotEmptySet_TrueReturned()
    {
        _dbContext.Persons.Add(new Person());
        _dbContext.SaveChanges();

        bool result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .AnyAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithNotMatchingPredicate_FalseReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 20 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        bool result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .AnyAsync(p => p.Age > 35);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnyAsync_WithMatchingPredicate_TrueReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 20 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        bool result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .AnyAsync(p => p.Age == 30);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_EmptySet_NullReturned()
    {
        Person result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .FirstOrDefaultAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NotEmptySet_FirstEntityReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 20 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        Person result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .FirstOrDefaultAsync();

        result.Should().NotBeNull();
        result.Age.Should().Be(20);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NotMatchingPredicate_NullReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 20 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        Person? result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .FirstOrDefaultAsync(p => p.Age < 18);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_MatchingPredicate_FirstEntityReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 20 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.Persons.Add(new Person { Age = 40 });
        _dbContext.SaveChanges();

        Person? result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .FirstOrDefaultAsync(p => p.Age > 25);

        result.Should().NotBeNull();
        result!.Age.Should().Be(30);
    }

    [Fact]
    public async Task CountAsync_EmptySet_ZeroReturned()
    {
        int result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .CountAsync();

        result.Should().Be(0);
    }

    [Fact]
    public async Task CountAsync_NotEmptySet_CountReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 20 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        int result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .CountAsync();

        result.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_CorrectCountReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 15 });
        _dbContext.Persons.Add(new Person { Age = 17 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        int result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .CountAsync(p => p.Age >= 18);

        result.Should().Be(1);
    }

    [Fact]
    public async Task ToListAsync_NotEmptySet_ListReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 15 });
        _dbContext.Persons.Add(new Person { Age = 17 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        List<Person> result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .ToListAsync();

        result.Should().NotBeNull();
        result.Should().BeOfType<List<Person>>();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ToListAsync_ApplyingWhereClause_FilteredListReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 15 });
        _dbContext.Persons.Add(new Person { Age = 17 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        List<Person> result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .Where(p => p.Age <= 18)
            .ToListAsync();

        result.Should().NotBeNull();
        result.Should().BeOfType<List<Person>>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ToDictionaryAsync_NotEmptySet_DictionaryReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 15 });
        _dbContext.Persons.Add(new Person { Age = 17 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        Dictionary<int, int> result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .ToDictionaryAsync(
                p => p.Id,
                p => p.Age);

        result.Should().NotBeNull();
        result.Should().BeOfType<Dictionary<int, int>>();
        result.Should().HaveCount(3);
        result.First().Key.Should().Be(1);
        result.First().Value.Should().Be(15);
    }

    [Fact]
    public async Task ToDictionaryAsync_ApplyingWhereClause_DictionaryReturned()
    {
        _dbContext.Persons.Add(new Person { Age = 15 });
        _dbContext.Persons.Add(new Person { Age = 17 });
        _dbContext.Persons.Add(new Person { Age = 30 });
        _dbContext.SaveChanges();

        Dictionary<int, int> result = await _efCoreDbContext.GetDbSet<Person>().AsQueryable()
            .Where(p => p.Age <= 18)
            .ToDictionaryAsync(
                p => p.Id,
                p => p.Age);

        result.Should().NotBeNull();
        result.Should().BeOfType<Dictionary<int, int>>();
        result.Should().HaveCount(2);
        result.First().Key.Should().Be(1);
        result.First().Value.Should().Be(15);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _dbInitializer.Dispose();
    }
}
