// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="BinaryFileMessageSerializer" /> or <see cref="BinaryFileMessageSerializer{TMessage}" />.
    /// </summary>
    public sealed class BinaryFileMessageSerializerBuilder
    {
        private IMessageSerializer? _serializer;

        private MethodInfo? _useModelMethodInfo;

        /// <summary>
        ///     Specifies a custom model to wrap the binary file.
        /// </summary>
        /// <typeparam name="TModel">
        ///     The type of the <see cref="IBinaryFileMessage" /> implementation.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="BinaryFileMessageSerializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        public BinaryFileMessageSerializerBuilder UseModel<TModel>()
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

        internal void UseModel(Type type)
        {
            if (!typeof(IBinaryFileMessage).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    $"The type {type.FullName} does not implement {nameof(IBinaryFileMessage)}.",
                    nameof(type));
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException(
                    $"The type {type.FullName} does not have a default constructor.",
                    nameof(type));
            }

            _useModelMethodInfo ??= GetType().GetMethod(nameof(UseModel))!;
            _useModelMethodInfo.MakeGenericMethod(type).Invoke(this, Array.Empty<object>());
        }
    }
}
