// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Messaging.Connectors.Model
{
    /// <summary>
    /// The entity to be stored in the offset storage table.
    /// </summary>

    public class StoredOffset
    {
        [Key, MaxLength(300)]
        public string Key { get; set; }

        [MaxLength(500)]
        public string Offset { get; set; }
    }
}