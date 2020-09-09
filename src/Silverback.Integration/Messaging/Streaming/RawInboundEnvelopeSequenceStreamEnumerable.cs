// TODO: DELETE?

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Silverback.Messaging.Broker;
// using Silverback.Messaging.Messages;
// using Silverback.Util;
//
// namespace Silverback.Messaging.Streaming
// {
//     internal class RawInboundEnvelopeSequenceStreamEnumerable : RawInboundEnvelopeStreamEnumerable
//     {
//         private readonly List<IOffset> _offsets = new List<IOffset>();
//
//         public RawInboundEnvelopeSequenceStreamEnumerable(int bufferCapacity = 1)
//             : base(bufferCapacity)
//         {
//         }
//
//         public IReadOnlyList<IOffset> Offsets => _offsets;
//
//         public override Task PushAsync(IRawInboundEnvelope message, CancellationToken cancellationToken = default)
//         {
//             Check.NotNull(message, nameof(message));
//
//             if (message.Offset != null)
//                 _offsets.Add(message.Offset);
//
//             return base.PushAsync(message, cancellationToken);
//         }
//     }
// }
