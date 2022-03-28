// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

[Trait("Category", "E2E:MQTT")]
public abstract class MqttFixture : E2EFixture
{
    protected const string DefaultTopicName = "e2e/default";

    protected const string DefaultClientId = "e2e-client";

    private IMqttTestingHelper? _testingHelper;

    private IClientSession? _defaultClientSession;

    protected MqttFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    protected IMqttTestingHelper Helper => _testingHelper ??= Host.ServiceProvider.GetRequiredService<IMqttTestingHelper>();

    protected IClientSession DefaultClientSession => _defaultClientSession ??= Helper.GetClientSession(DefaultClientId);

    protected IReadOnlyList<MqttApplicationMessage> GetDefaultTopicMessages() => Helper.GetMessages(DefaultTopicName);
}
