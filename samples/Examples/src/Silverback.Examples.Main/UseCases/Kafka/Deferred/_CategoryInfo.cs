// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases.Kafka.Deferred
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class _CategoryInfo : ICategory
    {
        public string Title => "Deferred publish (outbox table)";

        public string Description => "Use an outbox table to handle transactionality and make your microservice " +
                                     "even more resilient.";

        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(DeferredOutboundUseCase),
            typeof(OutboundWorkerUseCase)
        };
    }
}