// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IRequestPublisher
    {
        IEnumerable<TResponse> Execute<TResponse>(IRequest<TResponse> requestMessage);

        Task<IEnumerable<TResponse>> ExecuteAsync<TResponse>(IRequest<TResponse> requestMessage);
    }
}