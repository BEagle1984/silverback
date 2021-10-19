// TODO: DELETE




// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using Silverback.Messaging.Messages;
// using Silverback.Util;
//
// namespace Silverback.Messaging.Serialization
// {
//     /// <summary>
//     ///     The context that is passed from the producer or consumer to the serializer. It can be used to
//     ///     customize the serialization behavior according to the endpoint.
//     /// </summary>
//     public class MessageSerializationContext
//     {
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="MessageSerializationContext" /> class from the provided
//         ///     <see cref="IEndpoint" />.
//         /// </summary>
//         /// <param name="envelope">
//         ///     The <see cref="IOutboundEnvelope" />.
//         /// </param>
//         public MessageSerializationContext(IOutboundEnvelope envelope)
//         {
//             ActualEndpoint = Check.NotNull(envelope, nameof(envelope)).ActualEndpoint;
//         }
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="MessageSerializationContext" /> class from the provided
//         ///     <see cref="IEndpoint" />.
//         /// </summary>
//         /// <param name="envelope">
//         ///     The <see cref="IRawInboundEnvelope" />.
//         /// </param>
//         public MessageSerializationContext(IRawInboundEnvelope envelope)
//         {
//             ActualEndpoint = Check.NotNull(envelope, nameof(envelope)).ActualEndpoint;
//         }
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="MessageSerializationContext" /> class from the provided
//         ///     <see cref="IEndpoint" />.
//         /// </summary>
//         /// <param name="endpoint">
//         ///     The related endpoint.
//         /// </param>
//         public MessageSerializationContext(IActualEndpoint endpoint)
//         {
//             ActualEndpoint = Check.NotNull(endpoint, nameof(endpoint));
//         }
//
//         /// <summary>
//         ///     Gets the default instance of an empty context.
//         /// </summary>
//         public static MessageSerializationContext Empty { get; } = new(NullActualEndpoint.Instance);
//
//         /// <summary>
//         ///     Gets the related endpoint.
//         /// </summary>
//         public IActualEndpoint ActualEndpoint { get; }
//     }
// }
