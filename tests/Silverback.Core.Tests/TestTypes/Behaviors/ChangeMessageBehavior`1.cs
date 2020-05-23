// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Core.TestTypes.Behaviors
{
    public class ChangeMessageBehavior<TSourceType> : IBehavior
    {
        private readonly Func<object, IEnumerable<object>> _changedMessageFactory;

        public ChangeMessageBehavior(Func<object, IEnumerable<object>> changedMessageFactory)
        {
            _changedMessageFactory = changedMessageFactory;
        }

        public Task<IReadOnlyCollection<object?>> Handle(IReadOnlyCollection<object> messages, MessagesHandler next)
        {
            var newList = new List<object>();

            foreach (var msg in messages)
            {
                newList.AddRange(msg is TSourceType
                    ? _changedMessageFactory(msg)
                    : new[] { msg });
            }

            return next(newList);
        }
    }
}