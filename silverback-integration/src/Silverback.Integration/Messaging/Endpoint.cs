// TODO: DELETE

// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

//using System;
//using Silverback.Messaging.Batch;
//using Silverback.Messaging.Serialization;

//namespace Silverback.Messaging
//{
//    public abstract class ProducerEndpoint : IEndpoint, IEquatable<ProducerEndpoint>
//    {

//    }
//    public abstract class ConsumerEndpoint : IEndpoint, IEquatable<ConsumerEndpoint>
//    {
//        protected Endpoint(string name)
//        {
//            Name = name;
//        }

//        /// <summary>
//        /// The topic/queue name.
//        /// </summary>
//        public string Name { get; }

//        public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

//        public BatchSettings Batch { get; set; } = new BatchSettings();

//        /// <summary>
//        /// The maximum number of parallel threads used to process the messages in the batch.
//        /// </summary>
//        public virtual void Validate()
//        {
//            if (string.IsNullOrWhiteSpace(Name))
//                throw new EndpointConfigurationException("Name is not set.");

//            if (Serializer == null)
//                throw new EndpointConfigurationException("Serializer is not set.");

//            if (Batch.Size < 1)
//                throw new EndpointConfigurationException("Batch.Size must be greater or equal to 1.");

//            if (Batch.MaxDegreeOfParallelism < 1)
//                throw new EndpointConfigurationException("Batch.MaxDegreeOfParallelism must be greater or equal to 1.");

//            if (Batch.MaxWaitTime <= TimeSpan.Zero)
//                throw new EndpointConfigurationException("Batch.MaxWaitTime must be greater than 0. Set it to TimeSpan.MaxValue to disable the timer.");
//        }

//        public bool Equals(Endpoint other)
//        {
//            if (ReferenceEquals(null, other)) return false;
//            if (ReferenceEquals(this, other)) return true;
//            return string.Equals(Name, other.Name, StringComparison.InvariantCulture) && 
//                   Equals(Serializer, other.Serializer) &&
//                   Equals(Batch, other.Batch);
//        }

//        public override bool Equals(object obj)
//        {
//            if (ReferenceEquals(null, obj)) return false;
//            if (ReferenceEquals(this, obj)) return true;
//            return obj is Endpoint other && Equals(other);
//        }

//        public override int GetHashCode()
//        {
//            return Name != null ? Name.GetHashCode() : 0;
//        }
//    }
//}