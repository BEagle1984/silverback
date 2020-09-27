// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Topics
{
    public interface IInMemoryTopicCollection
    {
        IInMemoryTopic GetTopic(string name);
    }
}
