// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    public interface IReturnValueHandler
    {
        bool CanHandle(object returnValue);

        void Handle(object returnValue);

        Task HandleAsync(object returnValue);
    }
}