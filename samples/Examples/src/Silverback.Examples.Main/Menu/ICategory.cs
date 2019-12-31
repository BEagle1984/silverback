// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Examples.Main.Menu
{
    public interface ICategory : IMenuItemInfo
    {
        public IEnumerable<Type> Children { get; }
    }
}