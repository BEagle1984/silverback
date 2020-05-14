// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Background
{
    /// <summary>
    ///     Used to signal that no lock has to be acquired nor checked.
    /// </summary>
    public class NullLockSettings : DistributedLockSettings
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NullLockSettings" /> class.
        /// </summary>
        public NullLockSettings()
            : base("no-lock")
        {
        }
    }
}
