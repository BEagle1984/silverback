// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    ///     The base class for all services that transform the inbound or outbound raw binary.
    /// </summary>
    public interface IRawMessageTransformer
    {
        /// <summary>
        ///     Transforms the specified message.
        /// </summary>
        /// <param name="message">
        ///     The <see cref="Stream" /> containing the message to be transformed.
        /// </param>
        /// <param name="headers">
        ///     The headers collections (can be modified by the transformer).
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="Stream" /> with transformed message.
        /// </returns>
        Task<Stream?> TransformAsync(Stream? message, MessageHeaderCollection headers);
    }
}
