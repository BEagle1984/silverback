// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

internal sealed class MockedMqttOptionsBuilder : IMockedMqttOptionsBuilder
{
    public MockedMqttOptionsBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IMockedMqttOptions MockedMqttOptions =>
        Services.GetSingletonServiceInstance<IMockedMqttOptions>() ??
        throw new InvalidOperationException("IMockedMqttOptions not found, AddMockedMqtt has not been called.");

    public IMockedMqttOptionsBuilder DelayConnection(TimeSpan delay)
    {
        MockedMqttOptions.ConnectionDelay = delay;
        return this;
    }
}
