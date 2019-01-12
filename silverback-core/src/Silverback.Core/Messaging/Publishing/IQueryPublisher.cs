// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IQueryPublisher
    {
        IEnumerable<TResult> Execute<TResult>(IQuery<TResult> queryMessage);

        Task<IEnumerable<TResult>> ExecuteAsync<TResult>(IQuery<TResult> queryMessage);
    }
}