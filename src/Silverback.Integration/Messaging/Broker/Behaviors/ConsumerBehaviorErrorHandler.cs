// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Behaviors
{
    public delegate Task ConsumerBehaviorErrorHandler(
        ConsumerPipelineContext context,
        IServiceProvider serviceProvider,
        Exception exception);
}