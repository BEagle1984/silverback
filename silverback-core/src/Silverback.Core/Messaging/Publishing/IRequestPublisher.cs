// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IRequestPublisher<in TRequest, TResponse>
        where TRequest : IRequest
        where TResponse : IResponse
    {
        TResponse GetResponse(TRequest requestMessage, TimeSpan? timeout = null);

        Task<TResponse> GetResponseAsync(TRequest requestMessage, TimeSpan? timeout = null);
    }
}