// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Examples.Main.Menu;
using Silverback.Examples.Main.UseCases.HealthCheck;

namespace Silverback.Examples.Main.UseCases.Kafka.HealthCheck
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class _CategoryInfo : ICategory
    {
        public string Title => "Health checks";
        public string Description => "Silverback provides some health check capabilities to monitor the connection " +
                                     "with the message broker and more.";
        
        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(OutboundEndpointsHealthUseCase),
            typeof(OutboundQueueHealthUseCase)
        };
    }
}
