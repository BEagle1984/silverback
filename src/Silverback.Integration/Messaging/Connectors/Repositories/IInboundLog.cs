// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IInboundLog : ITransactional
    {
        Task Add(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Returns a boolean value indicating whether a message with the same id and endpoint
        ///     has already been processed.
        /// </summary>
        Task<bool> Exists(IRawInboundEnvelope envelope);

        Task<int> GetLength();
    }
}