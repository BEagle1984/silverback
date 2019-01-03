using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Batch;

namespace Silverback.Messaging.Connectors
{
    public class InboundConnectorSettings
    {
        public  BatchSettings Batch { get; set; } = new BatchSettings();

        /// <summary>
        /// The number of parallel consumers. The default is 1.
        /// </summary>
        public int Consumers { get; set; } = 1;
    }
}
