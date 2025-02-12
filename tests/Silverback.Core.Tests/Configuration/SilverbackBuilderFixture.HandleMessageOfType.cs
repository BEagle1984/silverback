// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

public partial class SilverbackBuilderFixture
{
    [Fact]
    public void HandleMessageOfType_ShouldAddHandledType_WhenTypeIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .HandleMessagesOfType(typeof(UnhandledMessage));

        builder.MediatorOptions.MessageTypes.ShouldBe(
        [
            typeof(IMessage),
                typeof(UnhandledMessage)
        ]);
    }

    [Fact]
    public void HandleMessageOfType_ShouldAddHandledType_WhenGenericArgumentIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .HandleMessagesOfType<UnhandledMessage>();

        builder.MediatorOptions.MessageTypes.ShouldBe(
        [
            typeof(IMessage),
                typeof(UnhandledMessage)
        ]);
    }

    private class UnhandledMessage;
}
