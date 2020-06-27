// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Database.Model
{
    /// <summary>
    ///     The entity stored in the offset storage table.
    /// </summary>
    public class StoredOffset
    {
        /// <summary>
        ///     Gets or sets the offset key.
        /// </summary>
        [Key]
        [MaxLength(300)]
        public string Key { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the assembly qualified name of the stored offset class.
        /// </summary>
        [MaxLength(300)]
        public string? ClrType { get; set; }

        /// <summary>
        ///     Gets or sets the offset value.
        /// </summary>
        [MaxLength(300)]
        public string? Value { get; set; }

        /// <summary>
        ///     Gets or sets the serialized offset.
        /// </summary>
        /// <remarks>
        ///     This field is no longer used (replaced by ClrType and Value) and will be removed with the next major
        ///     release.
        /// </remarks>
        [MaxLength(500)]
        [Obsolete("Replaced by ClrType and Value.")]
        public string? Offset { get; set; }
    }
}
