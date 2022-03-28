// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Packets;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.MqttConfigProxies;

public static class TypesMapper
{
    public static bool MustGenerate(Type type) =>
        type.Name switch
        {
            nameof(MqttApplicationMessage) => false,
            nameof(MqttUserProperty) => false,
            _ => true
        };

    public static string GetProxyClassName(Type proxiedType) =>
        proxiedType.Name switch
        {
            nameof(MqttClientOptions) => "MqttClientConfiguration",
            nameof(MqttApplicationMessage) => "MqttLastWillMessageConfiguration",
            _ => proxiedType.Name.Replace("Options", "Configuration", StringComparison.Ordinal)
        };

    public static (string PropertyType, bool IsMapped) GetMappedPropertyTypeString(Type propertyType) =>
        propertyType.Name switch
        {
            nameof(IMqttClientCredentials) => ("MqttClientCredentials?", true),
            nameof(MqttApplicationMessage) => ("MqttLastWillMessageConfiguration?", true),
            nameof(IMqttClientChannelOptions) => ("MqttClientChannelConfiguration?", true),
            nameof(MqttClientTlsOptions) => ("MqttClientTlsConfiguration?", true),
            nameof(MqttClientWebSocketProxyOptions) => ("MqttClientWebSocketProxyConfiguration?", true),
            _ => (ReflectionHelper.GetTypeString(propertyType, true), false)
        };
}
