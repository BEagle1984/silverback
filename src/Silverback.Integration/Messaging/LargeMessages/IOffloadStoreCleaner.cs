// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.LargeMessages
{
    public interface IOffloadStoreCleaner
    {
        void Cleanup();
    }
}