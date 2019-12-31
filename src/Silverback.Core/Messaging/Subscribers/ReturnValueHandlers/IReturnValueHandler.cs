// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    public interface IReturnValueHandler
    {
        bool CanHandle(object returnValue);

        IEnumerable<object> Handle(object returnValue);

        Task<IEnumerable<object>> HandleAsync(object returnValue);
    }
}