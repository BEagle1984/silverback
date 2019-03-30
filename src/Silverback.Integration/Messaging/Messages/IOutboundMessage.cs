// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Messages
{
    public interface IOutboundMessage
    {
        IEndpoint Endpoint { get; set; }

        MessageHeaderCollection Headers { get; }

        object Message { get; set; }
    }

    public interface IOutboundMessage<out TMessage> : IOutboundMessage
    {
        new TMessage Message { get; }
    }
}