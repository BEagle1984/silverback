// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="BinaryFileMessageSerializer{TModel}" /> or
    ///     <see cref="BinaryFileMessageSerializer{TMessage}" />.
    /// </summary>
    public interface IBinaryFileMessageSerializerBuilder
    {
        /// <summary>
        ///     Specifies a custom model to wrap the binary file.
        /// </summary>
        /// <typeparam name="TModel">
        ///     The type of the <see cref="IBinaryFileMessage" /> implementation.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IBinaryFileMessageSerializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        IBinaryFileMessageSerializerBuilder UseModel<TModel>()
            where TModel : IBinaryFileMessage, new();
    }
}
