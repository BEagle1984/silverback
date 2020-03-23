// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases.Consuming
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class _CategoryInfo : ICategory
    {
        public string Title => "Consuming";

        public string Description => "A set of examples to demonstrate different ways to " +
                                     "use Silverback to consume messages.";

        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(StartConsumerUserCase)
        };
    }
}