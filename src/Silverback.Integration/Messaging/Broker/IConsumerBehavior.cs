// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// Can be used to build a custom pipeline, plugging some functionality into the
    /// <see cref="IConsumer"/>.
    /// </summary>
    public interface IConsumerBehavior : IBrokerBehavior
    {
        /// <summary>
        /// Process, handles or transforms the message being consumed.
        /// </summary>
        /// <param name="message">The message being consumed.</param>
        /// <param name="next">The next behavior in the pipeline.</param>
        Task Handle(RawBrokerMessage message, RawBrokerMessageHandler next);
    }
}