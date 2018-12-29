// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IInboundLog
    {
        void Add(IIntegrationMessage message, IEndpoint endpoint);

        void Commit();

        void Rollback();

        /// <summary>
        /// Returns a boolean value indicating whether a message with the same id and endpoint
        /// has already been processed.
        /// </summary>
        bool Exists(IIntegrationMessage message, IEndpoint endpoint);

        int Length { get; }
    }
}