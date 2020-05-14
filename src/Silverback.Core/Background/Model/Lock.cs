// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Background.Model
{
    /// <summary>
    ///     The model of the persisted lock written by the <see cref="IDistributedLockManager"/>.
    /// </summary>
    public class Lock
    {
        /// <summary>
        ///     Gets or sets the name of the lock / the resource being locked.
        /// </summary>
        [Key]
        [MaxLength(500)]
        public string? Name { get; set; }

        /// <summary>
        ///     Gets or sets a unique identifier representing the entity trying to acquire the lock.
        /// </summary>
        [MaxLength(200)]
        public string? UniqueId { get; set; }

        /// <summary>
        ///     Gets or sets the record creation date.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        ///     Gets or sets the last heartbeat timestamp.
        /// </summary>
        public DateTime Heartbeat { get; set; }

        /// <summary>
        ///     Gets or sets the concurrency token.
        /// </summary>
        [Timestamp]
        [SuppressMessage("ReSharper", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? Timestamp { get; set; }
    }
}
