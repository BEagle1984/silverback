// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public sealed class TestMessageFilterAttribute : MessageFilterAttribute
    {
        public override bool MustProcess(object message) =>
            message is ITestMessage testMessage && testMessage.Message == "yes" ||
            message is IEnvelope envelope && envelope.Message is ITestMessage envelopeMessage &&
            envelopeMessage.Message == "yes";
    }
}
