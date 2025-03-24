// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Configuration;

public partial class BrokerOptionsBuilderEntityFrameworkExtensionsFixture
{
    private static DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
