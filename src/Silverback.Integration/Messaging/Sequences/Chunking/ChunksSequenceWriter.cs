// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Silverback.Messaging.Messages;
//
// namespace Silverback.Messaging.Sequences
// {
//     public class ChunksSequenceWriter : ISequenceWriter
//     {
//         public async Task<IAsyncEnumerable<IRawOutboundEnvelope>?> CreateSequence(IRawOutboundEnvelope envelope)
//         {
//             var settings = envelope.Endpoint.Chunk;
//
//             var chunkSize = settings?.Size ?? int.MaxValue;
//
//             if (envelope.RawMessage == null || chunkSize >= envelope.RawMessage.Length)
//                 return null;
//
//             var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);
//             if (string.IsNullOrEmpty(messageId))
//             {
//                 throw new InvalidOperationException(
//                     "Dividing into chunks is pointless if no unique MessageId can be retrieved. " +
//                     $"Please set the {DefaultMessageHeaders.MessageId} header.");
//             }
//
//             var messageMemory = envelope.RawMessage.AsMemory();
//             var chunksCount = (int)Math.Ceiling(envelope.RawMessage.Length / (double)chunkSize);
//             var offset = 0;
//
//             for (var i = 0; i < chunksCount; i++)
//             {
//                 var memorySlice = messageMemory.Slice(offset, Math.Min(chunkSize, envelope.RawMessage.Length - offset));
//
//                 var messageChunk = new OutboundEnvelope(envelope.Message, envelope.Headers, envelope.Endpoint)
//                 {
//                     RawMessage = memorySlice.ToArray()
//                 };
//
//                 messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunkIndex, i);
//                 messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunksCount, chunksCount);
//
//                 yield return messageChunk;
//
//                 offset += chunkSize;
//             }
//         }
//     }
// }


