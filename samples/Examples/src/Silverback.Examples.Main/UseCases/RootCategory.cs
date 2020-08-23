// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases
{
    public class RootCategory : ICategory
    {
        [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Null is the correct value.")]
        public string? Title { get; }

        [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Null is the correct value.")]
        public string? Description { get; }

        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(Producing.CategoryInfo),
            typeof(Consuming.CategoryInfo)
        };
    }
}
