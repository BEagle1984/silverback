// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public abstract class Endpoint : IEndpoint
    {
        protected Endpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The topic/queue name.
        /// </summary>
        public string Name { get; }

        public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

        public BatchSettings Batch { get; set; } = new BatchSettings();

        /// <summary>
        /// The maximum number of parallel threads used to process the messages in the batch.
        /// </summary>
        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new EndpointConfigurationException("Name is not set.");

            if (Serializer == null)
                throw new EndpointConfigurationException("Serializer is not set.");

            if (Batch.Size < 1)
                throw new EndpointConfigurationException("Batch.Size must be greater or equal to 1.");

            if (Batch.MaxDegreeOfParallelism < 1)
                throw new EndpointConfigurationException("Batch.MaxDegreeOfParallelism must be greater or equal to 1.");

            if (Batch.MaxWaitTime <= TimeSpan.Zero)
                throw new EndpointConfigurationException("Batch.MaxWaitTime must be greater than 0. Set it to TimeSpan.MaxValue to disable the timer.");
        }
    }
}