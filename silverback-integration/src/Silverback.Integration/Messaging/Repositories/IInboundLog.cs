using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Repositories
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

        void ClearOlderEntries(DateTime threshold);

        int Length { get; }
    }
}