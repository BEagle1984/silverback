// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Util;

namespace Silverback.Tests.Core.TestTypes.Behaviors
{
    public class ChangeTestEventOneContentBehavior : IBehavior
    {
        public Task<IReadOnlyCollection<object?>> Handle(IReadOnlyCollection<object> messages, MessagesHandler next)
        {
            messages.OfType<TestEventOne>().ForEach(m => m.Message = "behavior");

            var result = next(messages);

            return result;
        }
    }
}