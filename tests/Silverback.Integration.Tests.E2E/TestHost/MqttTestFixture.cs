// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    [Trait("Category", "E2E:MQTT")]
    public abstract class MqttTestFixture : E2ETestFixture<IMqttTestingHelper>
    {
        protected const string DefaultTopicName = "e2e/default";

        protected MqttTestFixture(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }
    }
}
