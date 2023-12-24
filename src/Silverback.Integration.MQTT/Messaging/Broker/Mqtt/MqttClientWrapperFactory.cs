// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt;

internal class MqttClientWrapperFactory : IMqttClientWrapperFactory
{
    private readonly IMqttNetClientFactory _clientFactory;

    private readonly IBrokerClientCallbacksInvoker _brokerClientCallbacksInvoker;

    private readonly ISilverbackLoggerFactory _silverbackLoggerFactory;

    public MqttClientWrapperFactory(
        IMqttNetClientFactory clientFactory,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        ISilverbackLoggerFactory silverbackLoggerFactory)
    {
        _clientFactory = Check.NotNull(clientFactory, nameof(clientFactory));
        _brokerClientCallbacksInvoker = Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _silverbackLoggerFactory = Check.NotNull(silverbackLoggerFactory, nameof(silverbackLoggerFactory));
    }

    public IMqttClientWrapper Create(string name, MqttClientConfiguration configuration) =>
        new MqttClientWrapper(
            name,
            _clientFactory.CreateClient(),
            configuration,
            _brokerClientCallbacksInvoker,
            _silverbackLoggerFactory.CreateLogger<MqttClientWrapper>());
}
