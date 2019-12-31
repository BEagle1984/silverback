// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Background.Model
{
    public class Lock
    {
        [Key, MaxLength(500)] public string Name { get; set; }

        [MaxLength(200)] public string UniqueId { get; set; }

        public DateTime Created { get; set; }

        public DateTime Heartbeat { get; set; }

        [Timestamp] public byte[] Timestamp { get; set; }
    }
}