// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Behaviors
{
    public class ChangeTestEventOneContentBehavior : IBehavior
    {
        [SuppressMessage("", "CA1822", Justification = Justifications.CalledBySilverback)]
        public Task<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next)
        {
            if (message is TestEventOne testEventOne)
                testEventOne.Message = "behavior";

            return next(message);
        }
    }
}
