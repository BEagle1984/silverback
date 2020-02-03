// TODO: Remove

//// Copyright (c) 2020 Sergio Aquilini
//// This code is licensed under MIT license (see LICENSE file for details)
//
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using FluentAssertions;
//using NSubstitute;
//using Silverback.Messaging.Connectors;
//using Silverback.Messaging.Messages;
//using Silverback.Messaging.Publishing;
//using Silverback.Tests.Integration.TestTypes;
//using Silverback.Tests.Integration.TestTypes.Domain;
//using Xunit;
//
//namespace Silverback.Tests.Integration.Messaging.Connectors
//{
//    public class InboundMessageUnwrapperTests
//    {
//        private readonly IPublisher _publisher = Substitute.For<IPublisher>();
//
//        [Fact]
//        // This is important to be able to effectively avoid the mortal loop
//        public async Task Unwrap_SomeInboundMessage_IsReferenceEqualToWrappedContent()
//        {
//            object unwrappedMessage = null;
//            _publisher
//                .When(x => x.PublishAsync(Arg.Any<IEnumerable<object>>()))
//                .Do(x => { unwrappedMessage = x.Arg<IEnumerable<object>>().FirstOrDefault(); });
//
//            var message = new TestEventOne();
//            var wrappedMessage =
//                new InboundMessage<TestEventOne>(new byte[1], null, null, TestConsumerEndpoint.GetDefault(), true)
//                {
//                    Content = message
//                };
//
//            await new InboundMessageUnwrapper(_publisher).OnMessagesReceived(new[] { wrappedMessage });
//
//            unwrappedMessage.Should().BeSameAs(message);
//        }
//    }
//}