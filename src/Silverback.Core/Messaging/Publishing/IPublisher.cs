// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing
{
    public interface IPublisher
    {
        void Publish(object message);

        Task PublishAsync(object message);

        IEnumerable<TResult> Publish<TResult>(object message);

        Task<IEnumerable<TResult>> PublishAsync<TResult>(object message);

        void Publish(IEnumerable<object> messages);

        Task PublishAsync(IEnumerable<object> messages);

        IEnumerable<TResult> Publish<TResult>(IEnumerable<object> messages);

        Task<IEnumerable<TResult>> PublishAsync<TResult>(IEnumerable<object> messages);
    }
}