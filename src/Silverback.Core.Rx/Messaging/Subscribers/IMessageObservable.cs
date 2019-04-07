// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers
{
    public interface IMessageObservable<out TMessage> : IObservable<TMessage>
    {
    }
}