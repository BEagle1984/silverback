// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IPublisher
    {
        void Publish(IMessage message);

        Task PublishAsync(IMessage message);

        IEnumerable<TResult> Publish<TResult>(IMessage message);

        Task<IEnumerable<TResult>> PublishAsync<TResult>(IMessage message);
    }
}