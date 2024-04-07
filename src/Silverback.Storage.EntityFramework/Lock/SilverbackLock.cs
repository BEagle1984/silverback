// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Lock;

/// <summary>
///     Represents a lock stored in the database.
/// </summary>
public class SilverbackLock
{
    /// <summary>
    ///     Gets the name of the lock.
    /// </summary>
    [Key]
    [Required]
    [MaxLength(200)]
    public string LockName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or sets the identifier of the handler that acquired the lock.
    /// </summary>
    [MaxLength(200)]
    public string? Handler { get; set; }

    /// <summary>
    ///     Gets or sets the acquired on timestamp.
    /// </summary>
    public DateTime? AcquiredOn { get; set; }

    /// <summary>
    ///     Gets or sets the last heartbeat timestamp.
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    ///    Gets or sets the concurrency token.
    /// </summary>
    [Timestamp]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Required by Entity Framework")]
    public byte[]? ConcurrencyToken { get; set; }
}
