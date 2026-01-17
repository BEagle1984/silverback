// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

[Trait("Type", "E2E")] // Specified here because traits are not inherited from E2ETests
[Trait("Broker", "MQTT")]
public abstract class MqttTests : E2ETests
{
    protected const string DefaultTopicName = "e2e/default";

    protected const string DefaultClientId = "e2e-client";

    protected MqttTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    protected IMqttTestingHelper Helper => field ??= Host.ServiceProvider.GetRequiredService<IMqttTestingHelper>();

    protected IClientSession DefaultClientSession => field ??= Helper.GetClientSession(DefaultClientId);

    protected IReadOnlyList<MqttApplicationMessage> GetDefaultTopicMessages() => Helper.GetMessages(DefaultTopicName);
}
