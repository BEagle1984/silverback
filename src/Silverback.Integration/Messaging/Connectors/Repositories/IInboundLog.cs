// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IInboundLog
    {
        Task Add(object message, IEndpoint endpoint);

        Task Commit();

        Task Rollback();

        /// <summary>
        /// Returns a boolean value indicating whether a message with the same id and endpoint
        /// has already been processed.
        /// </summary>
        Task<bool> Exists(object message, IEndpoint endpoint);

        Task<int> GetLength();
    }
}