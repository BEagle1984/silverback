// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>DeserializeJson</c> method to the
    ///     <see cref="ConsumerEndpointBuilder{TEndpoint,TBuilder}" />.
    /// </summary>
    public static class ConsumerEndpointBuilderDeserializeJsonExtensions
    {
        /// <summary>
        ///     <para>
        ///         Sets the serializer to an instance of <see cref="JsonMessageSerializer" /> (or
        ///         <see cref="JsonMessageSerializer{TMessage}" />) to deserialize the consumed JSON.
        ///     </para>
        ///     <para>
        ///         By default this serializer relies on the message type header to determine the type of the message
        ///         to be deserialized. This behavior can be changed using the builder action and specifying a fixed
        ///         message type.
        ///     </para>
        /// </summary>
        /// <typeparam name="TBuilder">
        ///     The actual builder type.
        /// </typeparam>
        /// <param name="endpointBuilder">
        ///     The endpoint builder.
        /// </param>
        /// <param name="serializerBuilderAction">
        ///     An optional <see cref="Action{T}" /> that takes the <see cref="IJsonMessageSerializerBuilder" /> and
        ///     configures it.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        public static TBuilder DeserializeJson<TBuilder>(
            this IConsumerEndpointBuilder<TBuilder> endpointBuilder,
            Action<IJsonMessageSerializerBuilder>? serializerBuilderAction = null)
            where TBuilder : IConsumerEndpointBuilder<TBuilder>
        {
            Check.NotNull(endpointBuilder, nameof(endpointBuilder));

            var serializerBuilder = new JsonMessageSerializerBuilder();

            if (endpointBuilder.MessageType != null)
                serializerBuilder.UseFixedType(endpointBuilder.MessageType);

            serializerBuilderAction?.Invoke(serializerBuilder);
            endpointBuilder.DeserializeUsing(serializerBuilder.Build());

            return (TBuilder)endpointBuilder;
        }
    }
}
