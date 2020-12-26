// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Testing
{
    /// <inheritdoc cref="ITestingHelper{TBroker}"/>
    public interface IMqttTestingHelper : ITestingHelper<MqttBroker>
    {
    }
}
