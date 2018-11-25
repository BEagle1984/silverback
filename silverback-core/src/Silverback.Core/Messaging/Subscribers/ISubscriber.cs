// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// This empty interface as to be implemented in all classes that contain one or more subscribed methods
    /// (see <see cref="SubscribeAttribute"/>) and it's sole purpose is to allow dependency injection 
    /// in the <see cref="IPublisher"/> implementation.
    /// </summary>
    public interface ISubscriber
    {
    }
}