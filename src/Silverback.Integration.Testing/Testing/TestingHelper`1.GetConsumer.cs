// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.Broker;

namespace Silverback.Testing;

/// <content>
///     Implements the <c>GetConsumer</c> methods.
/// </content>
public abstract partial class TestingHelper
{
    /// <inheritdoc cref="ITestingHelper.GetConsumer" />
    public IConsumer GetConsumer(string name) => _consumers?.First(consumer => consumer.Name == name) ??
                                                 throw new InvalidOperationException($"No consumer found with name '{name}'.");

    /// <inheritdoc cref="ITestingHelper.GetConsumerForEndpoint" />
    public IConsumer GetConsumerForEndpoint(string endpointName) =>
        _consumers?.FirstOrDefault(
            consumer => consumer.EndpointsConfiguration.Any(
                endpoint =>
                    endpoint.RawName == endpointName || endpoint.FriendlyName == endpointName)) ??
        throw new InvalidOperationException($"No consumer found for endpoint '{endpointName}'.");
}
