// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker
{
    public interface IConsumer
    {
        /// <summary>
        ///     Fired when a new message is received.
        /// </summary>
        event MessageReceivedHandler Received;

        /// <summary>
        ///     <param>Confirms that the message at the specified offset has been successfully processed.</param>
        ///     <param>
        ///         The acknowledgement will be sent to the message broker and the message will never be processed again
        ///         (by the same logical consumer / consumer group).
        ///     </param>
        /// </summary>
        /// <param name="offset">The offset to be committed.</param>
        Task Commit(IOffset offset);

        /// <summary>
        ///     <param>Confirms that the messages at the specified offsets have been successfully processed.</param>
        ///     <param>
        ///         The acknowledgement will be sent to the message broker and the message will never be processed again
        ///         (by the same logical consumer / consumer group).
        ///     </param>
        /// </summary>
        /// <param name="offsets">The offsets to be committed.</param>
        Task Commit(IEnumerable<IOffset> offsets);

        /// <summary>
        ///     <param>Notifies that an error occured while processing the message at the specified offset.</param>
        ///     <param>
        ///         If necessary the information will be sent to the message broker to ensure that the message will be
        ///         re-processed.
        ///     </param>
        /// </summary>
        /// <param name="offset">The offset to be rolled back.</param>
        Task Rollback(IOffset offset);

        /// <summary>
        ///     <param>Notifies that an error occured while processing the messages at the specified offsets.</param>
        ///     <param>
        ///         If necessary the information will be sent to the message broker to ensure that the message will be
        ///         re-processed.
        ///     </param>
        /// </summary>
        /// <param name="offsets">The offsets to be rolled back.</param>
        Task Rollback(IEnumerable<IOffset> offsets);

        /// <summary>
        ///     Connects and starts consuming.
        /// </summary>
        void Connect();

        /// <summary>
        ///     Disconnects and stops consuming.
        /// </summary>
        void Disconnect();
    }
}