// TODO: DELETE

//// Copyright (c) 2018-2019 Sergio Aquilini
//// This code is licensed under MIT license (see LICENSE file for details)

//using FluentAssertions;
//using Silverback.Messaging.Messages;
//using Silverback.Tests.Integration.TestTypes;
//using Xunit;

//namespace Silverback.Tests.Integration.Messaging.Messages
//{
//    public class InboundMessageHelperTests
//    {
//        [Fact]
//        public void Create_InboundMessage_MessageReplaced()
//        {
//            var inboundMessage = new InboundMessage
//            {
//                Message = 1,
//            };

//            var newInboundMessage = InboundMessageHelper.CreateNewInboundMessage(2, inboundMessage);

//            newInboundMessage.Message.Should().Be(2);
//        }

//        [Fact]
//        public void Create_InboundMessage_ValuesPreserved()
//        {
//            var inboundMessage = new InboundMessage
//            {
//                Message = 1,
//                Endpoint = TestEndpoint.Default,
//                FailedAttempts = 3,
//                Offset = new TestOffset("a", "b"),
//                MustUnwrap = true
//            };
//            inboundMessage.Headers.Add("h1", "h1");
//            inboundMessage.Headers.Add("h2", "h2");

//            var newInboundMessage = InboundMessageHelper.CreateNewInboundMessage(2, inboundMessage);

//            newInboundMessage.Endpoint.Should().BeEquivalentTo(inboundMessage.Endpoint);
//            newInboundMessage.FailedAttempts.Should().Be(inboundMessage.FailedAttempts);
//            newInboundMessage.Offset.Should().BeEquivalentTo(inboundMessage.Offset);
//            newInboundMessage.MustUnwrap.Should().Be(inboundMessage.MustUnwrap);
//            newInboundMessage.Headers.Should().BeEquivalentTo(inboundMessage.Headers);
//        }
//    }
//}