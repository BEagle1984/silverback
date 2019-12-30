// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases.Kafka
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class _CategoryInfo : ICategory
    {
        public string Title => "Apache Kafka";
        public string Description => "A set of examples to demonstrate different ways to " +
                                     "use Silverback with Apache Kafka.";
        
        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(Basic._CategoryInfo),
            typeof(Advanced._CategoryInfo),
            typeof(ErrorHandling._CategoryInfo),
            typeof(Deferred._CategoryInfo),
            typeof(HealthCheck._CategoryInfo),
            typeof(StartConsumerUserCase)
        };
    }
}
