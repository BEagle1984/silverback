// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Provides enrichment for activities produced by the <see cref="ActivityProducerBehavior" /> and
    ///     <see cref="ActivityConsumerBehavior" />.
    /// </summary>
    public interface IBrokerActivityEnricher
    {
        /// <summary>
        ///     Enriches Activities created by the <see cref="ActivityProducerBehavior" />.
        /// </summary>
        /// <param name="activity">
        ///     The <see cref="Activity"/> to be enriched.
        /// </param>
        /// <param name="producerContext">
        ///     The <see cref="ProducerPipelineContext" />.
        /// </param>
        public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext producerContext);

        /// <summary>
        ///     Enriches Activities created by the <see cref="ActivityConsumerBehavior" />.
        /// </summary>
        /// <param name="activity">
        ///     The <see cref="Activity"/> to be enriched.
        /// </param>
        /// <param name="consumerContext">
        ///     The <see cref="ConsumerPipelineContext" />.
        /// </param>
        public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext consumerContext);
    }
}
