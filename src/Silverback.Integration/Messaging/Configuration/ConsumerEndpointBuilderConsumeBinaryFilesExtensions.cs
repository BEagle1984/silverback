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
    ///     Adds the <c>ConsumeBinaryFiles</c> method to the
    ///     <see cref="ConsumerEndpointBuilder{TEndpoint,TBuilder}" />.
    /// </summary>
    public static class ConsumerEndpointBuilderConsumeBinaryFilesExtensions
    {
        /// <summary>
        ///     <para>
        ///         Sets the serializer to an instance of <see cref="BinaryFileMessageSerializer" /> (or
        ///         <see cref="BinaryFileMessageSerializer{TModel}" />) to wrap the consumed binary files into a
        ///         <see cref="BinaryFileMessage" />.
        ///     </para>
        ///     <para>
        ///         This settings will force the <see cref="BinaryFileMessageSerializer" /> to be used regardless of
        ///         the message type header.
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
        public static TBuilder ConsumeBinaryFiles<TBuilder>(
            this IConsumerEndpointBuilder<TBuilder> endpointBuilder,
            Action<IBinaryFileMessageSerializerBuilder>? serializerBuilderAction = null)
            where TBuilder : IConsumerEndpointBuilder<TBuilder>
        {
            Check.NotNull(endpointBuilder, nameof(endpointBuilder));

            var serializerBuilder = new BinaryFileMessageSerializerBuilder();
            serializerBuilderAction?.Invoke(serializerBuilder);
            endpointBuilder.DeserializeUsing(serializerBuilder.Build());

            return (TBuilder)endpointBuilder;
        }
    }
}
