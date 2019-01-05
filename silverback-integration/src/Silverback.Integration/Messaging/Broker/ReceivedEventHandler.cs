// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public delegate void ReceivedEventHandler(object sender, IMessage message, object offset);
}