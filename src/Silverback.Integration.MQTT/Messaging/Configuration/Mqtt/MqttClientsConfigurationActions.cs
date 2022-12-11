// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

internal class MqttClientsConfigurationActions
{
    public MergeableActionCollection<MqttClientConfigurationBuilder> ConfigurationActions { get; } = new();
}
