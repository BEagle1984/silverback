using System;

namespace Silverback.Messaging.Batch
{
    public class BatchSettings
    {
        /// <summary>
        /// The number of messages to be processed in batch.
        /// </summary>
        public int Size { get; set; } = 1;

        /// <summary>
        /// The maximum amount of time to wait for the batch to be filled. After this
        /// time the batch will be processed even if the BatchSize is not reached.
        /// </summary>
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.MaxValue;

        /// <summary>
        /// The maximum number of parallel threads used to process the messages in the batch.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 1;
    }
}