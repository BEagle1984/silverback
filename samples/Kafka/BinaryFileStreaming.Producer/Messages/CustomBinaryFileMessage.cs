﻿using Silverback.Messaging.Messages;

namespace Silverback.Samples.BinaryFileStreaming.Producer.Messages
{
    public class CustomBinaryFileMessage : BinaryFileMessage
    {
        [Header("x-filename")]
        public string? Filename { get; set; }
    }
}
