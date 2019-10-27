// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Messages
{
    internal interface IOutboundMessageInternal : IOutboundMessage
    {
        IOutboundRoute Route { get; }
    }
}