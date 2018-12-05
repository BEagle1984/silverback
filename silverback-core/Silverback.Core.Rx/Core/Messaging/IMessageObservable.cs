// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Core.Messaging
{
    public interface IMessageObservable<out TMessage> : IObservable<TMessage> where TMessage : IMessage
    {
    }
}