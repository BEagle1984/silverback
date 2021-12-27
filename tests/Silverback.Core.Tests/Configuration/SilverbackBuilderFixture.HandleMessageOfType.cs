// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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

        builder.BusOptions.MessageTypes.Should().BeEquivalentTo(
            new[]
            {
                typeof(IMessage),
                typeof(UnhandledMessage)
            });
    }

    [Fact]
    public void HandleMessageOfType_ShouldAddHandledType_WhenGenericArgumentIsSpecified()
    {
        SilverbackBuilder builder = new ServiceCollection()
            .AddSilverback()
            .HandleMessagesOfType<UnhandledMessage>();

        builder.BusOptions.MessageTypes.Should().BeEquivalentTo(
            new[]
            {
                typeof(IMessage),
                typeof(UnhandledMessage)
            });
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    private class UnhandledMessage
    {
    }
}
