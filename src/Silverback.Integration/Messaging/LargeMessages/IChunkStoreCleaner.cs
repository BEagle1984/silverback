// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.LargeMessages
{
    public interface IChunkStoreCleaner
    {
        Task Cleanup();
    }
}