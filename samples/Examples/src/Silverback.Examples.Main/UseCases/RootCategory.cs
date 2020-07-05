// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Examples.Main.Menu;
using Silverback.Examples.Main.UseCases.Producing.Kafka;

namespace Silverback.Examples.Main.UseCases
{
    public class RootCategory : ICategory
    {
        public string? Title { get; } = null;

        public string? Description { get; } = null;

        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(CategoryInfo),
            typeof(Producing.Rabbit.CategoryInfo),
            typeof(Consuming.CategoryInfo)
        };
    }
}
