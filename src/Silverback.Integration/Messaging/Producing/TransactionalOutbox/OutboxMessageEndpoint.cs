// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Encapsulates the endpoint information part of the <see cref="OutboxMessage" />.
/// </summary>
public class OutboxMessageEndpoint
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxMessageEndpoint" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The endpoint raw name.
    /// </param>
    /// <param name="friendlyName">
    ///     The endpoint friendly name.
    /// </param>
    /// <param name="serializedEndpoint">
    ///     The serialized endpoint (needed for dynamic endpoints only).
    /// </param>
    public OutboxMessageEndpoint(string rawName, string? friendlyName, byte[]? serializedEndpoint)
    {
        RawName = rawName;
        FriendlyName = friendlyName;
        SerializedEndpoint = serializedEndpoint;
    }

    /// <summary>
    ///     Gets the endpoint raw name.
    /// </summary>
    public string RawName { get; }

    /// <summary>
    ///     Gets the endpoint friendly name.
    /// </summary>
    public string? FriendlyName { get; }

    /// <summary>
    ///     Gets serialized the endpoint.
    /// </summary>
    /// <remarks>
    ///     This value will not be set for the static endpoints.
    /// </remarks>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Reviewed")]
    public byte[]? SerializedEndpoint { get; }
}
