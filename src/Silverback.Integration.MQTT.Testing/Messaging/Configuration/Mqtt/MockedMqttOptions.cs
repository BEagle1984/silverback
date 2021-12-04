// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Mqtt;

internal sealed class MockedMqttOptions : IMockedMqttOptions
{
    public TimeSpan ConnectionDelay { get; set; } = TimeSpan.FromMilliseconds(10);
}
