// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    // TODO: Add overload accepting enumerable
    public interface IEventPublisher
    {
        void Publish(IEvent eventMessage);

        Task PublishAsync(IEvent eventMessage);

        void Publish(IEnumerable<IEvent> eventMessages);

        Task PublishAsync(IEnumerable<IEvent> eventMessages);
    }
}