// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Messages
{
    public interface ICommand : IMessage
    {
    }

    public interface ICommand<TResult> : ICommand, IRequest<TResult>
    {
    }
}