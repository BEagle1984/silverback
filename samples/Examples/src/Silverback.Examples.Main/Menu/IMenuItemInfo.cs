// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.Menu
{
    public interface IMenuItemInfo
    {
        /// <summary>
        /// Gets the title to be displayed in the menu.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the longer description to be displayed when the menu is selected.
        /// </summary>
        public string Description { get; }
    }
}
