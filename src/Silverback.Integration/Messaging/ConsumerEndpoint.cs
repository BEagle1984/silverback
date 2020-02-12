// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging
{
    public abstract class ConsumerEndpoint : Endpoint, IConsumerEndpoint
    {
        protected ConsumerEndpoint(string name)
            : base(name)
        {
        }

        /// <inheritdoc cref="IConsumerEndpoint" />
        public abstract string GetUniqueConsumerGroupName();
    }
}