// Copyright (c) 2019 Sergio Aquilini
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
        
        IEnumerable<TResponse> Send<TResponse>(IEnumerable<IRequest<TResponse>> requestMessages);

        Task<IEnumerable<TResponse>> SendAsync<TResponse>(IEnumerable<IRequest<TResponse>> requestMessages);
    }
}