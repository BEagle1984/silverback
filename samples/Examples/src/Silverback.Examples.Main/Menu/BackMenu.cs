// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Examples.Main.Menu
{
    public class BackMenu : IBackMenu
    {
        public string Title => "Back to parent";
        public string Description => null;
    }
}