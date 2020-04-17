// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Background
{
    /// <summary>
    ///     Used to signal that no lock has to be issued / checked.
    /// </summary>
    public class NullLockSettings : DistributedLockSettings
    {
        public NullLockSettings() : base("no-lock")
        {
        }
    }
}