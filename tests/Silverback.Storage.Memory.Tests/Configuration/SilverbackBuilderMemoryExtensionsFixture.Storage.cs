// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Configuration;

public partial class SilverbackBuilderMemoryExtensionsFixture
{
    [Fact]
    public void AddInMemoryStorage_ShouldConfigureInMemoryStorageFactory()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddInMemoryStorage());
        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();

        InMemoryStorage<StoredItem> storage = storageFactory.GetStorage<StorageSettings, StoredItem>(new StorageSettings());

        storage.Should().NotBeNull();
        storage.Should().BeOfType<InMemoryStorage<StoredItem>>();
    }

    private record StorageSettings;

    [SuppressMessage("", "CA1812", Justification = "Class used for testing")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used for testing")]
    private class StoredItem
    {
    }
}
