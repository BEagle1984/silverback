// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Examples.Main.Menu
{
    public class MenuItemInfoEventArgs : EventArgs
    {
        public MenuItemInfoEventArgs(IMenuItemInfo menuItemInfo)
        {
            MenuItemInfo = menuItemInfo;
        }

        public IMenuItemInfo MenuItemInfo { get; }
    }
}
