// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface ICommand : IMessage
    {
    }

    public interface ICommand<out TResult> : ICommand, IRequest<TResult>
    {
    }
}