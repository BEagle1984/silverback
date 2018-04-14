using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;

namespace Silverback.Messaging
{

    public abstract class DeliveryProcessor : IDeliveryResultHandler
    {
        private bool _disposed;

        public abstract void ProcessDelivery(Message<byte[], byte[]> message);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DeliveryProcessor()
        {
            Dispose(false);
        }

    }
}
