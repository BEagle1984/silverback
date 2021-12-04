// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Database.Model;

namespace Silverback.Tests.Core.TestTypes.Database;

public class TestDbContext : DbContext
{
    public TestDbContext()
    {
    }

    public TestDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Lock> Locks { get; set; } = null!;
}
