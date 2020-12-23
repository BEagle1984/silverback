// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    [Trait("Category", "MQTT")]
    public abstract class MqttTestFixture : E2ETestFixture
    {
        protected const string DefaultTopicName = "default-e2e-topic";

        private IMqttTestingHelper? _testingHelper;

        protected MqttTestFixture(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected IMqttTestingHelper TestingHelper =>
            _testingHelper ??= Host.ScopedServiceProvider.GetRequiredService<IMqttTestingHelper>();
    }
}
