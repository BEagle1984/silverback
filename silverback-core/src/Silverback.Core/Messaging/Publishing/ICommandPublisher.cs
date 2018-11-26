// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface ICommandPublisher
    {
        void Send(ICommand commandMessage);

        Task SendAsync(ICommand commandMessage);

        IEnumerable<TResult> Execute<TResult>(ICommand<TResult> commandMessage);

        Task<IEnumerable<TResult>> ExecuteAsync<TResult>(ICommand<TResult> commandMessage);
    }
}