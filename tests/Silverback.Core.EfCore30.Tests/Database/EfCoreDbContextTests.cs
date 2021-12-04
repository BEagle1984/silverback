// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Database;
using Silverback.Tests.Core.EFCore30.TestTypes;
using Silverback.Tests.Core.EFCore30.TestTypes.Model;
using Xunit;

namespace Silverback.Tests.Core.EFCore30.Database;

public sealed class EfCoreDbContextTests : IDisposable
{
    private readonly TestDbContextInitializer _dbInitializer;

    private readonly TestDbContext _dbContext;

    private readonly EfCoreDbContext<TestDbContext> _efCoreDbContext;

    public EfCoreDbContextTests()
    {
        _dbInitializer = new TestDbContextInitializer();
        _dbContext = _dbInitializer.GetTestDbContext();
        _efCoreDbContext = new EfCoreDbContext<TestDbContext>(_dbContext);
    }

    [Fact]
    public void GetDbSet_SomeEntity_EfCoreDbSetReturned()
    {
        IDbSet<Person> dbSet = _efCoreDbContext.GetDbSet<Person>();

        dbSet.Should().NotBeNull();
        dbSet.Should().BeOfType<EfCoreDbSet<Person>>();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _dbInitializer.Dispose();
    }
}
