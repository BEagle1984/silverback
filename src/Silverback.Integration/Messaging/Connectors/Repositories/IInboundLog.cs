// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IInboundLog : ITransactional
    {
        Task Add(object message, IConsumerEndpoint endpoint);

        /// <summary>
        ///     Returns a boolean value indicating whether a message with the same id and endpoint
        ///     has already been processed.
        /// </summary>
        Task<bool> Exists(object message, IConsumerEndpoint endpoint);

        Task<int> GetLength();
    }
}