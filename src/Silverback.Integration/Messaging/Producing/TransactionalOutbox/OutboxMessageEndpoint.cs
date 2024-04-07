// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Encapsulates the endpoint information part of the <see cref="OutboxMessage" />.
/// </summary>
public class OutboxMessageEndpoint
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxMessageEndpoint" /> class.
    /// </summary>
    /// <param name="friendlyName">
    ///     The endpoint friendly name.
    /// </param>
    /// <param name="dynamicEndpoint">
    ///     The serialized dynamic endpoint.
    /// </param>
    public OutboxMessageEndpoint(string friendlyName, string? dynamicEndpoint)
    {
        FriendlyName = friendlyName;
        DynamicEndpoint = dynamicEndpoint;
    }

    /// <summary>
    ///     Gets the endpoint friendly name.
    /// </summary>
    public string FriendlyName { get; }

    /// <summary>
    ///     Gets the serialized dynamic endpoint.
    /// </summary>
    /// <remarks>
    ///     This value will not be set for the static endpoints.
    /// </remarks>
    public string? DynamicEndpoint { get; }
}
