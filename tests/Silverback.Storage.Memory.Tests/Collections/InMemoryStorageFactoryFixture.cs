// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Collections;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Collections;

public class InMemoryStorageFactoryFixture
{
    [Fact]
    public void Create_ShouldReturnInMemoryStorageAccordingToSettings()
    {
        InMemoryStorageFactory factory = new();

        InMemoryStorage<string> storage1A1 = factory.GetStorage<Settings1, string>(new Settings1 { Name = "A" });
        InMemoryStorage<string> storage1A2 = factory.GetStorage<Settings1, string>(new Settings1 { Name = "A" });
        InMemoryStorage<string> storage1B1 = factory.GetStorage<Settings1, string>(new Settings1 { Name = "B" });
        InMemoryStorage<string> storage1B2 = factory.GetStorage<Settings1, string>(new Settings1 { Name = "B" });
        InMemoryStorage<string> storage2A1 = factory.GetStorage<Settings2, string>(new Settings2 { Name = "A" });
        InMemoryStorage<string> storage2A2 = factory.GetStorage<Settings2, string>(new Settings2 { Name = "A" });

        storage1A1.Should().BeSameAs(storage1A2);
        storage1B1.Should().BeSameAs(storage1B2);
        storage1A1.Should().NotBeSameAs(storage1B1);
        storage2A1.Should().BeSameAs(storage2A2);
        storage2A1.Should().NotBeSameAs(storage1A1);
    }

    public record Settings1
    {
        public string? Name { get; init; }
    }

    public record Settings2
    {
        public string? Name { get; init; }
    }
}
