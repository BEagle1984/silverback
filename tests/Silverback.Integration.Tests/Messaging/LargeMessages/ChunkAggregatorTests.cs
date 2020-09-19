// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// TODO: Reimplement based on sequencer

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using FluentAssertions;
// using Silverback.Messaging.Chunking;
// using Silverback.Messaging.Chunking.Model;
// using Silverback.Messaging.Connectors;
// using Silverback.Messaging.Inbound.Transaction;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Serialization;
// using Silverback.Tests.Integration.TestTypes;
// using Silverback.Tests.Integration.TestTypes.Domain;
// using Silverback.Util;
// using Xunit;
//
// namespace Silverback.Tests.Integration.Messaging.LargeMessages
// {
//     public class ChunkAggregatorTests
//     {
//         private readonly IChunkStore _store =
//             new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());
//
//         private readonly ConsumerTransactionManager _transactionManager = new ConsumerTransactionManager();
//
//         private readonly IMessageSerializer _serializer = new JsonMessageSerializer();
//
//         [Fact]
//         public async Task AggregateIfComplete_AllChunks_Joined()
//         {
//             var headers = new MessageHeaderCollection();
//             var originalMessage = new BinaryMessage
//             {
//                 MessageId = Guid.NewGuid(),
//                 Content = GetByteArray(500)
//             };
//
//             var originalSerializedMessage = _serializer.Serialize(
//                 originalMessage,
//                 headers,
//                 MessageSerializationContext.Empty);
//
//             var chunks = new InboundEnvelope[3];
//             chunks[0] = new InboundEnvelope(
//                 originalSerializedMessage.AsMemory().Slice(0, 300).ToArray(),
//                 HeadersHelper.GetChunkHeaders(originalMessage.MessageId.ToString(), 0, 3),
//                 null,
//                 TestConsumerEndpoint.GetDefault(),
//                 TestConsumerEndpoint.GetDefault().Name);
//             chunks[1] = new InboundEnvelope(
//                 originalSerializedMessage.AsMemory().Slice(300, 300).ToArray(),
//                 HeadersHelper.GetChunkHeaders(originalMessage.MessageId.ToString(), 1, 3),
//                 null,
//                 TestConsumerEndpoint.GetDefault(),
//                 TestConsumerEndpoint.GetDefault().Name);
//             chunks[2] = new InboundEnvelope(
//                 originalSerializedMessage.AsMemory().Slice(600).ToArray(),
//                 HeadersHelper.GetChunkHeaders(originalMessage.MessageId.ToString(), 2, 3),
//                 null,
//                 TestConsumerEndpoint.GetDefault(),
//                 TestConsumerEndpoint.GetDefault().Name);
//
//             var result = await new ChunkAggregator(_store, _transactionManager).AggregateIfComplete(chunks[0]);
//             result.Should().BeNull();
//             result = await new ChunkAggregator(_store, _transactionManager).AggregateIfComplete(chunks[1]);
//             result.Should().BeNull();
//             result = await new ChunkAggregator(_store, _transactionManager).AggregateIfComplete(chunks[2]);
//             result.Should().NotBeNull();
//
//             var deserializedResult = (BinaryMessage)_serializer.Deserialize(
//                 result,
//                 headers,
//                 MessageSerializationContext.Empty).Item1!;
//
//             deserializedResult?.Content.Should().BeEquivalentTo(originalMessage.Content);
//         }
//
//         private static byte[] GetByteArray(int size) => Enumerable.Range(0, size).Select(_ => (byte)255).ToArray();
//     }
// }


