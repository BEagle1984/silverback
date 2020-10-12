// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Samples.BinaryFileStreaming.Consumer.Messages
{
    public class CustomBinaryFileMessage : BinaryFileMessage
    {
        [Header("x-filename")]
        public string? Filename { get; set; }
    }
}
