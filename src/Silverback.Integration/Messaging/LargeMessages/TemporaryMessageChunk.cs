﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Messaging.LargeMessages
{
    public class TemporaryMessageChunk
    {
        [Key, MaxLength(300)] public string OriginalMessageId { get; set; }

        [Key] public int ChunkId { get; set; }

        public int ChunksCount { get; set; }

        public byte[] Content { get; set; }

        public DateTime Received { get; set; }
    }
}