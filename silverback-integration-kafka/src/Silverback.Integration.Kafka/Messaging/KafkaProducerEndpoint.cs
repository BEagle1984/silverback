// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class KafkaProducerEndpoint : KafkaEndpoint, IEquatable<KafkaProducerEndpoint>
    {
        public KafkaProducerEndpoint(string name) : base(name)
        {
        }

        public KafkaProducerConfig Configuration { get; set; } = new KafkaProducerConfig();

        #region IEquatable

        public bool Equals(KafkaProducerEndpoint other) => 
            base.Equals(other) && Equals(Configuration, other?.Configuration);

        public override bool Equals(object obj) => 
            base.Equals(obj) &&obj is KafkaProducerEndpoint endpoint && Equals(endpoint);

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}