// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>ProduceBinaryFiles</c> method to the
    ///     <see cref="ProducerEndpointBuilder{TEndpoint,TBuilder}" />.
    /// </summary>
    public static class ProducerEndpointBuilderProduceBinaryFilesExtensions
    {
        /// <summary>
        ///     <para>
        ///         Sets the serializer to an instance of <see cref="BinaryFileMessageSerializer" /> (or
        ///         <see cref="BinaryFileMessageSerializer{TMessage}" />) to produce the <see cref="BinaryFileMessage" />.
        ///     </para>
        ///     <para>
        ///         By default this serializer forwards the message type in an header to let the consumer know which
        ///         type has to be deserialized. This approach allows to mix messages of different types in the same
        ///         endpoint and it's ideal when both the producer and the consumer are using Silverback but might not
        ///         be optimal for interoperability. This behavior can be changed using the builder action and
        ///         specifying the model to be used.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     This replaces the <see cref="IMessageSerializer" /> and the endpoint will only be able to deal with
        ///     binary files.
        /// </remarks>
        /// <typeparam name="TBuilder">
        ///     The actual builder type.
        /// </typeparam>
        /// <param name="endpointBuilder">
        ///     The endpoint builder.
        /// </param>
        /// <param name="serializerBuilderAction">
        ///     An optional <see cref="Action{T}" /> that takes the <see cref="IBinaryFileMessageSerializerBuilder" />
        ///     and configures it.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        public static TBuilder ProduceBinaryFiles<TBuilder>(
            this IProducerEndpointBuilder<TBuilder> endpointBuilder,
            Action<IBinaryFileMessageSerializerBuilder>? serializerBuilderAction = null)
            where TBuilder : IProducerEndpointBuilder<TBuilder>
        {
            Check.NotNull(endpointBuilder, nameof(endpointBuilder));

            var serializerBuilder = new BinaryFileMessageSerializerBuilder();
            serializerBuilderAction?.Invoke(serializerBuilder);
            endpointBuilder.SerializeUsing(serializerBuilder.Build());

            return (TBuilder)endpointBuilder;
        }
    }
}
