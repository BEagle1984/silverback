// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     The context that is passed from the producer or consumer to the serializer. It can be used to
    ///     customize the serialization behavior according to the endpoint.
    /// </summary>
    public class MessageSerializationContext
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageSerializationContext" /> class from the provided
        ///     <see cref="IEndpoint" />.
        /// </summary>
        /// <param name="endpoint"> The related endpoint configuration. </param>
        /// <param name="actualEndpointName"> The name of the actual related endpoint. </param>
        public MessageSerializationContext(IEndpoint? endpoint, string? actualEndpointName = null)
        {
            Endpoint = endpoint;
            ActualEndpointName = actualEndpointName ?? endpoint?.Name;
        }

        /// <summary> Gets the default instance of an empty context. </summary>
        public static MessageSerializationContext Empty { get; } = new MessageSerializationContext(null);

        /// <summary> Gets the related endpoint configuration. </summary>
        public IEndpoint? Endpoint { get; }

        /// <summary>
        ///     Gets the name of the actual endpoint (in case the <c> Endpoint </c> configuration points to multiple
        ///     endpoints, for example if consuming multiple topics with a single <c> KafkaConsumer </c>).
        /// </summary>
        public string? ActualEndpointName { get; }
    }
}
