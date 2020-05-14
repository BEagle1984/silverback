// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;

namespace Silverback.Messaging.Connectors.Model
{
    /// <summary>
    ///     The entity stored in the offset storage table.
    /// </summary>
    public class StoredOffset
    {
        /// <summary> Gets or sets the offset key. </summary>
        [Key]
        [MaxLength(300)]
        public string Key { get; set; } = null!;

        /// <summary> Gets or sets the serialized offset. </summary>
        [MaxLength(500)]
        public string Offset { get; set; } = null!;
    }
}
