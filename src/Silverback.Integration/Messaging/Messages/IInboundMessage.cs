// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Messages
{
    public interface IInboundMessage
    {
        IEndpoint Endpoint { get; set; }

        MessageHeaderCollection Headers { get; }

        object Message { get; set; }
    }

    public interface IInboundMessage<out TMessage> : IInboundMessage
    {
        new TMessage Message { get; }
    }
}