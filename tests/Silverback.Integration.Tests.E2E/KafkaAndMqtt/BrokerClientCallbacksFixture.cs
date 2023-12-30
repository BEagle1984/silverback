// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.KafkaAndMqtt;

[Trait("Broker", "Kafka+MQTT")]
public partial class BrokerClientCallbacksFixture : KafkaFixture
{
    public BrokerClientCallbacksFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }
}
