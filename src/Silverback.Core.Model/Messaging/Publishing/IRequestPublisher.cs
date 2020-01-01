// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IRequestPublisher
    {
        TResponse Send<TResponse>(IRequest<TResponse> requestMessage);

        Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> requestMessage);

        IReadOnlyCollection<TResponse> Send<TResponse>(IEnumerable<IRequest<TResponse>> requestMessages);

        Task<IReadOnlyCollection<TResponse>> SendAsync<TResponse>(IEnumerable<IRequest<TResponse>> requestMessages);
    }
}