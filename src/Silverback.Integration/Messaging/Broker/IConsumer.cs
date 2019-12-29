// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker
{
    public interface IConsumer
    {
        /// <summary>
        /// Fired when a new message is received.
        /// </summary>
        event MessageReceivedHandler Received;

        /// <summary>
        /// Commits the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        Task Acknowledge(IOffset offset);

        /// <summary>
        /// Commits the specified offsets.
        /// </summary>
        /// <param name="offsets"></param>
        /// <returns></returns>
        Task Acknowledge(IEnumerable<IOffset> offsets);

        /// <summary>
        /// Connects and starts consuming.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnects and stops consuming.
        /// </summary>
        void Disconnect();
    }
}