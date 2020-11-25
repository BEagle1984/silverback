// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="BinaryFileMessageSerializer" /> or
    ///     <see cref="BinaryFileMessageSerializer{TMessage}" />.
    /// </summary>
    public class BinaryFileMessageSerializerBuilder : IBinaryFileMessageSerializerBuilder
    {
        private IMessageSerializer? _serializer;

        /// <inheritdoc cref="IBinaryFileMessageSerializerBuilder.UseModel{TModel}" />
        public IBinaryFileMessageSerializerBuilder UseModel<TModel>()
            where TModel : IBinaryFileMessage, new()
        {
            _serializer = new BinaryFileMessageSerializer<TModel>();
            return this;
        }

        /// <summary>
        ///     Builds the <see cref="IMessageSerializer" /> instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMessageSerializer" />.
        /// </returns>
        public IMessageSerializer Build() => _serializer ?? new BinaryFileMessageSerializer();
    }
}
