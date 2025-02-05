// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Storage;

internal class DbContextAsyncEnumerable<T> : DisposableAsyncEnumerable<T>
{
    private readonly DbContext _dbContext;

    private readonly IServiceScope _serviceScope;

    public DbContextAsyncEnumerable(IAsyncEnumerable<T> wrappedAsyncEnumerable, DbContext dbContext, IServiceScope serviceScope)
        : base(wrappedAsyncEnumerable)
    {
        _dbContext = dbContext;
        _serviceScope = serviceScope;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _dbContext.Dispose();
        _serviceScope.Dispose();
    }
}
