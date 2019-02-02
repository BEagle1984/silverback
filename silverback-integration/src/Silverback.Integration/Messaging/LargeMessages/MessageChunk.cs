using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.LargeMessages
{
    public class MessageChunk : IMessage
    {
        public Guid MessageId { get; set; }

        public string OriginalMessageId { get; set; }

        public int ChunkId { get; set; }

        public int ChunksCount { get; set; }

        public byte[] Content { get; set; }
    }
}
