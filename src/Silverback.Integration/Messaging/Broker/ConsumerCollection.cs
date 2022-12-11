// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal class ConsumerCollection : List<IConsumer>, IConsumerCollection, IAsyncDisposable
{
    // TODO: Implement find methods (by id/name/friendly name)
    public ValueTask DisposeAsync() => this.DisposeAllAsync();
}
