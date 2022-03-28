// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Resolves the target endpoint (e.g. the target topic and partition) for a message being produced.
/// </summary>
public interface IProducerEndpointResolver
{
    /// <summary>
    ///     Gets the raw endpoint name that can be used as <see cref="EndpointConfiguration.RawName" />.
    /// </summary>
    string RawName { get; }

    /// <summary>
    ///     Gets the computed actual target endpoint for the message being produced.
    /// </summary>
    /// <param name="message">
    ///     The message being produced.
    /// </param>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpoint" /> for the specified message.
    /// </returns>
    ProducerEndpoint GetEndpoint(object? message, ProducerEndpointConfiguration configuration, IServiceProvider serviceProvider);
}
