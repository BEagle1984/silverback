// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Core.TestTypes.Behaviors;

public class ChangeMessageBehavior<TSourceType> : IBehavior
{
    private readonly Func<object, object> _changedMessageFactory;

    public ChangeMessageBehavior(Func<object, object> changedMessageFactory)
    {
        _changedMessageFactory = changedMessageFactory;
    }

    public Task<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next) =>
        next(
            message is TSourceType
                ? _changedMessageFactory(message)
                : message);
}
