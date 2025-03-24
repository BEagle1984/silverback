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
    ///     Gets or sets the message Id.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    ///     Gets or sets the message raw binary content.
    /// </summary>
    [Required]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Entity Framework requirement")]
    public byte[]? Content { get; set; }

    /// <summary>
    ///     Gets or sets the message headers.
    /// </summary>
    [MaxLength(1000)]
    public string? Headers { get; set; }

    /// <summary>
    ///     Gets or sets the endpoint name.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string EndpointName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the serialized dynamic endpoint.
    /// </summary>
    [MaxLength(1000)]
    public string? DynamicEndpoint { get; set; }

    /// <summary>
    ///     Gets or sets the creation timestamp.
    /// </summary>
    [Required]
    public DateTime Created { get; set; }
}
