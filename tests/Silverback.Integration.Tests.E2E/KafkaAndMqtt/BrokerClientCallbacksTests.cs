// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.KafkaAndMqtt;

[Trait("Type", "E2E")] // Specified here because traits are not inherited from E2ETests
[Trait("Broker", "Kafka+MQTT")]
public partial class BrokerClientCallbacksTests : KafkaTests
{
    public BrokerClientCallbacksTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }
}
