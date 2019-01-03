// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class KafkaConsumerEndpoint : KafkaEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        public KafkaConsumerEndpoint(params string[] names) : base(string.Join(",", names))
        {
            Names = names;
        }
        public string[] Names { get; }

        public KafkaConsumerConfig Configuration { get; set; } = new KafkaConsumerConfig();

        public override  void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        #region IEquatable

        #endregion

        public bool Equals(KafkaConsumerEndpoint other) => 
            base.Equals(other) && Equals(Configuration, other?.Configuration);

        public override bool Equals(object obj) => 
            base.Equals(obj) && obj is KafkaConsumerEndpoint endpoint && Equals(endpoint);
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}