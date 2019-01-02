// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public interface IConsumer
    {
        event EventHandler<IMessage> Received;
    }
}