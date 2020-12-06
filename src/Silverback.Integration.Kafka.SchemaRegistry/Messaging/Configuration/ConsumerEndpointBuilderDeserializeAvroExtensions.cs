// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>DeserializeAvro</c> method to the
    ///     <see cref="ConsumerEndpointBuilder{TEndpoint,TBuilder}" />.
    /// </summary>
    public static class ConsumerEndpointBuilderDeserializeAvroExtensions
    {
        /// <summary>
        ///     Sets the serializer to an instance of <see cref="AvroMessageSerializer{TMessage}" /> to deserialize
        ///     the consumed Avro serialized message.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///     The actual builder type.
        /// </typeparam>
        /// <param name="endpointBuilder">
        ///     The endpoint builder.
        /// </param>
        /// <param name="serializerBuilderAction">
        ///     An optional <see cref="Action{T}" /> that takes the <see cref="IAvroMessageSerializerBuilder" /> and
        ///     configures it.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        public static TBuilder DeserializeAvro<TBuilder>(
            this IConsumerEndpointBuilder<TBuilder> endpointBuilder,
            Action<IAvroMessageSerializerBuilder>? serializerBuilderAction = null)
            where TBuilder : IConsumerEndpointBuilder<TBuilder>
        {
            Check.NotNull(endpointBuilder, nameof(endpointBuilder));

            var serializerBuilder = new AvroMessageSerializerBuilder();
            serializerBuilderAction?.Invoke(serializerBuilder);
            endpointBuilder.DeserializeUsing(serializerBuilder.Build());

            return (TBuilder)endpointBuilder;
        }
    }
}
