// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Storage;

public class DbContextAsyncEnumerableTests
{
    [Fact]
    public async Task GetAsyncEnumerator_ShouldEnumerateWrappedEnumerable()
    {
        await using TestDbContext dbContext = new();
        IServiceScope serviceScope = Substitute.For<IServiceScope>();
        using DbContextAsyncEnumerable<int> enumerable = new(new[] { 1, 2, 3 }.ToAsyncEnumerable(), dbContext, serviceScope);

        int[] items = await enumerable.ToArrayAsync();

        items.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Dispose_ShouldDisposeDbContextAndServiceScope()
    {
        TestDbContext dbContext = new();
        IServiceScope serviceScope = Substitute.For<IServiceScope>();
        DbContextAsyncEnumerable<int> enumerable = new(new[] { 1, 2, 3 }.ToAsyncEnumerable(), dbContext, serviceScope);

        enumerable.Dispose();

        Action act = () => _ = dbContext.Model;
        act.ShouldThrow<ObjectDisposedException>();
        serviceScope.Received(1).Dispose();
    }

    private sealed class TestDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("Data Source=:memory:");
    }
}
