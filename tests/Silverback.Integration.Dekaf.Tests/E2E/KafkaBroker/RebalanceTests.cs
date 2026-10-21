// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Integration.E2E.KafkaBroker.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.KafkaBroker;

/// <summary>
///     Runs the in-memory <see cref="Kafka.RebalanceTests" /> against a Kafka container.
/// </summary>
[Trait("Type", "E2E")]
[Trait("Broker", "Kafka")]
[Trait("Target", "KafkaContainer")]
[Trait("Dependency", "Docker")] // Excluded from the CI build
public class RebalanceTests : Kafka.RebalanceTests
{
    public RebalanceTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        Host.PostConfigureServices(services => services.ReplaceMockedKafkaWithContainer());
    }
}
