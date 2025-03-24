// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Integration.E2E.TestHost;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class BrokerClientCallbacksFixture : MqttFixture
{
    public BrokerClientCallbacksFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }
}
