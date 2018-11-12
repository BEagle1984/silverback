using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    public class RequestPublisher<TRequest, TResponse> : IRequestPublisher<TRequest, TResponse>
        where TRequest : IRequest
        where TResponse : IResponse
    {
        private readonly IPublisher _publisher;

        public RequestPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public TResponse GetResponse(TRequest requestMessage, TimeSpan? timeout = null)
            => AsyncHelper.RunSynchronously(() => GetResponseAsync(requestMessage, timeout));

        public async Task<TResponse> GetResponseAsync(TRequest requestMessage, TimeSpan? timeout = null)
        {
            if (requestMessage == null) throw new ArgumentNullException(nameof(requestMessage));

            timeout = timeout ?? TimeSpan.FromSeconds(2);

            EnsureRequestIdIsSet(requestMessage);

            IResponse response = default;

            try
            {
                ResponseSubscriber.ResponseReceived += OnResponseSubscriberOnResponseReceived;

#pragma warning disable 4014
                _publisher.PublishAsync(requestMessage);
#pragma warning restore 4014

                await WaitForResponse();

                return (TResponse) response;
            }
            finally
            {
                ResponseSubscriber.ResponseReceived -= OnResponseSubscriberOnResponseReceived;
            }

            void EnsureRequestIdIsSet(IRequest message)
            {
                if (message.RequestId == Guid.Empty)
                    message.RequestId = Guid.NewGuid();
            }

            void OnResponseSubscriberOnResponseReceived(object _, IResponse r)
            {
                if (r.RequestId == requestMessage.RequestId)
                    response = r;
            }

            async Task WaitForResponse()
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                while (response == null)
                {
                    if (stopwatch.Elapsed >= timeout)
                        throw new TimeoutException($"The request with id {requestMessage.RequestId} was not replied in the allotted time.");

                    await Task.Delay(50); // TODO: Check this!
                }

                stopwatch.Stop();
            }
        }
    }
}