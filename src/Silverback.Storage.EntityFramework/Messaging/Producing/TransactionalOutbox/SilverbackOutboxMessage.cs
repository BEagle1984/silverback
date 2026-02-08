// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Represents a message stored in the outbox.
/// </summary>
public class SilverbackOutboxMessage
{
    /// <summary>
    ///     Gets the message Id.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }

    /// <summary>
    ///     Gets the message raw binary content.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Entity Framework requirement")]
    public byte[]? Content { get; init; }

    /// <summary>
    ///     Gets the message headers.
    /// </summary>
    [MaxLength(1000)]
    public string? Headers { get; init; }

    /// <summary>
    ///     Gets the optional extra data (broker-specific).
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Entity Framework requirement")]
    public byte[]? Extra { get; init; }

    /// <summary>
    ///     Gets the endpoint name.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string EndpointName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the serialized dynamic endpoint.
    /// </summary>
    [MaxLength(1000)]
    public string? DynamicEndpoint { get; init; }

    /// <summary>
    ///     Gets the creation timestamp.
    /// </summary>
    [Required]
    public DateTime Created { get; init; }
}
