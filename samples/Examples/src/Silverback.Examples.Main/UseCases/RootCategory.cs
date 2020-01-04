﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases
{
    public class RootCategory : ICategory
    {
        public string Title => null;
        public string Description => null;

        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(Kafka._CategoryInfo),
            typeof(Rabbit._CategoryInfo)
        };
    }
}
