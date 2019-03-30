// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing
{
    public interface IBehavior
    {
        Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next);
    }
}