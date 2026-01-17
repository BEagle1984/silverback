// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

[Trait("Type", "E2E")] // Specified here because traits are not inherited from E2ETests
[Trait("Broker", "Kafka")]
public abstract class KafkaTests : E2ETests
{
    protected const string DefaultTopicName = "default-e2e-topic";

    protected const string DefaultGroupId = "e2e-consumer-group-1";

    protected KafkaTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    protected IKafkaTestingHelper Helper => field ??= Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();

    protected IInMemoryTopic DefaultTopic => field ??= Helper.GetTopic(DefaultTopicName);

    protected IMockedConsumerGroup DefaultConsumerGroup => field ??= Helper.GetConsumerGroup(DefaultGroupId);
}
