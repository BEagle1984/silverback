// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Testing;

/// <content>
///     Implements the <c>GetConsumer</c> methods.
/// </content>
public abstract partial class TestingHelper
{
    /// <inheritdoc cref="ITestingHelper.GetConsumer" />
    public IConsumer GetConsumer(string name) => _consumers?[name] ??
                                                 throw new InvalidOperationException($"No consumer found with name '{name}'.");

    /// <inheritdoc cref="ITestingHelper.GetConsumerForEndpoint" />
    public IConsumer GetConsumerForEndpoint(string endpointName) =>
        _consumers?.FirstOrDefault(
            consumer => consumer.EndpointsConfiguration.Any(
                endpoint =>
                    endpoint.RawName == endpointName || endpoint.FriendlyName == endpointName)) ??
        throw new InvalidOperationException($"No consumer found for endpoint '{endpointName}'.");

    /// <summary>
    ///     Returns the <see cref="ConsumerEndpointConfiguration" /> for the specified endpoint.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name.
    /// </param>
    /// <returns>
    ///     The <see cref="ConsumerEndpointConfiguration" />.
    /// </returns>
    protected ConsumerEndpointConfiguration? GetConsumerEndpointConfiguration(string endpointName) =>
        _consumers?
            .SelectMany(consumer => consumer.EndpointsConfiguration)
            .FirstOrDefault(configuration => configuration.RawName == endpointName || configuration.FriendlyName == endpointName);
}
