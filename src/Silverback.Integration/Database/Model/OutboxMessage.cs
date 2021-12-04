// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Database.Model;

/// <summary>
///     The entity stored in the outbox table.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    ///     Gets or sets the primary key (identity).
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the assembly qualified name of the message class.
    /// </summary>
    [MaxLength(500)]
    public string? MessageType { get; set; }

    /// <summary>
    ///     Gets or sets the message content (body).
    /// </summary>
    [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
    public byte[]? Content { get; set; }

    /// <summary>
    ///     Gets or sets the serialized message headers.
    /// </summary>
    [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
    public byte[]? Headers { get; set; }

    /// <summary>
    ///     Gets or sets the raw name of the producer endpoint.
    /// </summary>
    [MaxLength(300)]
    public string EndpointRawName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the friendly name of the producer endpoint.
    /// </summary>
    [MaxLength(300)]
    public string? EndpointFriendlyName { get; set; }

    /// <summary>
    ///     Gets or sets the serialized endpoint.
    /// </summary>
    [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
    public byte[]? Endpoint { get; set; }

    /// <summary>
    ///     Gets or sets the datetime when the message was stored in the queue.
    /// </summary>
    public DateTime Created { get; set; }
}
